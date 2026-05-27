using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using MyAIPet.Services.Interfaces;

namespace MyAIPet.Services;

public class BackendChatService : IChatService
{
    private readonly HttpClient _httpClient;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };
    private const int TimeoutSeconds = 120;

    public event Action<string, int>? OnEmotionChanged;

    public BackendChatService(string baseUrl = "http://localhost:8000")
    {
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(baseUrl.TrimEnd('/')),
            Timeout = TimeSpan.FromSeconds(TimeoutSeconds)
        };
    }

    public void SetPersonalityPrompt(string prompt)
    {
    }

    public void ClearHistory()
    {
    }

    public async Task<List<string>> SendMessageWithHistoryAsync(string userMessage, CancellationToken cancellationToken = default)
    {
        var payload = JsonSerializer.Serialize(
            new { msg = userMessage, session_id = "default" }, JsonOptions);
        var content = new StringContent(payload, Encoding.UTF8, "application/json");

        using var response = await _httpClient.PostAsync("/chat/stream", content, cancellationToken);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        var fullText = new StringBuilder();

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var line = await reader.ReadLineAsync(cancellationToken);
            if (line is null) break;
            if (string.IsNullOrWhiteSpace(line)) continue;

            var dict = JsonSerializer.Deserialize<Dictionary<string, object?>>(line);
            if (dict == null) continue;

            if (!TryGetString(dict, "type", out var type)) continue;

            switch (type)
            {
                case "meta":
                    var emotion = TryGetString(dict, "emotion", out var em) ? em : "normal";
                    var affection = TryGetInt(dict, "affection", out var aff) ? aff : 0;
                    OnEmotionChanged?.Invoke(emotion, affection);
                    break;

                case "text":
                    if (TryGetString(dict, "content", out var textContent))
                        fullText.Append(textContent);
                    break;

                case "done":
                    goto endLoop;
            }
        }
        endLoop:

        var reply = fullText.ToString();
        return string.IsNullOrWhiteSpace(reply)
            ? new List<string> { GetFallbackReply() }
            : SplitSentences(reply);
    }

    private static bool TryGetString(Dictionary<string, object?> dict, string key, out string value)
    {
        if (dict.TryGetValue(key, out var obj) && obj != null)
        {
            if (obj is string s) { value = s; return true; }
            if (obj is JsonElement { ValueKind: JsonValueKind.String } je)
            {
                value = je.GetString() ?? string.Empty;
                return true;
            }
        }
        value = string.Empty;
        return false;
    }

    private static bool TryGetInt(Dictionary<string, object?> dict, string key, out int value)
    {
        if (dict.TryGetValue(key, out var obj) && obj != null)
        {
            if (obj is int i) { value = i; return true; }
            if (obj is long l) { value = (int)l; return true; }
            if (obj is JsonElement { ValueKind: JsonValueKind.Number } je)
            {
                value = je.GetInt32();
                return true;
            }
        }
        value = 0;
        return false;
    }

    private static string GetFallbackReply()
    {
        return "希格雯的连接断开了，主人检查一下后端有没有启动哦~";
    }

    private static List<string> SplitSentences(string text)
    {
        var sentences = new List<string>();
        var current = new StringBuilder();

        foreach (var c in text)
        {
            current.Append(c);

            if (c is '。' or '！' or '？' or ';' or '\n')
            {
                if (current.Length > 0)
                {
                    sentences.Add(current.ToString().Trim());
                    current.Clear();
                }
            }
        }

        if (current.Length > 0)
        {
            sentences.Add(current.ToString().Trim());
        }

        if (sentences.Count == 0)
        {
            sentences.Add(text);
        }

        return sentences;
    }
}
