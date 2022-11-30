using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.TeamFoundation.Build.WebApi;
using TfsInfoService.Utilities;

namespace TfsInfoService.Controllers
{
    [Route("_apis/infos")]
    public class InfosController : Controller
    {
        private readonly TfsOptions m_tfsOptions;
        private readonly ILogger m_logger;
        private static readonly ConcurrentDictionary<(int, string), string> s_agentNameCache = new();

        public InfosController(IOptionsSnapshot<TfsOptions> tfsOptions, ILoggerFactory loggerFactory)
        {
            m_tfsOptions = tfsOptions.Value;
            m_logger = loggerFactory.CreateLogger(typeof(InfosController));
        }

        [HttpGet]
        public IEnumerable<string> Get()
        {
            return new[] { "result-age", "buildnumber", "duration", "finishdate", "coverage", "best-coverage",
                "queue-name", "queue-position", "agent-computer", "source-version", "source-branch",
                "custom" };
        }

        [HttpGet("clear-caches")]
        public async Task<IActionResult> ClearCaches()
        {
            s_agentNameCache.Clear();
            await Task.Delay(0);
            return Ok();
        }

        [HttpGet("{teamProject}/{buildDefinitionId}/{type}/badge")]
        public async Task<IActionResult> GetBadge(Guid teamProject, int buildDefinitionId, string type,
            string title, string titlefg, string titlebg,
            string value, string valuefg, string valuebg,
            string subType,
            string toolTip,
            string href)
        {
            try
            {
                // http://localhost:50573/_apis/infos/c4f86f26-1bf4-4452-bd7e-67db7a5c1486/335/buildnumber/badge
                // http://localhost:50573/_apis/infos/c4f86f26-1bf4-4452-bd7e-67db7a5c1486/335/custom/badge?title=hello&value=33773

                titlebg = GetValue(titlebg, "#f1f1f1");
                titlefg = GetValue(titlefg, "#000");
                valuebg = GetValue(valuebg, "#08298A");
                valuefg = GetValue(valuefg, "#fff");
                string link = null;

                // Badge and tooltip might use same values, some of which are expensive.
                var scopeCache = new Dictionary<(string, string), (string, string)>();

                if (type == "custom")
                {
                    value = value ?? "-";
                }
                else
                {
                    using (var client = m_tfsOptions.GetBuildClient())
                    {
                        var result = await client.GetBuildsAsync(teamProject, new[] { buildDefinitionId });
                        if (!result.Any())
                        {
                            return new JsonResult($"Build definition {buildDefinitionId} was not found.");
                        }

                        var build = result.First();

                        (title, value) = await GetTypeAndValueAsync(teamProject, build, type, subType, client, scopeCache);
                        if (type?.ToLowerInvariant() == "result-age")
                        {
                            GetResultColors(build, ref valuebg, ref valuefg);
                        }

                        if (!string.IsNullOrWhiteSpace(toolTip))
                        {
                            toolTip = await GenerateToolTipText(teamProject, build, toolTip, client, scopeCache);
                        }

                        link = GetLink(href, build);
                    }
                }
                var doc = BadgeGenerator.CreateSvgBadge(title, titlefg, titlebg, value, valuefg, valuebg, toolTip, link);
                return Content(doc.ToString(), "image/svg+xml");
            }
            catch (Exception ex)
            {
                m_logger.LogError(ex, $"Request failed: {Request.GetDisplayUrl()}");
                throw;
            }
        }

        private async Task<string> GetAgentComputerName(Guid teamProject, Build build, BuildHttpClient client)
        {
            var timeline = await client.GetBuildTimelineAsync(teamProject, build.Id);
            var record = timeline.Records.FirstOrDefault(t => !string.IsNullOrEmpty(t.WorkerName));

            if (record != null)
            {
                string workerName = record.WorkerName;
                int poolId = build.Queue.Pool.Id;

                if (!s_agentNameCache.TryGetValue((poolId, workerName), out string computerName))
                {
                    using (var c = m_tfsOptions.GetTaskAgentClient())
                    {
                        var data = await c.GetAgentsAsync(poolId, workerName, true);

                        if (data.Count > 0 && data[0].SystemCapabilities.TryGetValue("Agent.ComputerName", out computerName))
                        {
                            s_agentNameCache.TryAdd((poolId, workerName), computerName);
                            return computerName;
                        }
                    }
                }
            }

            return null;
        }

