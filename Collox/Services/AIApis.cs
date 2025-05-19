using OllamaSharp;
using OpenAI;
using System.ClientModel;

namespace Collox.Services;

public class AIApis
{
    public OllamaApiClient Ollama { get; private set; }

    public OpenAIClient OpenAI { get; private set; }

    public void Init()
    {
        Ollama = new OllamaApiClient(Settings.OllamaEndpoint);
        OpenAI = new OpenAIClient(new ApiKeyCredential(Settings.OpenAIApiKey),
            new OpenAIClientOptions { Endpoint = new Uri(Settings.OpenAIEndpoint) });
    }
}
