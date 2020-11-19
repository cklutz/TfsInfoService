using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.WindowsServices;
using Microsoft.Extensions.Configuration;

namespace TfsInfoService
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            if (Environment.UserInteractive || !RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
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
                .UseKestrel()
                .UseStartup<Startup>()
                .Build();
        }
    }
}
