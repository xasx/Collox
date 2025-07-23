using System.Collections.ObjectModel;
using Collox.ViewModels;

namespace Collox.Services;

public interface ICommandService
{
    Task<CommandResult> ProcessCommandAsync(string command, CommandContext context);
}

public class CommandResult
{
    public bool Success { get; set; }
    public ColloxMessage ResultMessage { get; set; }
    public string ErrorMessage { get; set; }
}

public class CommandContext
{
    public ObservableCollection<ColloxMessage> Messages { get; set; }
    public ObservableCollection<TaskViewModel> Tasks { get; set; }
    public TabData ConversationContext { get; set; }
    public IStoreService StoreService { get; set; }
    public IAudioService AudioService { get; set; }
}
