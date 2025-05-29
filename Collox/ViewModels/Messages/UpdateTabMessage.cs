using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Collox.ViewModels.Messages;

public class UpdateTabMessage(TabData tabData) : ValueChangedMessage<TabData>(tabData);
