using Collox.Common; // Added for .Count()
using Collox.Services;
using Collox.ViewModels;
using Collox.ViewModels.Messages;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Xaml.Controls;
using Moq;
using NFluent;
using System.Speech.Synthesis;
using Windows.System;
using ITimer = Collox.ViewModels.ITimer;

namespace Collox.Tests.ViewModels;

[TestClass]
public class WriteViewModelTests
{
    private readonly Mock<IStoreService> _storeServiceMock;
    private readonly Mock<IAIService> _aiServiceMock;
    private readonly Mock<IAudioService> _audioServiceMock;
    private readonly Mock<IMessageProcessingService> _messageProcessingServiceMock;
    private readonly Mock<ICommandService> _commandServiceMock;
    private readonly WriteViewModel _viewModel;

    private readonly Mock<ITimer> _timerMock = new();

    private readonly AppConfig Settings = AppHelper.Settings;

    public WriteViewModelTests()
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
            _aiServiceMock.Object,
            _audioServiceMock.Object,
            _messageProcessingServiceMock.Object,
            _commandServiceMock.Object)
        {
            ConversationContext = new TabData()
        };

        
        _timerMock = new Mock<ITimer>();
        MessageRelativeTimeUpdater.CreateTimer = () => _timerMock.Object;
    }

    [TestMethod]
    public void Constructor_InitializesProperties()
    {
        // Assert
        Check.That(_viewModel.Messages).IsNotNull();
        Check.That(_viewModel.Tasks).IsNotNull();
        Check.That(_viewModel.InputMessage).IsEmpty();
        Check.That(_viewModel.SubmitModeIcon).IsEqualTo(Symbol.Send);
        Check.That(_viewModel.IsBeeping).IsEqualTo(Settings.AutoBeep);
        Check.That(_viewModel.IsSpeaking).IsEqualTo(Settings.AutoRead);
    }

    [TestMethod]
    public async Task Submit_WithEmptyInput_DoesNothing()
    {
        // Arrange
        _viewModel.InputMessage = string.Empty;

        // Act
        await _viewModel.SubmitCommand.ExecuteAsync(null);

        // Assert
        Check.That(_viewModel.Messages).IsEmpty();
    }

    [TestMethod]
    public async Task Submit_WithTextInputAndSendMode_AddsTextMessage()
    {
        // Arrange
        _viewModel.InputMessage = "Test message";
        _viewModel.SubmitModeIcon = Symbol.Send;
        Settings.EnableAI = false; // Disable AI to simplify test

        // Act
        await _viewModel.SubmitCommand.ExecuteAsync(null);

        // Assert
        Check.That(_viewModel.Messages).HasSize(1);
        Check.That(_viewModel.Messages[0]).IsInstanceOf<TextColloxMessage>();
        Check.That(((TextColloxMessage)_viewModel.Messages[0]).Text).IsEqualTo("Test message");
        Check.That(_viewModel.InputMessage).IsEmpty();
    }

    [TestMethod]
    public async Task Submit_WithCommandInput_ProcessesCommand()
    {
        // Arrange
        _viewModel.InputMessage = "help";
        _viewModel.SubmitModeIcon = Symbol.Play;

        var commandResult = new CommandResult { Success = true };
        _commandServiceMock.Setup(
            s => s.ProcessCommandAsync("help", It.IsAny<CommandContext>()))
            .ReturnsAsync(commandResult);

        // Act
        await _viewModel.SubmitCommand.ExecuteAsync(null);

        // Assert
        _commandServiceMock.Verify(
            s => s.ProcessCommandAsync("help", It.IsAny<CommandContext>()), 
            Times.Once);
        Check.That(_viewModel.InputMessage).IsEmpty();
    }

    [TestMethod]
    public async Task Clear_ClearsMessagesAndSaves()
    {
        // Arrange
        _viewModel.Messages.Add(new TextColloxMessage { Text = "Message 1" });

        // Act
        await _viewModel.ClearCommand.ExecuteAsync(null);

        // Assert
        Check.That(_viewModel.Messages).IsEmpty();
        _storeServiceMock.Verify(s => s.SaveNow(), Times.Once);
    }

    [TestMethod]
    public async Task SaveNow_CallsStoreService()
    {
        // Act
        await _viewModel.SaveNowCommand.ExecuteAsync(null);

        // Assert
        _storeServiceMock.Verify(s => s.SaveNow(), Times.Once);
    }

    [TestMethod]
    public void SwitchMode_TogglesBetweenSendAndPlay()
    {
        // Arrange - starts with Send mode
        Check.That(_viewModel.SubmitModeIcon).IsEqualTo(Symbol.Send);

        // Act - switch to command mode
        _viewModel.SwitchModeCommand.Execute(null);

        // Assert
        Check.That(_viewModel.SubmitModeIcon).IsEqualTo(Symbol.Play);

        // Act - switch back to send mode
        _viewModel.SwitchModeCommand.Execute(null);

        // Assert
        Check.That(_viewModel.SubmitModeIcon).IsEqualTo(Symbol.Send);
    }

    [TestMethod]
    public void OnIsBeepingChanged_UpdatesSettings()
    {
        // Arrange
        var originalValue = Settings.AutoBeep;

        // Act
        _viewModel.IsBeeping = !originalValue;

        // Assert
        Check.That(Settings.AutoBeep).IsEqualTo(!originalValue);
    }

    [TestMethod]
    public void OnIsSpeakingChanged_UpdatesSettings()
    {
        // Arrange
        var originalValue = Settings.AutoRead;

        // Act
        _viewModel.IsSpeaking = !originalValue;

        // Assert
        Check.That(Settings.AutoRead).IsEqualTo(!originalValue);
    }
}
