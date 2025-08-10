using System.Collections.ObjectModel;
using Collox.Models;
using Microsoft.Extensions.AI;

namespace Collox.Services;

public interface IMessageProcessingService
{
    Task ProcessMessageAsync(TextColloxMessage message, IEnumerable<IntelligentProcessor> processors);
    Task<string> CreateCommentAsync(TextColloxMessage message, IntelligentProcessor processor, IChatClient client);
    Task<string> CreateTaskAsync(TextColloxMessage message, IntelligentProcessor processor, IChatClient client, ObservableCollection<TaskViewModel> tasks);
    Task<string> ModifyMessageAsync(TextColloxMessage message, IntelligentProcessor processor, IChatClient client);
    Task<string> CreateChatMessageAsync(IEnumerable<TextColloxMessage> messages, IntelligentProcessor processor, IChatClient client, ObservableCollection<ColloxMessage> messagesCollection, string context);
}
