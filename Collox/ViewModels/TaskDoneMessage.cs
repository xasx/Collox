using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Collox.ViewModels;

public class TaskDoneMessage(TaskViewModel value) : ValueChangedMessage<TaskViewModel>(value);
