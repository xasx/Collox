using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Collox.ViewModels;
public class UpdateTabMessage(TabData tabData) : ValueChangedMessage<TabData>(tabData);
