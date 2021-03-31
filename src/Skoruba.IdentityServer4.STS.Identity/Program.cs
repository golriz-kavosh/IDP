﻿using System;
using System.IO;
using System.Reflection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;
using Skoruba.IdentityServer4.Shared.Helpers;

namespace Skoruba.IdentityServer4.STS.Identity
{
    public class Program
    {
        private static readonly string BuildConfiguration = typeof(Program)
            .Assembly
            .GetCustomAttribute<AssemblyConfigurationAttribute>()?
            .Configuration;
        
        public static void Main(string[] args)
        {
            var configuration = GetConfiguration(args);

            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .CreateLogger();
            try
            {
                DockerHelpers.ApplyDockerConfiguration(configuration);

                CreateHostBuilder(args).Build().Run();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Host terminated unexpectedly");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        private static IConfiguration GetConfiguration(string[] args)
        {
            var configurationBuilder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory());

            if (BuildConfiguration == "Debug")
                configurationBuilder
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .AddJsonFile("serilog.json", optional: true, reloadOnChange: true);
            else
                configurationBuilder
                    .AddJsonFile($"appsettings.{BuildConfiguration}.json", optional: false, reloadOnChange: true)
                    .AddJsonFile($"serilog.{BuildConfiguration}.json", optional: true, reloadOnChange: true);

            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            if (environment == Environments.Development)
                configurationBuilder.AddUserSecrets<Startup>();

            var configuration = configurationBuilder.Build();

            configuration.AddAzureKeyVaultConfiguration(configurationBuilder);

            configurationBuilder.AddCommandLine(args);
            configurationBuilder.AddEnvironmentVariables();

            return configurationBuilder.Build();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                 .ConfigureAppConfiguration((hostContext, configApp) =>
                 {
                     if (BuildConfiguration == "Debug")
                         configApp
                             .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                             .AddJsonFile("serilog.json", optional: true, reloadOnChange: true);
                     else
                         configApp
                             .AddJsonFile($"appsettings.{BuildConfiguration}.json", optional: false, reloadOnChange: true)
                             .AddJsonFile($"serilog.{BuildConfiguration}.json", optional: true, reloadOnChange: true);

                     if (hostContext.HostingEnvironment.IsDevelopment())
                         configApp.AddUserSecrets<Startup>();

                     var configurationRoot = configApp.Build();
                     configurationRoot.AddAzureKeyVaultConfiguration(configApp);

                     configApp.AddEnvironmentVariables();
                     configApp.AddCommandLine(args);
                 })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.ConfigureKestrel(options => options.AddServerHeader = false);
                    webBuilder.UseStartup<Startup>();
                })
                .UseSerilog((hostContext, loggerConfig) =>
                {
                    loggerConfig
                        .ReadFrom.Configuration(hostContext.Configuration)
                        .Enrich.WithProperty("ApplicationName", hostContext.HostingEnvironment.ApplicationName);
                });
    }
}