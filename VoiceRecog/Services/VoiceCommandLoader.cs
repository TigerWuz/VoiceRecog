using System.IO;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace VoiceRecog.Services;

public static class VoiceCommandLoader
{
    public static List<VoiceCommand> Load(string filePath)
    {
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

        using var reader = new StreamReader(filePath);

        var config = deserializer.Deserialize<VoiceCommandConfig>(reader);

        Logger.Log($"Loaded {config.Commands.Count} commands.");

        return config?.Commands ?? new List<VoiceCommand>();
    }
}
