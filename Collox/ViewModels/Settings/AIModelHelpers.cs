using System.ClientModel;
using OllamaSharp;
using OpenAI.Models;

namespace Collox.ViewModels;

internal static class AIModelHelpers
{

    public static async Task<IEnumerable<string>> GetOllamaModels(CancellationToken cancellationToken = default)
    {
        try
        {
            using var client = new OllamaApiClient(Settings.OllamaEndpoint);
            var models = await client.ListLocalModelsAsync().ConfigureAwait(false);
            return models?.Select(m => m.Name) ?? Enumerable.Empty<string>();
        }
        catch (HttpRequestException ex)
        {
            throw new InvalidOperationException($"Failed to connect to Ollama at {Settings.OllamaEndpoint}. Please check if Ollama is running.", ex);
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            throw new TimeoutException($"Timeout connecting to Ollama at {Settings.OllamaEndpoint}", ex);
        }
    }

    public static async Task<IEnumerable<string>> GetOpenAIModels(CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(Settings.OpenAIApiKey))
                throw new InvalidOperationException("OpenAI API key is not configured");

            var client = new OpenAIModelClient(
                new ApiKeyCredential(Settings.OpenAIApiKey),
                new OpenAI.OpenAIClientOptions() { Endpoint = new Uri(Settings.OpenAIEndpoint) });

            var res = await client.GetModelsAsync(cancellationToken).ConfigureAwait(false);
            return res.Value?.Select(m => m.Id) ?? Enumerable.Empty<string>();
        }
        catch (ClientResultException ex) when (ex.Status == 401)
        {
            throw new UnauthorizedAccessException("Invalid OpenAI API key", ex);
        }
        catch (ClientResultException ex) when (ex.Status == 429)
        {
            throw new InvalidOperationException("OpenAI API rate limit exceeded", ex);
        }
    }
}
