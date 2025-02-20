namespace Collox.Models;

public class MarkdownRecording
{
    public DateOnly Date { get; set; }

    public string Preview { get; set; }

    public Func<string> Content { get; set; }
}
