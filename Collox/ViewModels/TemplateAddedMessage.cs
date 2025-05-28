using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Collox.ViewModels;

public class TemplateAddedMessage(Template template) : ValueChangedMessage<Template>(template);
