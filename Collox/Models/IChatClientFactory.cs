using Microsoft.Extensions.AI;

public interface IChatClientFactory
{
    Task<IEnumerable<string>> AvailableModels { get; }

    IChatClient CreateClient(string modelId);
}
