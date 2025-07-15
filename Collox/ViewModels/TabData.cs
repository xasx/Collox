using System.Collections.ObjectModel;
using Collox.Models;

namespace Collox.ViewModels;

public partial class TabData : ObservableObject
{
    [ObservableProperty] public partial ObservableCollection<IntelligentProcessor> ActiveProcessors { get; set; } = [];

    [ObservableProperty] public partial string Context { get; set; }

    [ObservableProperty] public partial bool IsCloseable { get; set; }

    [ObservableProperty] public partial bool IsEditing { get; set; }
}
