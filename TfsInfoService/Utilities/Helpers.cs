using System;
using System.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.TeamFoundation.Test.WebApi;
using Microsoft.TeamFoundation.TestManagement.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;

namespace TfsInfoService.Utilities
{
    internal static class Helpers
    {
        public static VssConnection GetVssConnection(Uri serverUrl, string personalAccessToken)
        {
            var credentials = new PatCredentials("", personalAccessToken);
            var connection = new VssConnection(serverUrl, credentials);
            return connection;
        }

        public static BuildHttpClient GetBuildClient(this TfsOptions configuration)
        {
            var connection = GetVssConnection(new Uri(configuration.ServerUrl), configuration.Token);
            var settings = new VssHttpRequestSettings();
            return new BuildHttpClient(connection.Uri, connection.Credentials, settings);
        }

        public static TaskAgentHttpClient GetTaskAgentClient(this TfsOptions configuration)
        {
            var connection = GetVssConnection(new Uri(configuration.ServerUrl), configuration.Token);
            var settings = new VssHttpRequestSettings();
            return new TaskAgentHttpClient(connection.Uri, connection.Credentials, settings);
        }

        public static TestHttpClient GetTestClient(this TfsOptions configuration)
        {
            var connection = GetVssConnection(new Uri(configuration.ServerUrl), configuration.Token);
            return new TestHttpClient(connection.Uri, connection.Credentials);
        }

        public static TestManagementHttpClient GetTestManagementClient(this TfsOptions configuration)
        {
            var connection = GetVssConnection(new Uri(configuration.ServerUrl), configuration.Token);
            return new TestManagementHttpClient(connection.Uri, connection.Credentials);
        }

        public static string Ago(this DateTime dt)
        {
            TimeSpan timeSince = DateTime.Now.Subtract(dt);

            if (timeSince.TotalMilliseconds < 1)
                return "not yet";

            if (timeSince.TotalMinutes < 1)
                return "just now";
            if (timeSince.TotalMinutes < 2)
                return "1 minute ago";
            if (timeSince.TotalMinutes < 60)
                return string.Format("{0} minutes ago", timeSince.Minutes);
            if (timeSince.TotalMinutes < 120)
                return "1 hour ago";
            if (timeSince.TotalHours < 24)
                return string.Format("{0} hours ago", timeSince.Hours);
            if (timeSince.TotalDays == 1)
                return "yesterday";
            if (timeSince.TotalDays < 7)
                return string.Format("{0} days ago", timeSince.Days);
            if (timeSince.TotalDays < 14)
                return "last week";
            if (timeSince.TotalDays < 21)
                return "2 weeks ago";
            if (timeSince.TotalDays < 28)
                return "3 weeks ago";
            if (timeSince.TotalDays < 60)
                return "last month";
            if (timeSince.TotalDays < 365)
                return string.Format("{0} months ago", Math.Round(timeSince.TotalDays / 30));
            if (timeSince.TotalDays < 730)
                return "last year";

            return string.Format("{0} years ago", Math.Round(timeSince.TotalDays / 365));
        }
    }
}