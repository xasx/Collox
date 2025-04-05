using System.ClientModel;
using Microsoft.Extensions.AI;
using OpenAI.Chat;

namespace Collox.Services;

public class AIApis
{
    public OllamaChatClient OllamaChat { get; private set; }

    public OpenAIChatClient OpenAIChat { get; private set; }

    private void Init()
    {
        if (AppHelper.Settings.IsOllamaEnabled)
        {
            OllamaChat = new OllamaChatClient(AppHelper.Settings.OllamaEndpoint, AppHelper.Settings.OllamaModelId);
        }

        if (AppHelper.Settings.IsOpenAIEnabled)
        {
            OpenAIChat = new OpenAIChatClient(new ChatClient(AppHelper.Settings.OpenAIModelId,
                new ApiKeyCredential(AppHelper.Settings.OpenAIApiKey),
                new OpenAI.OpenAIClientOptions() { Endpoint = new Uri(AppHelper.Settings.OpenAIEndpoint) }));
        }
    }
}
