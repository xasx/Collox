using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Collox.ViewModels;
public class TemplateEditedMessage(Template template) : ValueChangedMessage<Template>(template);
