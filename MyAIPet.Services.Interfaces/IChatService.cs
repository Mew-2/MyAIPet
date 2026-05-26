using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace MyAIPet.Services.Interfaces;

public interface IChatService
{
    Task<List<string>> SendMessageWithHistoryAsync(string userMessage, CancellationToken cancellationToken = default);
    void SetPersonalityPrompt(string prompt);
    void ClearHistory();
}

public class ChatMessage
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;
}

public class ChatRequest
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = "kimi-k2.5";

    [JsonPropertyName("messages")]
    public List<ChatMessage> Messages { get; set; } = new();

    [JsonPropertyName("max_completion_tokens")]
    public int MaxCompletionTokens { get; set; } = 2048;

    [JsonPropertyName("top_p")]
    public double TopP { get; set; } = 0.95;

    [JsonPropertyName("n")]
    public int N { get; set; } = 1;

    [JsonPropertyName("thinking")]
    public ThinkingConfig Thinking { get; set; } = new() { Type = "enabled" };
}

public class ThinkingConfig
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "enabled";
}

public class ChatResponse
{
    [JsonPropertyName("choices")]
    public List<Choice> Choices { get; set; } = new();
}

public class Choice
{
    [JsonPropertyName("message")]
    public MessageDelta Message { get; set; } = new();
}

public class MessageDelta
{
    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;
}
