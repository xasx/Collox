using Collox.Common;
using Collox.Models;
using Collox.Services;
using Collox.ViewModels;
using Microsoft.Extensions.AI;
using Microsoft.UI.Xaml.Controls;
using Moq;
using NFluent;
using Windows.System;
using System.Collections.ObjectModel;

namespace Collox.Tests.ViewModels;

[TestClass]
public class WriteViewModelAITests
{
    private readonly Mock<IStoreService> _storeServiceMock;
    private readonly Mock<IAIService> _aiServiceMock;
    private readonly Mock<IAudioService> _audioServiceMock;
    private readonly Mock<IMessageProcessingService> _messageProcessingServiceMock;
    private readonly Mock<ICommandService> _commandServiceMock;
    private readonly Mock<IChatClient> _chatClientMock;
    private readonly WriteViewModel _viewModel;

    private readonly Mock<Collox.ViewModels.ITimer> _timerMock;

    private readonly AppConfig Settings = AppHelper.Settings;

    public WriteViewModelAITests()
    {
        _storeServiceMock = new Mock<IStoreService>();
        _aiServiceMock = new Mock<IAIService>();
        _audioServiceMock = new Mock<IAudioService>();
        _messageProcessingServiceMock = new Mock<IMessageProcessingService>();
        _commandServiceMock = new Mock<ICommandService>();
        _chatClientMock = new Mock<IChatClient>();

        _aiServiceMock.Setup(a => a.GetChatClient(It.IsAny<AIProvider>(), It.IsAny<string>()))
            .Returns(_chatClientMock.Object);
    
        var mockVoices = new List<System.Speech.Synthesis.VoiceInfo>();
        _audioServiceMock.Setup(a => a.GetInstalledVoices()).Returns(mockVoices);

        _viewModel = new WriteViewModel(
            _storeServiceMock.Object, 
            _aiServiceMock.Object,
            _audioServiceMock.Object,
            _messageProcessingServiceMock.Object,
            _commandServiceMock.Object);
        _viewModel.ConversationContext = new TabData();
        _timerMock = new Mock<Collox.ViewModels.ITimer>();
        MessageRelativeTimeUpdater.CreateTimer = () => _timerMock.Object;
    }

    [TestMethod]
    public async Task ProcessMessageWithAI_CallsMessageProcessingService()
    {
        // Arrange
        var textMessage = new TextColloxMessage { Text = "Test message" };
        var processors = new ObservableCollection<IntelligentProcessor>
        {
            new IntelligentProcessor
            {
                Id = Guid.NewGuid(),
                Name = "Test Processor",
                Prompt = "Test prompt",
                Target = Target.Comment
            }
        };

        _viewModel.ConversationContext.ActiveProcessors = processors;

        // Act - we'll use reflection to test the private method
        var method = typeof(WriteViewModel).GetMethod("ProcessMessageWithAI",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (method != null)
        {
            var task = (Task)method.Invoke(_viewModel, new object[] { textMessage });
            await task;
        }

        // Assert
        _messageProcessingServiceMock.Verify(
            s => s.ProcessMessageAsync(textMessage, processors), 
            Times.Once);
    }

    [TestMethod]
    public async Task AddTextMessage_ExecutesAudioOperations_WhenEnabled()
    {
        // Arrange
        _viewModel.InputMessage = "Test message";
        _viewModel.IsBeeping = true;
        _viewModel.IsSpeaking = true;
        Settings.EnableAI = false; // Disable AI to simplify test

        // Act
        await _viewModel.SubmitCommand.ExecuteAsync(null);

        // Assert
        _audioServiceMock.Verify(a => a.PlayBeepSoundAsync(), Times.Once);
        _audioServiceMock.Verify(a => a.ReadTextAsync("Test message", It.IsAny<string>()), Times.Once);
    }

    [TestMethod]
    public async Task SpeakLastAsync_CallsAudioService()
    {
        // Arrange
        var textMessage = new TextColloxMessage { Text = "Test message to speak" };
        _viewModel.Messages.Add(textMessage);

        // Act
        await _viewModel.SpeakLastCommand.ExecuteAsync(null);

        // Assert
        _audioServiceMock.Verify(
            a => a.ReadTextAsync(It.IsAny<string>(), It.IsAny<string>()), 
            Times.Once);
    }

    [TestMethod]
    public async Task ProcessCommand_CallsCommandService()
    {
        // Arrange
        _viewModel.InputMessage = "test command";
        _viewModel.SubmitModeIcon = Symbol.Play;

        var commandResult = new CommandResult { Success = true };
        _commandServiceMock.Setup(
            s => s.ProcessCommandAsync("test command", It.IsAny<CommandContext>()))
            .ReturnsAsync(commandResult);

        // Act
        await _viewModel.SubmitCommand.ExecuteAsync(null);

        // Assert
        _commandServiceMock.Verify(
            s => s.ProcessCommandAsync("test command", It.IsAny<CommandContext>()), 
            Times.Once);
    }

    [TestMethod]
    public void InstalledVoices_ReturnsAudioServiceVoices()
    {
        // Arrange
        var mockVoices = new List<System.Speech.Synthesis.VoiceInfo>();
        _audioServiceMock.Setup(a => a.GetInstalledVoices()).Returns(mockVoices);

        // Act
        var voices = _viewModel.InstalledVoices;

        // Assert
        Check.That(voices).IsEqualTo(mockVoices);
        _audioServiceMock.Verify(a => a.GetInstalledVoices(), Times.AtLeastOnce);
    }
}
