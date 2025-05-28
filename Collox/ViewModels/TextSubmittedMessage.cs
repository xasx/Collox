using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Collox.ViewModels;
public class TextSubmittedMessage(TextColloxMessage value) : ValueChangedMessage<TextColloxMessage>(value);