        private static string GetLink(string href, Build build)
        {
            string link = null;

            if (href == "build-result")
            {
                if (build.Links?.Links != null && build.Links.Links.TryGetValue("web", out var entry))
                {
                    if (entry is Microsoft.VisualStudio.Services.WebApi.ReferenceLink referenceLink)
                    {
                        link = referenceLink.Href;
                    }
                }
            }
            else
            {
                link = href;
            }

            return link;
        }

        private async Task<string> GenerateToolTipText(Guid teamProject, Build build, string template, BuildHttpClient client,
            Dictionary<(string, string), (string, string)> scopeCache)
        {
            try
            {
                int paramNumber = 0;
                var values = new List<string>();

                string formatString = Regex.Replace(template, @"{(?<exp>[^}]+)}", match =>
                {
                    values.Add(match.Groups["exp"].Value);
                    return "{" + paramNumber++ + "}".ToString();
                }, RegexOptions.Singleline | RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(5));

                for (int i = 0; i < values.Count; i++)
                {
                    string type = values[i];
                    var (_, value) = await GetTypeAndValueAsync(teamProject, build, type, "", client, scopeCache);
                    values[i] = value;
                }

                return string.Format(formatString, values.ToArray());
            }
            catch (FormatException ex)
            {
                m_logger.LogError(ex, $"Failed to generate tool tip text for '{template}");
                return template;
            }
            catch (RegexMatchTimeoutException ex)
            {
                m_logger.LogError(ex, $"Failed to generate tool tip text for '{template}");
                return template;
            }
        }

        private async Task<(string Title, string Value)> GetTypeAndValueAsync(Guid teamProject, Build build, string type, string subType,
            BuildHttpClient client, Dictionary<(string, string), (string, string)> scopeCache)
        {
            string title = null;
            string value = null;

            if (scopeCache.TryGetValue((type, subType), out (string, string) cached))
            {
                return cached;
            }

            switch (type.ToLowerInvariant())
            {
                case "buildnumber":
                    title = title ?? "number";
                    value = build.BuildNumber;
                    break;
                case "result-age":
                    title = title ?? "build";
                    value = GetResultAge(subType, build, ref title);
                    break;
                case "duration":
                    title = title ?? "duration";
                    value = GetDuration(build);
                    break;
                case "finishdate":
                    title = title ?? "finished";
                    value = GetFinishDate(build);
                    break;
                case "best-coverage":
                    title = title ?? "coverage";
                    value = await GetBestCoverageAsync(teamProject, build);
                    break;
                case "coverage":
                    title = title ?? "coverage";
                    value = await GetCoverageAsync(teamProject, subType, build);
                    break;
                case "queue-name":
                    title = title ?? "queue";
                    value = build.Queue?.Name ?? "-";
                    break;
                case "queue-position":
                    title = title ?? "queue position";
                    value = build.QueuePosition != null ? build.QueuePosition.ToString() : "-";
                    break;
                case "agent-computer":
                    title = title ?? "agent";
                    value = await GetAgentComputerName(teamProject, build, client);
                    break;
                case "source-version":
                    title = title ?? "source version";
                    value = build.SourceVersion;
                    break;
                case "source-branch":
                    title = title ?? "source branch";
                    value = build.SourceBranch;
                    break;
                default:
                    // Use title, value, etc. as passed.
                    value = value ?? "-";
                    break;
            }

            scopeCache.Add((type, subType), (title, value));

            return (title, value);
        }

        private static void GetResultColors(Build build, ref string valuebg, ref string valuefg)
        {
            if (!build.FinishTime.HasValue)
            {
                valuefg = "#fff";
                valuebg = "#2E64FE";
            }
            else
            {
                switch (build.Result.GetValueOrDefault())
                {
                    case BuildResult.Succeeded:
                        valuefg = "#fff";
                        valuebg = "#4BAE4F";
                        break;
                    case BuildResult.PartiallySucceeded:
                        valuefg = "#000";
                        valuebg = "#FEC006";
                        break;
                    case BuildResult.Failed:
                        valuefg = "#fff";
                        valuebg = "#F34235";
                        break;
                    case BuildResult.Canceled:
                        valuefg = "#fff";
                        valuebg = "#F34235";
                        break;
                    default:
                        valuefg = "#fff";
                        valuebg = "#BBBBBB";
                        break;
                }
            }
        }

