using System;
using System.Diagnostics;
using System.IO;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.WindowsServices;
using Microsoft.Extensions.Configuration;

namespace TfsInfoService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            if (Environment.UserInteractive)
            {
                BuildWebHost(args).Run();
            }
            else
            {
                BuildWebHost(args).RunAsService();
            }
        }

        public static IWebHost BuildWebHost(string[] args)
        {
            var pathToExe = Process.GetCurrentProcess().MainModule.FileName;
            var pathToContentRoot = Path.GetDirectoryName(pathToExe);

            var configuration = new ConfigurationBuilder()
                .AddCommandLine(args).Build();

            return WebHost.CreateDefaultBuilder(args)
                .UseContentRoot(pathToContentRoot)
                .UseConfiguration(configuration)
                .UseStartup<Startup>()
                .Build();
        }
    }
}
