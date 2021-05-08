using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;
using Skoruba.IdentityServer4.Admin.EntityFramework.Configuration.Configuration;
using Skoruba.IdentityServer4.Admin.EntityFramework.Shared.DbContexts;
using Skoruba.IdentityServer4.Admin.EntityFramework.Shared.Entities.Identity;
using Skoruba.IdentityServer4.Admin.EntityFramework.Shared.Helpers;
using Skoruba.IdentityServer4.Shared.Configuration.Helpers;

namespace Skoruba.IdentityServer4.Admin
{
	public class Program
    {
        private const string SeedArgs = "/seed";
        private static readonly string BuildConfiguration = typeof(Program)
            .Assembly
            .GetCustomAttribute<AssemblyConfigurationAttribute>()?
            .Configuration;

        public static async Task Main(string[] args)
        {
            var configuration = GetConfiguration(args);

            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .CreateLogger();

            try
            {
                DockerHelpers.ApplyDockerConfiguration(configuration);

                var host = CreateHostBuilder(args).Build();

                await ApplyDbMigrationsWithDataSeedAsync(args, configuration, host);

                host.Run();
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

        private static async Task ApplyDbMigrationsWithDataSeedAsync(string[] args, IConfiguration configuration, IHost host)
        {
            var applyDbMigrationWithDataSeedFromProgramArguments = args.Any(x => x == SeedArgs);
            if (applyDbMigrationWithDataSeedFromProgramArguments) args = args.Except(new[] { SeedArgs }).ToArray();

            var seedConfiguration = configuration.GetSection(nameof(SeedConfiguration)).Get<SeedConfiguration>();
            var databaseMigrationsConfiguration = configuration.GetSection(nameof(DatabaseMigrationsConfiguration))
                .Get<DatabaseMigrationsConfiguration>();

            await DbMigrationHelpers
                .ApplyDbMigrationsWithDataSeedAsync<IdentityServerConfigurationDbContext, AdminIdentityDbContext,
                    IdentityServerPersistedGrantDbContext, AdminLogDbContext, AdminAuditLogDbContext,
                    IdentityServerDataProtectionDbContext, UserIdentity, UserIdentityRole>(host,
                    applyDbMigrationWithDataSeedFromProgramArguments, seedConfiguration, databaseMigrationsConfiguration);
        }

        private static IConfiguration GetConfiguration(string[] args)
        {
            var configurationBuilder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory());

            if (BuildConfiguration == "Debug")
                configurationBuilder
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true).AddJsonFile("serilog.json", optional: true, reloadOnChange: true)
                    .AddJsonFile("serilog.json", optional: true, reloadOnChange: true)
                    .AddJsonFile("identitydata.json", optional: true, reloadOnChange: true)
                    .AddJsonFile("identityserverdata.json", optional: true, reloadOnChange: true);
            else
                configurationBuilder
                    .AddJsonFile($"appsettings.{BuildConfiguration}.json", optional: false, reloadOnChange: true)
                    .AddJsonFile($"serilog.{BuildConfiguration}.json", optional: true, reloadOnChange: true)
                    .AddJsonFile($"identitydata.{BuildConfiguration}.json", optional: true, reloadOnChange: true)
                    .AddJsonFile($"identityserverdata.{BuildConfiguration}.json", optional: true, reloadOnChange: true);

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
                             .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true).AddJsonFile("serilog.json", optional: true, reloadOnChange: true)
                             .AddJsonFile("serilog.json", optional: true, reloadOnChange: true)
                             .AddJsonFile("identitydata.json", optional: true, reloadOnChange: true)
                             .AddJsonFile("identityserverdata.json", optional: true, reloadOnChange: true);
                     else
                         configApp
                             .AddJsonFile($"appsettings.{BuildConfiguration}.json", optional: false, reloadOnChange: true)
                             .AddJsonFile($"serilog.{BuildConfiguration}.json", optional: true, reloadOnChange: true)
                             .AddJsonFile($"identitydata.{BuildConfiguration}.json", optional: true, reloadOnChange: true)
                             .AddJsonFile($"identityserverdata.{BuildConfiguration}.json", optional: true, reloadOnChange: true);

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