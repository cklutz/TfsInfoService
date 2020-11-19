using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace TfsInfoService
{
    public class Startup
    {
        private readonly ILogger m_logger;

        public Startup(IConfiguration configuration, IWebHostEnvironment env, ILoggerFactory loggerFactory)
        {
            Configuration = configuration;
            loggerFactory.AddFile(Path.Combine(env.ContentRootPath, @"logs\log-{Date}.txt"));
            //TODO: loggerFactory.AddEventLog(LogLevel.Error);
            m_logger = loggerFactory.CreateLogger(typeof(Startup));

            m_logger.LogInformation("Starting...");
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddOptions();
            services.Configure<TfsOptions>(Configuration.GetSection("tfs"));
            services.AddMvc(o =>
            {
                o.EnableEndpointRouting = false;
            });

            services.AddHttpsRedirection(options =>
            {
                options.RedirectStatusCode = StatusCodes.Status307TemporaryRedirect;
                options.HttpsPort = 5001;
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILoggerFactory loggerFactory)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();
            app.UseMvc();
        }

    }
}