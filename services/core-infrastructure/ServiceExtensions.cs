using DevPulse.Application.Publishing;
using DevPulse.Infrastructure.Config;
using DevPulse.Infrastructure.Data;
using DevPulse.Infrastructure.Publishers;
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

        services.AddHttpClient<DevToPublisher>(client =>
        {
            client.BaseAddress = new Uri("https://dev.to/");
            client.DefaultRequestHeaders.Add("api-key", devToConfig.ApiKey);
        });

        services.AddTransient<IPublisher>(sp => sp.GetRequiredService<PfstrCorePublisher>());
        services.AddTransient<IPublisher>(sp => sp.GetRequiredService<DevToPublisher>());
    }
}
