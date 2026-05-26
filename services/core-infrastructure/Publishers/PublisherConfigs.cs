namespace DevPulse.Infrastructure.Publishers;

public record PfstrCoreConfig
{
    public string BaseUrl { get; init; } = "";
    public string ApiKey  { get; init; } = "";
}

public record DevToConfig
{
    public string ApiKey { get; init; } = "";
}
