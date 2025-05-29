using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Collox.ViewModels.Messages;

public class TemplateDeletedMessage(Template template) : ValueChangedMessage<Template>(template);
