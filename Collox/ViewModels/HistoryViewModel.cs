using System.Diagnostics;
using Collox.Services;
using CommunityToolkit.Mvvm.Collections;
using Microsoft.Windows.AppNotifications.Builder;
using Microsoft.Windows.AppNotifications;

namespace Collox.ViewModels;
public partial class HistoryViewModel : ObservableObject
{
    private readonly IStoreService storeService = App.GetService<IStoreService>();

    public HistoryViewModel()
    {
        Histories = new ObservableGroupedCollection<string, HistoryEntry>();
    }

    public ObservableGroupedCollection<string, HistoryEntry> Histories { get; set; }

    [ObservableProperty]
    public partial HistoryEntry SelectedHistoryEntry { get; set; }

    [RelayCommand]
    public async Task LoadHistory()
    {
        Histories.Clear();
        var hist = await storeService.Load();
        foreach (var (month, hists) in hist)
        {
            foreach (var hihi in hists)
            {
                Histories.AddItem(month, new HistoryEntry()
                {
                    Day = hihi.Date,
                    Preview = hihi.Preview
                });
            }
        }
    }
}

public class HistoryEntry
{
    public DateOnly Day { get; set; }
    public string Preview { get; set; }

}
