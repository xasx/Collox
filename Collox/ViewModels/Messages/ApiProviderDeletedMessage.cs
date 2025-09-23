using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Collox.ViewModels.Messages;

public class ApiProviderDeletedMessage(IntelligenceApiProviderViewModel intelligenceApiProviderViewModel) : ValueChangedMessage<IntelligenceApiProviderViewModel>(
    intelligenceApiProviderViewModel);
