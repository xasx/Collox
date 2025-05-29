using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Collox.ViewModels.Messages;

public class MessageSelectedMessage(ColloxMessage value) : ValueChangedMessage<ColloxMessage>(value);