        private static string GetResultAge(string subType, Build build, ref string title)
        {
            string value;

            if (!build.FinishTime.HasValue)
            {
                if (build.StartTime.HasValue)
                {
                    value = "started " + build.StartTime.Value.Ago();
                }
                else
                {
                    value = "in progress";
                }
            }
            else
            {
                string displayResult;
                switch (build.Result.GetValueOrDefault())
                {
                    case BuildResult.Succeeded:
                        displayResult = "succeeded";
                        break;
                    case BuildResult.PartiallySucceeded:
                        displayResult = "partially succeeded";
                        break;
                    case BuildResult.Failed:
                        displayResult = "failed";
                        break;
                    case BuildResult.Canceled:
                        displayResult = "canceled";
                        break;
                    default:
                        displayResult = "none";
                        break;
                }

                value = build.FinishTime.Value.Ago();

                if (!string.IsNullOrWhiteSpace(subType))
                {
                    string[] subTypes = subType.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var s in subTypes)
                    {
                        if ("result-value".Equals(s, StringComparison.OrdinalIgnoreCase))
                        {
                            value = displayResult + " " + value;
                        }
                        else if ("buildnumber-title".Equals(s, StringComparison.OrdinalIgnoreCase))
                        {
                            title = build.BuildNumber;
                        }
                    }
                }
            }
            return value;
        }

        private async Task<string> GetCoverageAsync(Guid teamProject, string subType, Build build)
        {
            var sb = new StringBuilder();
            using (var tc = m_tfsOptions.GetTestManagementClient())
            {
                var data = await tc.GetCodeCoverageSummaryAsync(teamProject, build.Id);
                foreach (var entry in data.CoverageData)
                {
                    foreach (var x in entry.CoverageStats)
                    {
                        if (subType != null && !subType.Equals(x.Label, StringComparison.OrdinalIgnoreCase))
                            continue;

                        if (sb.Length > 0)
                        {
                            sb.Append(" ");
                        }
                        sb.Append(x.Label.ToLowerInvariant());
                        sb.Append("  ");
                        sb.Append((x.Covered / (double)x.Total * 100.0).ToString("N1"));
                        sb.Append("%");
                    }
                }
            }
            return sb.Length == 0 ? "n.a." : sb.ToString();
        }

        private async Task<string> GetBestCoverageAsync(Guid teamProject, Build build)
        {
            double coverage = double.MinValue;
            using (var tc = m_tfsOptions.GetTestManagementClient())
            {
                var data = await tc.GetCodeCoverageSummaryAsync(teamProject, build.Id);
                foreach (var entry in data.CoverageData)
                {
                    foreach (var x in entry.CoverageStats)
                    {
                        double c = x.Covered / (double)x.Total * 100.0;
                        coverage = Math.Max(c, coverage);
                    }
                }
            }
            return coverage == Double.MinValue ? "n.a." : coverage.ToString("N1") + "%";
        }

        private static string GetFinishDate(Build build)
        {
            string value;
            if (build.FinishTime.HasValue)
            {
                value = build.FinishTime.Value.ToString("yyyy-MM-dd");
            }
            else
            {
                value = build.Status.GetValueOrDefault().ToString();
            }
            return value;
        }

        private static string GetDuration(Build build)
        {
            string value;
            if (build.FinishTime.HasValue && build.StartTime.HasValue)
            {
                value = (build.FinishTime.Value - build.StartTime.Value).TotalMinutes.ToString("N2") + " min";
            }
            else
            {
                value = build.Status.GetValueOrDefault().ToString();
            }
            return value;
        }

        private static string GetValue(string str, string def)
        {
            if (string.IsNullOrWhiteSpace(str))
                return def;
            return str;
        }
    }
}
