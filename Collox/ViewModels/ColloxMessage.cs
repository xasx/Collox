using System.Collections.ObjectModel;

namespace Collox.ViewModels;

public partial class ColloxMessage : ObservableObject
{
    private static readonly MessageRelativeTimeUpdater timeUpdater = new();

    protected ColloxMessage()
    {
        timeUpdater.RegisterMessage(this);
    }

    [ObservableProperty] public partial TimeSpan RelativeTimestamp { get; set; } = TimeSpan.Zero;

    public DateTime Timestamp { get; init; }
}

public partial class TextColloxMessage : ColloxMessage
{
    [ObservableProperty] public partial string Text { get; set; }

    [ObservableProperty] public partial ObservableCollection<ColloxMessageComment> Comments { get; set; } = [];

    [ObservableProperty] public partial string ErrorMessage { get; set; }

    [ObservableProperty] public partial bool IsLoading { get; set; }

    [ObservableProperty] public partial bool IsGenerated { get; set; } = false;

    [ObservableProperty] public partial Guid GeneratorId { get; set; }

    [ObservableProperty] public partial bool HasProcessingError { get; set; } = false;

    public string Context { get; init; }

    [RelayCommand]
    public void Read() { WriteViewModel.ReadText(Text, Settings.Voice); }
}

public partial class TimeColloxMessage : ColloxMessage
{
    public TimeSpan Time { get; init; }
}

public partial class InternalColloxMessage : ColloxMessage
{
    public string Message { get; set; }

    public InfoBarSeverity Severity { get; set; }
}

public partial class ColloxMessageComment : ObservableObject
{
    [ObservableProperty] public partial string Comment { get; set; }

    [ObservableProperty] public partial Guid GeneratorId { get; set; }
}
