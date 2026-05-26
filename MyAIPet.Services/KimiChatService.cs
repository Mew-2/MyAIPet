using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using MyAIPet.Services.Interfaces;

namespace MyAIPet.Services;

public class KimiChatService : IChatService
{
    private readonly HttpClient _httpClient;
    private readonly List<ChatMessage> _messageHistory = new();
    private readonly IMemoryService? _memoryService;
    private string _personalityPrompt = string.Empty;
    private const string BaseUrl = "https://api.moonshot.cn/v1/";
    private const string DefaultModel = "kimi-k2.5";

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly List<string> _fallbackReplies = new()
    {
        "呜...服务器好像有点累了呢...让我休息一下再陪你玩好不好？",
        "哎呀，脑子突然转不过来了~让我缓缓嘛...",
        "哼，才不是我不理你呢，是服务器在偷懒！",
        "好累哦...刚才用脑过度了，让我撒个娇缓缓~",
        "哎呀，我的思考回路好像打结了！让我重新整理一下嘛...",
        "呜...你刚才说的话太难了，我需要时间想一想~",
        "叮！系统提示：需要补充能量中...让我撒个娇先~",
        "嘿嘿，其实我是在考验你的耐心啦~再陪我玩一下下嘛~"
    };

    private readonly Random _random = new();

    public KimiChatService(string apiKey, IMemoryService? memoryService = null, string model = DefaultModel)
    {
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(BaseUrl),
            Timeout = TimeSpan.FromSeconds(120)
        };
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        _memoryService = memoryService;
    }

    public void SetPersonalityPrompt(string prompt)
    {
        _personalityPrompt = prompt;
        RebuildSystemMessage();
    }

    private void RebuildSystemMessage()
    {
        _messageHistory.Clear();
        var systemContent = _personalityPrompt;

        if (_memoryService != null)
        {
            var memoryContext = _memoryService.FormatForContext();
            if (!string.IsNullOrEmpty(memoryContext))
            {
                systemContent += memoryContext;
            }
        }

        _messageHistory.Add(new ChatMessage { Role = "system", Content = systemContent });
    }

    public void ClearHistory()
    {
        RebuildSystemMessage();
    }

    public async Task<List<string>> SendMessageWithHistoryAsync(string userMessage, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(_personalityPrompt))
        {
            throw new InvalidOperationException("请先设置人格Prompt");
        }

        if (_messageHistory.Count == 0 || _messageHistory[0].Role != "system")
        {
            RebuildSystemMessage();
        }

        _messageHistory.Add(new ChatMessage { Role = "user", Content = userMessage });

        var request = new ChatRequest
        {
            Model = DefaultModel,
            Messages = _messageHistory,
            MaxCompletionTokens = 2048,
            TopP = 0.95,
            N = 1,
            Thinking = new ThinkingConfig { Type = "enabled" }
        };

        var reply = await SendWithRetryAsync(request, cancellationToken);

        _messageHistory.Add(new ChatMessage { Role = "assistant", Content = reply });

        var sentences = SplitSentences(reply);
        return sentences;
    }

    private async Task<string> SendWithRetryAsync(ChatRequest request, CancellationToken cancellationToken)
    {
        var maxRetries = 3;
        var baseDelayMs = 1000;

        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            try
            {
                var json = JsonSerializer.Serialize(request, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("chat/completions", content, cancellationToken);

                var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var chatResponse = JsonSerializer.Deserialize<ChatResponse>(responseJson, _jsonOptions);
                    return chatResponse?.Choices?[0]?.Message?.Content ?? GetFallbackReply();
                }

                if (responseJson.Contains("overloaded") || responseJson.Contains("rate_limit"))
                {
                    if (attempt < maxRetries - 1)
                    {
                        var delay = baseDelayMs * (int)Math.Pow(2, attempt);
                        await Task.Delay(delay, cancellationToken);
                        continue;
                    }
                    else
                    {
                        return GetFallbackReply();
                    }
                }

                throw new HttpRequestException($"API返回错误: {response.StatusCode}, 内容: {responseJson}");
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception)
            {
                if (attempt < maxRetries - 1)
                {
                    var delay = baseDelayMs * (int)Math.Pow(2, attempt);
                    await Task.Delay(delay, cancellationToken);
                }
                else
                {
                    return GetFallbackReply();
                }
            }
        }

        return GetFallbackReply();
    }

    private string GetFallbackReply()
    {
        return _fallbackReplies[_random.Next(_fallbackReplies.Count)];
    }

    private List<string> SplitSentences(string text)
    {
        var sentences = new List<string>();
        var current = new StringBuilder();

        foreach (var c in text)
        {
            current.Append(c);

            if (c == '。' || c == '！' || c == '？' || c == ';' || c == '\n')
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
