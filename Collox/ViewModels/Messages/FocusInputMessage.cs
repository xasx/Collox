using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Collox.ViewModels.Messages;

public class FocusInputMessage : ValueChangedMessage<object>
{
    public FocusInputMessage() : base(null)
    {
    }
}
