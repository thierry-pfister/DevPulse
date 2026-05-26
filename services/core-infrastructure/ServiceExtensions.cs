using DevPulse.Application.Episodes;
using DevPulse.Application.Generation;
using DevPulse.Application.Publishing;
using DevPulse.Infrastructure.Claude;
using DevPulse.Infrastructure.Config;
using DevPulse.Infrastructure.Data;
using DevPulse.Infrastructure.Episodes;
using DevPulse.Infrastructure.Migrations;
using DevPulse.Infrastructure.Notifications;
using DevPulse.Infrastructure.Publishers;
using DevPulse.Infrastructure.Scheduling;
using DevPulse.Infrastructure.TopicQueue;
using FluentMigrator.Runner;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DevPulse.Infrastructure;

public static class ServiceExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration          configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("Postgres connection string is required.");

        var topicConfigPath = configuration["TopicConfig:Path"] ?? "topic_config.yaml";

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IEpisodeRepository, EpisodeRepository>();
        services.AddScoped<ITopicQueueRepository, TopicQueueRepository>();

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

        RegisterClaude(services, configuration);
        RegisterNotifier(services, configuration);
        RegisterPublishers(services, configuration);
        RegisterSchedulingJobs(services, configuration);

        return services;
    }

    public static IApplicationBuilder UseDevPulseJobs(
        this IApplicationBuilder app,
        IConfiguration           configuration)
    {
        var config = configuration.GetSection("Scheduling").Get<SchedulingConfig>()
            ?? new SchedulingConfig();

        RecurringJob.AddOrUpdate<TopicSchedulerJob>(
            "topic-scheduler",
            j => j.Execute(),
            config.DailyJobCron);

        RecurringJob.AddOrUpdate<BacklogTopperJob>(
            "backlog-topper",
            j => j.Execute(),
            config.BacklogJobCron);

        return app;
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private static void RegisterClaude(IServiceCollection services, IConfiguration configuration)
    {
        var claudeConfig = configuration.GetSection("Claude").Get<ClaudeConfig>()
            ?? new ClaudeConfig();

        services.AddSingleton(claudeConfig);

        services.AddHttpClient<ClaudeClient>(client =>
        {
            client.BaseAddress = new Uri("https://api.anthropic.com/");
            client.DefaultRequestHeaders.Add("x-api-key", claudeConfig.ApiKey);
            client.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
        });

        services.AddTransient<IClaudeClient>(sp => sp.GetRequiredService<ClaudeClient>());
    }

    private static void RegisterNotifier(IServiceCollection services, IConfiguration configuration)
    {
        var resendConfig = configuration.GetSection("Resend").Get<ResendConfig>()
            ?? new ResendConfig();

        services.AddSingleton(resendConfig);

        services.AddHttpClient<ResendNotifier>(client =>
        {
            client.BaseAddress = new Uri("https://api.resend.com/");
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {resendConfig.ApiKey}");
        });

        services.AddTransient<IEmailNotifier>(sp => sp.GetRequiredService<ResendNotifier>());
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

    private static void RegisterSchedulingJobs(IServiceCollection services, IConfiguration configuration)
    {
        var schedulingConfig = configuration.GetSection("Scheduling").Get<SchedulingConfig>()
            ?? new SchedulingConfig();

        services.AddSingleton(schedulingConfig);

        services.AddScoped<TopicSchedulerJob>();
        services.AddScoped<GenerateEpisodeJob>();
        services.AddScoped<AutoPublishJob>();
        services.AddScoped<PublishEpisodeJob>();
        services.AddScoped<BacklogTopperJob>();
    }
}
