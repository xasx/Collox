using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Collox.ViewModels.Messages;

public class FocusTabMessage(TabData tabData) : ValueChangedMessage<TabData>(tabData);
