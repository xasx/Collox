using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Collox.ViewModels.Messages;

public class ProcessorDeletedMessage(IntelligentProcessorViewModel intelligentProcessorViewModel) : ValueChangedMessage<IntelligentProcessorViewModel>(
    intelligentProcessorViewModel);
