namespace DevPulse.Infrastructure.Scheduling;

public class SchedulingConfig
{
    public Dictionary<string, int> TagCadence               { get; set; } = new();
    public string                  DailyJobCron             { get; set; } = "0 6 * * *";
    public string                  BacklogJobCron           { get; set; } = "0 3 * * 0";
    public int                     InterventionWindowMinutes { get; set; } = 30;
    public int                     BacklogMinPerTag          { get; set; } = 5;
}
