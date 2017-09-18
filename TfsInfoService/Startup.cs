using System;
using System.IO;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace TfsInfoService
{
    public class Startup
    {
        private readonly ILogger m_logger;

        public Startup(IConfiguration configuration, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            Configuration = configuration;
            loggerFactory.AddFile(Path.Combine(env.ContentRootPath, @"logs\log-{Date}.txt"));
            loggerFactory.AddEventLog(LogLevel.Error);
            m_logger = loggerFactory.CreateLogger(typeof(Startup));

            m_logger.LogInformation("Starting...");
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddOptions();
            services.Configure<TfsOptions>(Configuration.GetSection("tfs"));
            services.AddMvc();
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseMvc();
        }
    }
}