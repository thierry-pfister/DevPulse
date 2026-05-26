using DevPulse.Infrastructure;
using Hangfire;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHangfireServer();

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.UseHangfireDashboard("/hangfire");

app.Run();
