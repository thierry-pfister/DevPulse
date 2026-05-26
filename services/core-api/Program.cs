using DevPulse.Api.Auth;
using DevPulse.Infrastructure;
using FluentMigrator.Runner;
using Hangfire;
using Microsoft.AspNetCore.Authentication;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHangfireServer();
builder.Services
    .AddAuthentication(ApiKeyAuthenticationHandler.SchemeName)
    .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(ApiKeyAuthenticationHandler.SchemeName, _ => { });
builder.Services.AddAuthorization();
builder.Services.AddControllers();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
    scope.ServiceProvider.GetRequiredService<IMigrationRunner>().MigrateUp();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.UseHangfireDashboard("/hangfire");
app.UseDevPulseJobs(builder.Configuration);
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
