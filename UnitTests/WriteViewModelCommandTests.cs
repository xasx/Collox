using Collox.Services;
using Collox.ViewModels;
using Microsoft.UI.Xaml.Controls;
using Moq;
using NFluent;
using Windows.System;

namespace Collox.Tests.ViewModels;

[TestClass]
public class WriteViewModelCommandTests
{
    private readonly Mock<IStoreService> _storeServiceMock;
    private readonly Mock<IAIService> _aiServiceMock;
    private readonly Mock<IAudioService> _audioServiceMock;
    private readonly Mock<IMessageProcessingService> _messageProcessingServiceMock;
    private readonly Mock<ICommandService> _commandServiceMock;
    private readonly WriteViewModel _viewModel;

    private readonly Mock<Collox.ViewModels.ITimer> _timerMock = new();

    public WriteViewModelCommandTests()
    {
        _storeServiceMock = new Mock<IStoreService>();
        _aiServiceMock = new Mock<IAIService>();
        _audioServiceMock = new Mock<IAudioService>();
        _messageProcessingServiceMock = new Mock<IMessageProcessingService>();
        _commandServiceMock = new Mock<ICommandService>();


        var mockVoices = new List<System.Speech.Synthesis.VoiceInfo>();
        _audioServiceMock.Setup(a => a.GetInstalledVoices()).Returns(mockVoices);

        _viewModel = new WriteViewModel(
            _storeServiceMock.Object, 
            _audioServiceMock.Object,
            _messageProcessingServiceMock.Object,
            _commandServiceMock.Object);
        _viewModel.ConversationContext = new TabData();

        _timerMock = new Mock<Collox.ViewModels.ITimer>();
        MessageRelativeTimeUpdater.CreateTimer = () => _timerMock.Object;
    }

    [TestMethod]
    public async Task ProcessCommand_Clear_CallsCommandService()
    {
        // Arrange
        _viewModel.Messages.Add(new TextColloxMessage { Text = "Message 1" });
        _viewModel.InputMessage = "clear";
        _viewModel.SubmitModeIcon = Symbol.Play;

        var commandResult = new CommandResult { Success = true };
        _commandServiceMock.Setup(
            s => s.ProcessCommandAsync("clear", It.IsAny<CommandContext>()))
            .ReturnsAsync(commandResult);

        // Act
        await _viewModel.SubmitCommand.ExecuteAsync(null);

        // Assert
        _commandServiceMock.Verify(
            s => s.ProcessCommandAsync("clear", It.IsAny<CommandContext>()), 
            Times.Once);
    }

    [TestMethod]
    public async Task ProcessCommand_Save_CallsCommandService()
    {
        // Arrange
        _viewModel.InputMessage = "save";
        _viewModel.SubmitModeIcon = Symbol.Play;

        var commandResult = new CommandResult { Success = true };
        _commandServiceMock.Setup(
            s => s.ProcessCommandAsync("save", It.IsAny<CommandContext>()))
            .ReturnsAsync(commandResult);

        // Act
        await _viewModel.SubmitCommand.ExecuteAsync(null);

        // Assert
        _commandServiceMock.Verify(
            s => s.ProcessCommandAsync("save", It.IsAny<CommandContext>()), 
            Times.Once);
    }

    [TestMethod]
    public async Task ProcessCommand_Help_CallsCommandService()
    {
        // Arrange
        _viewModel.InputMessage = "help";
        _viewModel.SubmitModeIcon = Symbol.Play;

        var helpMessage = new InternalColloxMessage
        {
            Message = "Available commands: clear, save, speak, time, pin, unpin, task",
            Severity = InfoBarSeverity.Informational
        };
        var commandResult = new CommandResult { Success = true, ResultMessage = helpMessage };
        _commandServiceMock.Setup(
            s => s.ProcessCommandAsync("help", It.IsAny<CommandContext>()))
            .ReturnsAsync(commandResult);

        // Act
        await _viewModel.SubmitCommand.ExecuteAsync(null);

        // Assert
        _commandServiceMock.Verify(
            s => s.ProcessCommandAsync("help", It.IsAny<CommandContext>()), 
            Times.Once);
        Check.That(_viewModel.Messages).HasSize(1);
        Check.That(_viewModel.Messages[0]).IsInstanceOf<InternalColloxMessage>();
    }

