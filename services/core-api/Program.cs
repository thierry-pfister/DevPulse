using DevPulse.Infrastructure;
using Hangfire;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Postgres")
    ?? throw new InvalidOperationException("Postgres connection string is required.");

var topicConfigPath = builder.Configuration["TopicConfig:Path"] ?? "topic_config.yaml";

builder.Services.AddInfrastructure(connectionString, topicConfigPath);
builder.Services.AddHangfireServer();

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.UseHangfireDashboard("/hangfire");

app.Run();
