namespace Collox.Models;

public class IntelligentProcessor
{
    public Guid Id { get; set; }

    public string Name { get; set; }

    public bool IsEnabled { get; set; }

    public AIProvider Provider { get; set; }

    public string Prompt { get; set; }

    public string ModelId { get; set; }

    public Target Target { get; set; }

    public Guid FallbackId { get; set; }
}

public enum Target
{
    Comment,
    Task,
    Context,
    Chat
}

public enum AIProvider
{
    Ollama,
    OpenAI
}
