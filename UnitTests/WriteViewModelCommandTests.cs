using Collox.Services;
using Collox.ViewModels;
using Microsoft.UI.Xaml.Controls;
using Moq;
using NFluent; // Added for NFluent assertions

namespace Collox.Tests.ViewModels;

[TestClass]
public class WriteViewModelCommandTests
{
    private readonly Mock<IStoreService> _storeServiceMock;
    private readonly Mock<IAIService> _aiServiceMock;
    private readonly WriteViewModel _viewModel;

    public WriteViewModelCommandTests()
    {
        _storeServiceMock = new Mock<IStoreService>();
        _aiServiceMock = new Mock<IAIService>();
        _viewModel = new WriteViewModel(_storeServiceMock.Object, _aiServiceMock.Object);
        _viewModel.ConversationContext = new TabData();
    }

    [TestMethod]
    public async Task ProcessCommand_Clear_ClearsMessages()
    {
        // Arrange
        _viewModel.Messages.Add(new TextColloxMessage { Text = "Message 1" });
        _viewModel.InputMessage = "clear";

        // Use reflection to call the private method
        var method = typeof(WriteViewModel).GetMethod("ProcessCommand",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        // Act
        var task = (Task)method.Invoke(_viewModel, null);
        await task;

        // Assert
        Check.That(_viewModel.Messages).IsEmpty();
        _storeServiceMock.Verify(s => s.SaveNow(), Times.Once);
    }

    [TestMethod]
    public async Task ProcessCommand_Save_CallsStoreService()
    {
        // Arrange
        _viewModel.InputMessage = "save";

        // Use reflection to call the private method
        var method = typeof(WriteViewModel).GetMethod("ProcessCommand",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        // Act
        var task = (Task)method.Invoke(_viewModel, null);
        await task;

        // Assert
        _storeServiceMock.Verify(s => s.SaveNow(), Times.Once);
    }

    [TestMethod]
    public async Task ProcessCommand_Help_AddsHelpMessage()
    {
        // Arrange
        _viewModel.InputMessage = "help";

        // Use reflection to call the private method
        var method = typeof(WriteViewModel).GetMethod("ProcessCommand",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        // Act
        var task = (Task)method.Invoke(_viewModel, null);
        await task;

        // Assert
        Check.That(_viewModel.Messages).HasSize(1);
        Check.That(_viewModel.Messages[0]).IsInstanceOf<InternalColloxMessage>();
        Check.That(((InternalColloxMessage)_viewModel.Messages[0]).Message).Contains("Available commands");
        Check.That(((InternalColloxMessage)_viewModel.Messages[0]).Severity).IsEqualTo(InfoBarSeverity.Informational);
    }

    [TestMethod]
    public async Task ProcessCommand_Time_AddsTimeMessage()
    {
        // Arrange
        _viewModel.InputMessage = "time";

        // Use reflection to call the private method
        var method = typeof(WriteViewModel).GetMethod("ProcessCommand",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        // Act
        var task = (Task)method.Invoke(_viewModel, null);
        await task;

        // Assert
        Check.That(_viewModel.Messages).HasSize(1);
        Check.That(_viewModel.Messages[0]).IsInstanceOf<TimeColloxMessage>();
    }

    [TestMethod]
    public async Task ProcessCommand_Pin_SetsIsCloseable()
    {
        // Arrange
        _viewModel.InputMessage = "pin";
        _viewModel.ConversationContext.IsCloseable = true;

        // Use reflection to call the private method
        var method = typeof(WriteViewModel).GetMethod("ProcessCommand",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        // Act
        var task = (Task)method.Invoke(_viewModel, null);
        await task;

        // Assert
        Check.That(_viewModel.ConversationContext.IsCloseable).IsFalse();
    }

    [TestMethod]
    public async Task ProcessCommand_Unpin_SetsIsCloseable()
    {
        // Arrange
        _viewModel.InputMessage = "unpin";
        _viewModel.ConversationContext.IsCloseable = false;

        // Use reflection to call the private method
        var method = typeof(WriteViewModel).GetMethod("ProcessCommand",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        // Act
        var task = (Task)method.Invoke(_viewModel, null);
        await task;

        // Assert
        Check.That(_viewModel.ConversationContext.IsCloseable).IsTrue();
    }

    [TestMethod]
    public async Task ProcessCommand_Task_AddsTask()
    {
        // Arrange
        _viewModel.InputMessage = "task Write unit tests";

        // Use reflection to call the private method
        var method = typeof(WriteViewModel).GetMethod("ProcessCommand",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        // Act
        var task = (Task)method.Invoke(_viewModel, null);
        await task;

        // Assert
        Check.That(_viewModel.Tasks).HasSize(1);
        Check.That(_viewModel.Tasks[0].Name).IsEqualTo("Write unit tests");
        Check.That(_viewModel.Tasks[0].IsDone).IsFalse();
    }
}
