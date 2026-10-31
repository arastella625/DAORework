using System.Text.Json;
using System.Text.Json.Serialization;

namespace DAORework.Infrastructure.Persistence;

/// <summary>The one JSON configuration for every persisted document (SDD §8.4, audit B3): camelCase, indented, enums as strings.</summary>
public static class JsonDefaults
{
    public static JsonSerializerOptions Options { get; } = Create();

    private static JsonSerializerOptions Create()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true };
        options.Converters.Add(new JsonStringEnumConverter());
        options.MakeReadOnly(populateMissingResolver: true);
        return options;
    }
}
