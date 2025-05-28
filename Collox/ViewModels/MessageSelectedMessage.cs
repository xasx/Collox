using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Collox.ViewModels;

public class MessageSelectedMessage(ColloxMessage value) : ValueChangedMessage<ColloxMessage>(value);
