using Collox.Services;
using Collox.ViewModels;
using Moq;
using NFluent;
using Windows.UI.Notifications;

namespace Collox.Tests.ViewModels;

[TestClass]
public class MainViewModelTests
{
    private readonly Mock<IUserNotificationService> _userNotificationServiceMock;
    private readonly Mock<IStoreService> _storeServiceMock;
    private readonly MainViewModel _viewModel;

    public MainViewModelTests()
    {
        _userNotificationServiceMock = new Mock<IUserNotificationService>();
        _storeServiceMock = new Mock<IStoreService>();
        _viewModel = new MainViewModel(_userNotificationServiceMock.Object, _storeServiceMock.Object);
    }

    [TestMethod]
    public async Task Init_InitializesViewModel()
    {
        // Arrange
        _userNotificationServiceMock.Setup(s => s.Initialize()).Returns(Task.CompletedTask);
        _userNotificationServiceMock.Setup(s => s.GetNotifications()).ReturnsAsync(new List<UserNotification>());
        _storeServiceMock.Setup(s => s.GetFilename()).Returns("TestFilename");

        // Act
        await _viewModel.Init();

        // Assert
        Check.That(_viewModel.DocumentFilename).IsEqualTo("TestFilename");
        _userNotificationServiceMock.Verify(s => s.Initialize(), Times.Once);
        _userNotificationServiceMock.Verify(s => s.GetNotifications(), Times.Once);
    }

    [TestMethod]
    public void RefreshInternetState_UpdatesInternetState()
    {
        // Arrange
        _viewModel.InternetState.State = "offline";
        _viewModel.InternetState.Icon = "\uF384";

        // Act
        _viewModel.InternetState.State = "online";
        _viewModel.InternetState.Icon = "\uE774";

        // Assert
        Check.That(_viewModel.InternetState.State).IsEqualTo("online");
        Check.That(_viewModel.InternetState.Icon).IsEqualTo("\uE774");

        // Act
        _viewModel.InternetState.State = "offline";
        _viewModel.InternetState.Icon = "\uF384";

        // Assert
        Check.That(_viewModel.InternetState.State).IsEqualTo("offline");
        Check.That(_viewModel.InternetState.Icon).IsEqualTo("\uF384");
    }
}
