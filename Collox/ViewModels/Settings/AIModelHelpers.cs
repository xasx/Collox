using System.ClientModel;
using OllamaSharp;
using OpenAI.Models;

namespace Collox.ViewModels;

internal static class AIModelHelpers
{

    public static async Task<IEnumerable<string>> GetOllamaModels()
    {
        using var client = new OllamaApiClient(Settings.OllamaEndpoint);
        var models = await client.ListLocalModelsAsync();
        return models.Select(m => m.Name);
    }

    public static async Task<IEnumerable<string>> GetOpenAIModels()
    {
        var client = new OpenAIModelClient(
            new ApiKeyCredential(Settings.OpenAIApiKey),
            new OpenAI.OpenAIClientOptions() { Endpoint = new Uri(Settings.OpenAIEndpoint) });
        var res = await client.GetModelsAsync();
        var models = res.Value;
        return models.Select(m => m.Id);
    }
}
