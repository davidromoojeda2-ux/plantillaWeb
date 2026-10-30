namespace Aplication.DTOs.Automation;

/// <summary>Preset de un proveedor de IA compatible con el formato OpenAI.</summary>
public sealed record AiProviderPreset(string Key, string Name, string BaseUrl, string DefaultModel);

/// <summary>Presets de proveedores de IA compatibles con el formato OpenAI.</summary>
public static class AiProviderPresets
{
    public static readonly IReadOnlyList<AiProviderPreset> All =
    [
        new("openai", "OpenAI", "https://api.openai.com/v1", "gpt-4o-mini"),
        new("deepseek", "DeepSeek", "https://api.deepseek.com", "deepseek-chat"),
        new("openrouter", "OpenRouter", "https://openrouter.ai/api/v1", "meta-llama/llama-3.3-70b-instruct"),
        new("groq", "Groq", "https://api.groq.com/openai/v1", "llama-3.3-70b-versatile"),
        new("mistral", "Mistral", "https://api.mistral.ai/v1", "mistral-small-latest"),
        new("ollama", "Ollama (local)", "http://localhost:11434/v1", "llama3"),
        new("custom", "Personalizado", "", "")
    ];

    public static AiProviderPreset? Find(string? key) =>
        All.FirstOrDefault(p => string.Equals(p.Key, key, StringComparison.OrdinalIgnoreCase));
}
