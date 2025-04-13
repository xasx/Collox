using System.ClientModel;
using OllamaSharp;
using OpenAI;

namespace Collox.Services;

public class AIApis
{
    public OllamaApiClient Ollama { get; private set; }

    public OpenAIClient OpenAI { get; private set; }

    private void Init()
    {
        if (Settings.IsOllamaEnabled)
        {
            Ollama = new OllamaApiClient(Settings.OllamaEndpoint);
        }

        if (Settings.IsOpenAIEnabled)
        {
            OpenAI = new OpenAIClient(new ApiKeyCredential(Settings.OpenAIApiKey),
                new OpenAIClientOptions { Endpoint = new Uri(Settings.OpenAIEndpoint) });
        }
    }
}
