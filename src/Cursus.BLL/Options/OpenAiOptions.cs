namespace Cursus.BLL.Options;

/// <summary>
/// Configuration used by the OpenAI-compatible advisor client.
/// </summary>
public sealed class OpenAiOptions
{
    public const string SectionName = "OpenAi";
    public const string DefaultBaseUrl = "https://openrouter.ai/api/v1";
    public const string DefaultModel = "openrouter/free";

    public string ApiKey { get; init; } = string.Empty;
    public string BaseUrl { get; init; } = DefaultBaseUrl;
    public string Model { get; init; } = DefaultModel;
    public int MaxOutputTokenCount { get; init; } = 500;
    public float Temperature { get; init; } = 0.3f;
    public float TopP { get; init; } = 0.9f;
}