    [TestMethod]
    public async Task ProcessCommand_Time_CallsCommandService()
    {
        // Arrange
        _viewModel.InputMessage = "time";
        _viewModel.SubmitModeIcon = Symbol.Play;

        var timeMessage = new TimeColloxMessage { Time = DateTime.Now.TimeOfDay };
        var commandResult = new CommandResult { Success = true, ResultMessage = timeMessage };
        _commandServiceMock.Setup(
            s => s.ProcessCommandAsync("time", It.IsAny<CommandContext>()))
            .ReturnsAsync(commandResult);

        // Act
        await _viewModel.SubmitCommand.ExecuteAsync(null);

        // Assert
        _commandServiceMock.Verify(
            s => s.ProcessCommandAsync("time", It.IsAny<CommandContext>()), 
            Times.Once);
        Check.That(_viewModel.Messages).HasSize(1);
        Check.That(_viewModel.Messages[0]).IsInstanceOf<TimeColloxMessage>();
    }

    [TestMethod]
    public async Task ProcessCommand_Pin_CallsCommandService()
    {
        // Arrange
        _viewModel.InputMessage = "pin";
        _viewModel.SubmitModeIcon = Symbol.Play;
        _viewModel.ConversationContext.IsCloseable = true;

        var commandResult = new CommandResult { Success = true };
        _commandServiceMock.Setup(
            s => s.ProcessCommandAsync("pin", It.IsAny<CommandContext>()))
            .ReturnsAsync(commandResult);

        // Act
        await _viewModel.SubmitCommand.ExecuteAsync(null);

        // Assert
        _commandServiceMock.Verify(
            s => s.ProcessCommandAsync("pin", It.IsAny<CommandContext>()), 
            Times.Once);
    }

    [TestMethod]
    public async Task ProcessCommand_Unpin_CallsCommandService()
    {
        // Arrange
        _viewModel.InputMessage = "unpin";
        _viewModel.SubmitModeIcon = Symbol.Play;
        _viewModel.ConversationContext.IsCloseable = false;

        var commandResult = new CommandResult { Success = true };
        _commandServiceMock.Setup(
            s => s.ProcessCommandAsync("unpin", It.IsAny<CommandContext>()))
            .ReturnsAsync(commandResult);

        // Act
        await _viewModel.SubmitCommand.ExecuteAsync(null);

        // Assert
        _commandServiceMock.Verify(
            s => s.ProcessCommandAsync("unpin", It.IsAny<CommandContext>()), 
            Times.Once);
    }

    [TestMethod]
    public async Task ProcessCommand_Task_CallsCommandService()
    {
        // Arrange
        _viewModel.InputMessage = "task Write unit tests";
        _viewModel.SubmitModeIcon = Symbol.Play;

        var commandResult = new CommandResult { Success = true };
        _commandServiceMock.Setup(
            s => s.ProcessCommandAsync("task Write unit tests", It.IsAny<CommandContext>()))
            .ReturnsAsync(commandResult);

        // Act
        await _viewModel.SubmitCommand.ExecuteAsync(null);

        // Assert
        _commandServiceMock.Verify(
            s => s.ProcessCommandAsync("task Write unit tests", It.IsAny<CommandContext>()), 
            Times.Once);
    }

    [TestMethod]
    public async Task ProcessCommand_WithResultMessage_AddsMessageToCollection()
    {
        // Arrange
        _viewModel.InputMessage = "help";
        _viewModel.SubmitModeIcon = Symbol.Play;

        var helpMessage = new InternalColloxMessage
        {
            Message = "Available commands",
            Severity = InfoBarSeverity.Informational
        };
        var commandResult = new CommandResult { Success = true, ResultMessage = helpMessage };
        _commandServiceMock.Setup(
            s => s.ProcessCommandAsync("help", It.IsAny<CommandContext>()))
            .ReturnsAsync(commandResult);

        // Act
        await _viewModel.SubmitCommand.ExecuteAsync(null);

        // Assert
        Check.That(_viewModel.Messages).HasSize(1);
        Check.That(_viewModel.Messages[0]).IsEqualTo(helpMessage);
    }

    [TestMethod]
    public async Task ProcessCommand_WithFailure_LogsError()
    {
        // Arrange
        _viewModel.InputMessage = "invalid";
        _viewModel.SubmitModeIcon = Symbol.Play;

        var commandResult = new CommandResult { Success = false, ErrorMessage = "Unknown command" };
        _commandServiceMock.Setup(
            s => s.ProcessCommandAsync("invalid", It.IsAny<CommandContext>()))
            .ReturnsAsync(commandResult);

        // Act
        await _viewModel.SubmitCommand.ExecuteAsync(null);

        // Assert - The error should be logged but not throw an exception
        _commandServiceMock.Verify(
            s => s.ProcessCommandAsync("invalid", It.IsAny<CommandContext>()), 
            Times.Once);
    }
}
