using Microsoft.Extensions.AI;

public interface IChatClientManager
{
    Task<IEnumerable<string>> AvailableModels { get; }
    Task<IChatClient> GetChatClientAsync(string modelId);
}
