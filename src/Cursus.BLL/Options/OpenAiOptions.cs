namespace Cursus.BLL.Options;

/// <summary>
/// Configuration used by the OpenAI-backed advisor client.
/// </summary>
public sealed class OpenAiOptions
{
    public const string SectionName = "OpenAi";

    public string ApiKey { get; init; } = string.Empty;
    public string BaseUrl { get; init; } = string.Empty;
    public string Model { get; init; } = "gpt-4o-mini";
    public int MaxOutputTokenCount { get; init; } = 500;
    public float Temperature { get; init; } = 0.3f;
    public float TopP { get; init; } = 0.9f;
}
