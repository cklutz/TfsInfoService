using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.TeamFoundation.Common.Internal;
using TfsInfoService.Utilities;

namespace TfsInfoService.Controllers
{
    [Route("_apis/infos")]
    public class InfosController : Controller
    {
        private readonly TfsOptions m_tfsOptions;

        public InfosController(IOptionsSnapshot<TfsOptions> tfsOptions)
        {
            m_tfsOptions = tfsOptions.Value;
        }

        [HttpGet]
        public IEnumerable<string> Get()
        {
            return new[] { "result-age", "buildnumber", "duration", "finishdate", "coverage", "best-coverage", "custom" };
        }

        [HttpGet("{teamProject}/{buildDefinitionId}/{type}/badge")]
        public async Task<IActionResult> GetBadge(Guid teamProject, int buildDefinitionId, string type,
            string title, string titlefg, string titlebg,
            string value, string valuefg, string valuebg,
            string subType)
        {
            // http://localhost:50573/_apis/infos/c4f86f26-1bf4-4452-bd7e-67db7a5c1486/335/buildnumber/badge
            // http://localhost:50573/_apis/infos/c4f86f26-1bf4-4452-bd7e-67db7a5c1486/335/custom/badge?title=hello&value=33773

            titlebg = GetValue(titlebg, "#f1f1f1");
            titlefg = GetValue(titlefg, "#000");
            valuebg = GetValue(valuebg, "#08298A");
            valuefg = GetValue(valuefg, "#fff");

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

                    switch (type.ToLowerInvariant())
                    {
                        case "buildnumber":
                            title = title ?? "number";
                            value = build.BuildNumber;
                            break;
                        case "result-age":
                            title = title ?? "build";
                            value = GetResultAge(subType, build, ref valuebg, ref valuefg);
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
                        default:
                            // Use title, value, etc. as passed.
                            value = value ?? "-";
                            break;
                    }
                }
            }
            var doc = BadgeGenerator.CreateSvgBadge(title, titlefg, titlebg, value, valuefg, valuebg);
            return Content(doc.ToString(), "image/svg+xml");
        }

        private static string GetResultAge(string subType, Build build, ref string valuebg, ref string valuefg)
        {
            string value;

            if (!build.FinishTime.HasValue)
            {
                valuefg = "#fff";
                valuebg = "#2E64FE";
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
                        valuefg = "#fff";
                        valuebg = "#4BAE4F";
                        displayResult = "succeeded";
                        break;
                    case BuildResult.PartiallySucceeded:
                        valuefg = "#000";
                        valuebg = "#FEC006";
                        displayResult = "partially succeeded";
                        break;
                    case BuildResult.Failed:
                        valuefg = "#fff";
                        valuebg = "#F34235";
                        displayResult = "failed";
                        break;
                    case BuildResult.Canceled:
                        valuefg = "#fff";
                        valuebg = "#F34235";
                        displayResult = "canceled";
                        break;
                    default:
                        valuefg = "#fff";
                        valuebg = "#BBBBBB";
                        displayResult = "none";
                        break;
                }

                value = build.FinishTime.Value.Ago();

                if ("result-label".Equals(subType, StringComparison.OrdinalIgnoreCase))
                {
                    value = displayResult + " " + value;
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
