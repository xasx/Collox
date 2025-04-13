using System.ClientModel;
using Microsoft.Extensions.AI;
using OpenAI;
using OpenAI.Chat;

namespace Collox.Services;

public class AIApis
{
    public OllamaChatClient OllamaChat { get; private set; }

    public OpenAIChatClient OpenAIChat { get; private set; }

    private void Init()
    {
        if (Settings.IsOllamaEnabled)
        {
            OllamaChat = new OllamaChatClient(Settings.OllamaEndpoint, Settings.OllamaModelId);
        }

        if (Settings.IsOpenAIEnabled)
        {
            OpenAIChat = new OpenAIChatClient(new ChatClient(Settings.OpenAIModelId,
                new ApiKeyCredential(Settings.OpenAIApiKey),
                new OpenAIClientOptions { Endpoint = new Uri(Settings.OpenAIEndpoint) }));
        }
    }
}
