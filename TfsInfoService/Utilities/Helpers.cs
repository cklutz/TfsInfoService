using System;
using Microsoft.Extensions.Configuration;
using Microsoft.TeamFoundation.Build.WebApi;
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
    }
}