using Collox.Models;
using Collox.ViewModels;
using Microsoft.Extensions.AI;
using System.Collections.ObjectModel;

namespace Collox.Services;

public interface IMessageProcessingService
{
    Task ProcessMessageAsync(TextColloxMessage message, IEnumerable<IntelligentProcessor> processors);
    Task<string> CreateCommentAsync(TextColloxMessage message, IntelligentProcessor processor, IChatClient client);
    Task<string> CreateTaskAsync(TextColloxMessage message, IntelligentProcessor processor, IChatClient client, ObservableCollection<TaskViewModel> tasks);
    Task<string> ModifyMessageAsync(TextColloxMessage message, IntelligentProcessor processor, IChatClient client);
    Task<string> CreateChatMessageAsync(IEnumerable<TextColloxMessage> messages, IntelligentProcessor processor, IChatClient client, ObservableCollection<ColloxMessage> messagesCollection, string context);
}
