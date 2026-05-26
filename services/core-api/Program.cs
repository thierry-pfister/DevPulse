using DevPulse.Infrastructure;
using FluentMigrator.Runner;
using Hangfire;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHangfireServer();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
    scope.ServiceProvider.GetRequiredService<IMigrationRunner>().MigrateUp();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.UseHangfireDashboard("/hangfire");

app.Run();
