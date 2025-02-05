using Collox.Services;
using CommunityToolkit.Mvvm.Collections;

namespace Collox.ViewModels;
public partial class HistoryViewModel : ObservableObject
{
    private readonly IStoreService storeService = App.GetService<IStoreService>();

    public ObservableGroupedCollection<string, HistoryEntry> Histories { get; set; } = [];

    [ObservableProperty]
    public partial HistoryEntry SelectedHistoryEntry { get; set; }

    [RelayCommand]
    public async Task LoadHistory()
    {
        Histories.Clear();
        var historyData = await storeService.Load();
        foreach (var (month, historyItems) in historyData)
        {
            foreach (var historyItem in historyItems)
            {
                Histories.AddItem(month, new HistoryEntry()
                {
                    Day = historyItem.Date,
                    Preview = historyItem.Preview
                });
            }
        }
    }
}

public class HistoryEntry
{
    public DateOnly Day { get; init; }
    public string Preview { get; init; }
}
