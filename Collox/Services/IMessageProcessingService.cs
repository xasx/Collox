using System.Collections.ObjectModel;
using Collox.Models;
using Microsoft.Extensions.AI;

namespace Collox.Services;

public interface IMessageProcessingService
{
    Task<string> CreateChatMessageAsync(MessageProcessingContext context, IntelligentProcessor processor, IChatClient client, CancellationToken cancellationToken = default);
    Task<string> CreateCommentAsync(MessageProcessingContext context, IntelligentProcessor processor, IChatClient client, CancellationToken cancellationToken = default);
    Task<string> CreateTaskAsync(MessageProcessingContext context, IntelligentProcessor processor, IChatClient client, CancellationToken cancellationToken = default);
    Task<string> ModifyMessageAsync(MessageProcessingContext context, IntelligentProcessor processor, IChatClient client, CancellationToken cancellationToken = default);
    Task ProcessMessageAsync(MessageProcessingContext context, IEnumerable<IntelligentProcessor> processors, CancellationToken cancellationToken = default);


}

public record MessageProcessingContext(TextColloxMessage CurrentMessage, ObservableCollection<ColloxMessage> Messages, string Context, ObservableCollection<TaskViewModel> Tasks);
