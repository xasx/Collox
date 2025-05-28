namespace Collox.ViewModels;

public class HistoryEntry
{
    public Lazy<string> Content { get; init; }

    public DateOnly Day { get; init; }

    public string Preview { get; init; }
}
