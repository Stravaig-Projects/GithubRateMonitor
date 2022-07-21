using System.Net.Http.Headers;
using GithubRateMonitor;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Refit;
using Stravaig.Configuration.Diagnostics.Logging;

await CreateHostBuilder(args).RunConsoleAsync();


IHostBuilder CreateHostBuilder(string[] args)
{
    var hostBuilder = Host.CreateDefaultBuilder(args);

    hostBuilder
        .ConfigureLogging(ConfigureLogging)
        .ConfigureServices(ConfigureServices)
        .ConfigureAppConfiguration(ConfigureAppConfiguration);

    return hostBuilder;
}

void ConfigureLogging(HostBuilderContext ctx, ILoggingBuilder builder)
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Warning);
}

void ConfigureAppConfiguration(HostBuilderContext ctx, IConfigurationBuilder builder)
{
    builder.AddCommandLine(args);
}

void ConfigureServices(HostBuilderContext ctx, IServiceCollection services)
{
    services.AddSingleton(p => (IConfigurationRoot) ctx.Configuration);
    services.Configure<GitHubApiSettings>(ctx.Configuration.GetSection("GitHubApi"));
    services.Configure<ApplicationSettings>(ctx.Configuration.GetSection("Application"));
    services.AddHostedService<ApplicationHost>();
    services.AddRefitClient<IGithubRateLimitApi>()
        .ConfigureHttpClient((p, c) =>
        {
            try
            {
                var settings = p.GetRequiredService<IOptions<GitHubApiSettings>>().Value;
                var baseUrl = settings.BaseUrl;
                c.BaseAddress = new Uri(baseUrl);
                c.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));

                if (string.IsNullOrWhiteSpace(settings.Token))
                    throw new InvalidOperationException(
                        "You must supply a token on the command line --GitHubApi:Token=<user-name>");

                c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("token", settings.Token);
                if (string.IsNullOrWhiteSpace(settings.UserName))
                    throw new InvalidOperationException(
                        "You must supply a user name on the command line --GitHubApi:UserName=<user-name>");
                
                c.DefaultRequestHeaders.Add("User-Agent", settings.UserName);
            }
            catch (Exception)
            {
                var configRoot = p.GetRequiredService<IConfigurationRoot>();
                var logger = p.GetRequiredService<ILoggerFactory>().CreateLogger<Program>();
                logger.LogProviders(configRoot, LogLevel.Warning);
                logger.LogConfigurationValues(configRoot, LogLevel.Warning);
                throw;
            }
        });
}