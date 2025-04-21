using Microsoft.Extensions.AI;

namespace Collox.Services;
public class AIService(AIApis apis)
{
    public void Init()
    {
        apis.Init();
    }

    public IChatClient GetChatClient(ApiType apiType, string modelId)
    {
        switch (apiType)
        {
            case ApiType.Ollama:
                apis.Ollama.SelectedModel = modelId;
                return apis.Ollama;
            case ApiType.OpenAI:
                return apis.OpenAI.GetChatClient(modelId).AsIChatClient();
            default:
                throw new NotSupportedException($"API type {apiType} is not supported.");
        }
    }
}
public enum ApiType
{
    Ollama,
    OpenAI
}
