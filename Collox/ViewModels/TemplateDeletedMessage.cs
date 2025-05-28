using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Collox.ViewModels;

public class TemplateDeletedMessage(Template template) : ValueChangedMessage<Template>(template);
