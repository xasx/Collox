using Microsoft.Extensions.AI;

public interface IChatClientManager
{
    Task<IEnumerable<string>> AvailableModels { get; }
    IChatClient GetChatClient(string modelId);
}
