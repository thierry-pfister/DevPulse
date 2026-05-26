using DevPulse.Application.Episodes;
using DevPulse.Application.Publishing;
using DevPulse.Infrastructure.Config;
using DevPulse.Infrastructure.Data;
using DevPulse.Infrastructure.Episodes;
using DevPulse.Infrastructure.Migrations;
using DevPulse.Infrastructure.Publishers;
using FluentMigrator.Runner;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DevPulse.Infrastructure;

public static class ServiceExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("Postgres connection string is required.");

        var topicConfigPath = configuration["TopicConfig:Path"] ?? "topic_config.yaml";

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IEpisodeRepository, EpisodeRepository>();

        services.AddFluentMigratorCore()
            .ConfigureRunner(runner => runner
                .AddPostgres()
                .WithGlobalConnectionString(connectionString)
                .ScanIn(typeof(M20260526001_CreateEpisodesTable).Assembly).For.Migrations())
            .AddLogging(lb => lb.AddFluentMigratorConsole());

        services.AddHangfire(config =>
            config.UsePostgreSqlStorage(c =>
                c.UseNpgsqlConnection(connectionString)));

        services.AddSingleton(new TopicConfigLoader(topicConfigPath));

        RegisterPublishers(services, configuration);

        return services;
    }

    private static void RegisterPublishers(IServiceCollection services, IConfiguration configuration)
    {
        var pfstrConfig = configuration.GetSection("Publishers:PfstrCore").Get<PfstrCoreConfig>()
            ?? new PfstrCoreConfig();

        var devToConfig = configuration.GetSection("Publishers:DevTo").Get<DevToConfig>()
            ?? new DevToConfig();

        services.AddHttpClient<PfstrCorePublisher>(client =>
        {
            client.BaseAddress = new Uri(pfstrConfig.BaseUrl.TrimEnd('/') + "/");
            client.DefaultRequestHeaders.Add("X-Api-Key", pfstrConfig.ApiKey);
        });

        services.Configure<DevToConfig>(configuration.GetSection("Publishers:DevTo"));

        services.AddHttpClient<DevToPublisher>(client =>
        {
            client.BaseAddress = new Uri("https://dev.to/");
            if (!string.IsNullOrWhiteSpace(devToConfig.ApiKey))
                client.DefaultRequestHeaders.Add("api-key", devToConfig.ApiKey);
        });

        services.AddTransient<IPublisher>(sp => sp.GetRequiredService<PfstrCorePublisher>());
        services.AddTransient<IPublisher>(sp => sp.GetRequiredService<DevToPublisher>());
    }
}
