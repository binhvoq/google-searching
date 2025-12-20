using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace GoogleSearching.Api.Services;

public class AzureOpenAIRequestException : Exception
{
    public AzureOpenAIRequestException(string message, int statusCode, int? retryAfterSeconds, string? bodyPreview, Exception? innerException = null)
        : base(message, innerException)
    {
        StatusCode = statusCode;
        RetryAfterSeconds = retryAfterSeconds;
        BodyPreview = bodyPreview;
    }

    public int StatusCode { get; }
    public int? RetryAfterSeconds { get; }
    public string? BodyPreview { get; }
}

public class AzureOpenAIChatClient
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AzureOpenAIChatClient> _logger;

    public AzureOpenAIChatClient(HttpClient httpClient, IConfiguration configuration, ILogger<AzureOpenAIChatClient> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    private AzureOpenAIOptions GetOptions()
    {
        var options = new AzureOpenAIOptions();
        _configuration.GetSection("AzureOpenAI").Bind(options);

        // Allow env-var style keys without appsettings section (common on Azure App Service)
        options.Endpoint = options.Endpoint.Trim();
        options.DeploymentName = options.DeploymentName.Trim();
        options.ApiVersion = string.IsNullOrWhiteSpace(options.ApiVersion) ? "2024-08-01-preview" : options.ApiVersion.Trim();

        return options;
    }

    public async Task<AzureChatCompletion> CreateChatCompletionAsync(object payload, CancellationToken cancellationToken)
    {
        var options = GetOptions();
        if (string.IsNullOrWhiteSpace(options.Endpoint) ||
            string.IsNullOrWhiteSpace(options.ApiKey) ||
            string.IsNullOrWhiteSpace(options.DeploymentName))
        {
            throw new InvalidOperationException("Missing AzureOpenAI configuration (Endpoint/ApiKey/DeploymentName).");
        }

        var endpoint = options.Endpoint.TrimEnd('/');
        var url = $"{endpoint}/openai/deployments/{Uri.EscapeDataString(options.DeploymentName)}/chat/completions?api-version={Uri.EscapeDataString(options.ApiVersion)}";

        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Add("api-key", options.ApiKey);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var statusCode = (int)response.StatusCode;
            var retryAfterSeconds = TryGetRetryAfterSeconds(response);
            var bodyPreview = json.Length <= 3000 ? json : $"{json[..3000]}…(truncated)";
            _logger.LogWarning("Azure OpenAI error {Status}. retryAfterSec={RetryAfterSec} body={Body}",
                statusCode, retryAfterSeconds, bodyPreview);
            throw new AzureOpenAIRequestException(
                $"Azure OpenAI request failed: {statusCode}.",
                statusCode,
                retryAfterSeconds,
                bodyPreview);
        }

        return AzureChatCompletion.Parse(json);
    }

    private static int? TryGetRetryAfterSeconds(HttpResponseMessage response)
    {
        // Azure may return Retry-After, retry-after-ms, or x-ms-retry-after-ms.
        if (response.Headers.TryGetValues("Retry-After", out var retryAfterValues))
        {
            var v = retryAfterValues.FirstOrDefault();
            if (int.TryParse(v, out var sec) && sec >= 0) return sec;
        }

        if (response.Headers.TryGetValues("retry-after-ms", out var retryAfterMsValues) ||
            response.Headers.TryGetValues("x-ms-retry-after-ms", out retryAfterMsValues))
        {
            var v = retryAfterMsValues.FirstOrDefault();
            if (int.TryParse(v, out var ms) && ms >= 0) return (int)Math.Ceiling(ms / 1000.0);
        }

        return null;
    }
}

public class AzureChatCompletion
{
    public string? AssistantContent { get; set; }
    public List<AzureToolCall> ToolCalls { get; set; } = new();

    public static AzureChatCompletion Parse(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var message = root
            .GetProperty("choices")[0]
            .GetProperty("message");

        var result = new AzureChatCompletion
        {
            AssistantContent = message.TryGetProperty("content", out var contentEl) ? contentEl.GetString() : null
        };

        if (message.TryGetProperty("tool_calls", out var toolCallsEl) && toolCallsEl.ValueKind == JsonValueKind.Array)
        {
            foreach (var tc in toolCallsEl.EnumerateArray())
            {
                var function = tc.GetProperty("function");
                result.ToolCalls.Add(new AzureToolCall
                {
                    Id = tc.GetProperty("id").GetString() ?? string.Empty,
                    Name = function.GetProperty("name").GetString() ?? string.Empty,
                    ArgumentsJson = function.GetProperty("arguments").GetString() ?? "{}",
                });
            }
        }

        return result;
    }
}

public class AzureToolCall
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ArgumentsJson { get; set; } = "{}";
}
