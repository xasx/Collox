using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Collox.ViewModels.Messages;

public class TemplateEditedMessage(Template template) : ValueChangedMessage<Template>(template);
