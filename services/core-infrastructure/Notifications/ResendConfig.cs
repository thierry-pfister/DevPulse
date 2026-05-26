namespace DevPulse.Infrastructure.Notifications;

public class ResendConfig
{
    public string ApiKey    { get; set; } = "";
    public string FromEmail { get; set; } = "devpulse@thierrypfister.dev";
    public string ToEmail   { get; set; } = "";
}
