using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace DevPulse.Infrastructure.Config;

public class TopicConfigLoader
{
    private readonly string _path;
    private readonly IDeserializer _deserializer;

    public TopicConfigLoader(string path)
    {
        _path = path;
        _deserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();
    }

    public IReadOnlyList<TopicEntry> Load()
    {
        var yaml = File.ReadAllText(_path);
        var config = _deserializer.Deserialize<TopicConfig>(yaml);
        return config.Topics ?? [];
    }
}

file record TopicConfig
{
    public List<TopicEntry>? Topics { get; init; }
}
