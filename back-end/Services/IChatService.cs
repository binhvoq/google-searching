using GoogleSearching.Api.Models;

namespace GoogleSearching.Api.Services;

public interface IChatService
{
    Task<ChatResponse> SendAsync(ChatRequest request, CancellationToken cancellationToken);
}

