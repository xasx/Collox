using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Collox.ViewModels;

public class FocusTabMessage(TabData tabData) : ValueChangedMessage<TabData>(tabData);
