using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Collox.ViewModels.Messages;

public class TaskDoneMessage(TaskViewModel value) : ValueChangedMessage<TaskViewModel>(value);
