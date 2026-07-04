using System.Text.Json;

namespace Cursus.PL;

/// <summary>
/// JSON helpers for embedding model data in Razor views (camelCase for JavaScript).
/// </summary>
public static class ViewJson
{
    public static readonly JsonSerializerOptions CamelCase = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static string Serialize<T>(T value) => JsonSerializer.Serialize(value, CamelCase);
}
