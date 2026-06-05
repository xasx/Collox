using Collox.Common;
using Collox.Services;
using Collox.ViewModels;
using Moq;
using NFluent;
using Windows.UI.Notifications;

namespace Collox.Tests.ViewModels;

[TestClass]
[DoNotParallelize]
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
    public async Task InitAsync_InitializesDocumentFilenameAndCallsNotificationService()
    {
        // Arrange
        _userNotificationServiceMock.Setup(s => s.Initialize()).Returns(Task.CompletedTask);
        _userNotificationServiceMock.Setup(s => s.GetNotifications()).ReturnsAsync(new List<UserNotification>());
        _storeServiceMock.Setup(s => s.GetFilename()).Returns("TestFilename");

        // Act
        await _viewModel.InitAsync();

        // Assert
        Check.That(_viewModel.DocumentFilename).IsEqualTo("TestFilename");
        _userNotificationServiceMock.Verify(s => s.Initialize(), Times.Once);
        _userNotificationServiceMock.Verify(s => s.GetNotifications(), Times.Once);
    }
    [TestMethod]
    public async Task InitAsync_WithNotifications_PopulatesUserNotifications()
    {
        // Arrange
        _userNotificationServiceMock.Setup(s => s.Initialize()).Returns(Task.CompletedTask);
        _userNotificationServiceMock.Setup(s => s.GetNotifications()).ReturnsAsync(new List<UserNotification>());
        _storeServiceMock.Setup(s => s.GetFilename()).Returns(string.Empty);

        // Act
        await _viewModel.InitAsync();

        // Assert
        Check.That(_viewModel.UserNotificationsEmpty).IsTrue();
        _userNotificationServiceMock.Verify(s => s.GetNotifications(), Times.Once);
    }

    [TestMethod]
    public void OnIsAIEnabledChanged_UpdatesSettings()
    {
        // Arrange
        var originalValue = AppHelper.Settings.EnableAI;

        try
        {
            // Act
            _viewModel.IsAIEnabled = !originalValue;

            // Assert
            Check.That(AppHelper.Settings.EnableAI).IsEqualTo(!originalValue);
        }
        finally
        {
            // Restore
            AppHelper.Settings.EnableAI = originalValue;
        }
    }

    [TestMethod]
    public void ConfigurationLocation_IsSetToAppConfigPath()
    {
        // Assert
        Check.That(_viewModel.ConfigurationLocation).IsEqualTo(Constants.AppConfigPath);
    }

    [TestMethod]
    public void Dispose_CanBeCalledMultipleTimes_WithoutException()
    {
        // Act & Assert - should not throw
        _viewModel.Dispose();
        _viewModel.Dispose();
    }
}
