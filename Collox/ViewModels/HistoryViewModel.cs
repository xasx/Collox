using Collox.Services;
using CommunityToolkit.Mvvm.Collections;

namespace Collox.ViewModels;

public partial class HistoryViewModel(IStoreService storeService) : ObservableObject
{
    private readonly IStoreService storeService = storeService;

    public ObservableGroupedCollection<string, HistoryEntry> Histories { get; set; } = [];

    [ObservableProperty] public partial HistoryEntry SelectedHistoryEntry { get; set; }

    [RelayCommand]
    public async Task LoadHistory()
    {
        Histories.Clear();
        var historyData = await storeService.Load(CancellationToken.None).ConfigureAwait(true);

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
