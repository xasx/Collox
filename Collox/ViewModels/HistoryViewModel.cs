using Collox.Services;
using ColorCode.Common;
using CommunityToolkit.Mvvm.Collections;

namespace Collox.ViewModels;

public partial class HistoryViewModel : ObservableObject
{
    private readonly IStoreService storeService = App.GetService<IStoreService>();

    public ObservableGroupedCollection<string, HistoryEntry> Histories { get; set; } = [];

    [ObservableProperty] public partial HistoryEntry SelectedHistoryEntry { get; set; }

    [RelayCommand]
    public async Task LoadHistory()
    {
        Histories.Clear();
        var historyData = await storeService.Load().ConfigureAwait(true);

        foreach (var (month, historyItems) in historyData.Reverse())
        {
            foreach (var historyItem in historyItems.Reverse())
            {
                Histories.AddItem(month, new HistoryEntry
                {
                    Day = historyItem.Date,
                    Preview = historyItem.Preview,
                    Content = new Lazy<string>(historyItem.Content)
                });
            }
        }
    }
}

public class HistoryEntry
{
    public DateOnly Day { get; init; }

    public string Preview { get; init; }

    public Lazy<string> Content { get; init; }
}
