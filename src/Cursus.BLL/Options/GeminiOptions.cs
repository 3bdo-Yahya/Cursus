namespace Cursus.BLL.Options;

public sealed class GeminiOptions
{
    public const string SectionName = "Gemini";

    public string ApiKey { get; init; } = string.Empty;
    public string Model { get; init; } = "models/gemini-2.5-flash";
    public float Temperature { get; init; } = 0.3f;
    public int MaxOutputTokens { get; init; } = 1000;
}
