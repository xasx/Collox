using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Collox.ViewModels.Messages;

public class TemplateAddedMessage(Template template) : ValueChangedMessage<Template>(template);
