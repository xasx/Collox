using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Collox.ViewModels.Messages;

public class TextSubmittedMessage(TextColloxMessage value) : ValueChangedMessage<TextColloxMessage>(value);
