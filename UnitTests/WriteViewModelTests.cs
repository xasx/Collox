using System.Speech.Synthesis;
using Collox.Common; // Added for .Count()
using Collox.Services;
using Collox.ViewModels;
using Collox.ViewModels.Messages;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Xaml.Controls;
using Moq;
using NFluent; // Added for NFluent assertions

namespace Collox.Tests.ViewModels;

[TestClass]
public class WriteViewModelTests
{
    private readonly Mock<IStoreService> _storeServiceMock;
    private readonly Mock<IAIService> _aiServiceMock;
    private readonly WriteViewModel _viewModel;

    private readonly AppConfig Settings = new();

    public WriteViewModelTests()
    {
        _storeServiceMock = new Mock<IStoreService>();
        _aiServiceMock = new Mock<IAIService>();
        _viewModel = new WriteViewModel(_storeServiceMock.Object, _aiServiceMock.Object);
        _viewModel.ConversationContext = new TabData();
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

        // Act
        await _viewModel.SubmitCommand.ExecuteAsync(null);

        // Assert
        Check.That(_viewModel.Messages).HasSize(1);
        Check.That(_viewModel.Messages[0]).IsInstanceOf<InternalColloxMessage>();
        Check.That(((InternalColloxMessage)_viewModel.Messages[0]).Message).Contains("Available commands");
        Check.That(_viewModel.InputMessage).IsEmpty();
    }

    [TestMethod]
    public async Task Clear_RemovesAllMessages()
    {
        // Arrange
        _viewModel.Messages.Add(new TextColloxMessage { Text = "Message 1" });
        _viewModel.Messages.Add(new TextColloxMessage { Text = "Message 2" });

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
    public void ChangeModeToCmd_SetsCorrectIcon()
    {
        // Act
        _viewModel.ChangeModeToCmd();

        // Assert
        Check.That(_viewModel.SubmitModeIcon).IsEqualTo(Symbol.Play);
    }

    [TestMethod]
    public void ChangeModeToWrite_SetsCorrectIcon()
    {
        // Act
        _viewModel.ChangeModeToWrite();

        // Assert
        Check.That(_viewModel.SubmitModeIcon).IsEqualTo(Symbol.Send);
    }

    [TestMethod]
    public void SwitchMode_TogglesMode()
    {
        // Arrange
        _viewModel.SubmitModeIcon = Symbol.Send;

        // Act
        _viewModel.SwitchMode();

        // Assert
        Check.That(_viewModel.SubmitModeIcon).IsEqualTo(Symbol.Play);

        // Act again
        _viewModel.SwitchMode();

        // Assert
        Check.That(_viewModel.SubmitModeIcon).IsEqualTo(Symbol.Send);
    }

    [TestMethod]
    public void OnIsBeepingChanged_UpdatesSettings()
    {
        // Arrange
        bool originalValue = Settings.AutoBeep;

        try
        {
            // Act
            _viewModel.IsBeeping = !originalValue;

            // Assert
            Check.That(Settings.AutoBeep).IsEqualTo(_viewModel.IsBeeping);
        }
        finally
        {
            // Cleanup
            Settings.AutoBeep = originalValue;
        }
    }

    [TestMethod]
    public void OnIsSpeakingChanged_UpdatesSettings()
    {
        // Arrange
        bool originalValue = Settings.AutoRead;

        try
        {
            // Act
            _viewModel.IsSpeaking = !originalValue;

            // Assert
            Check.That(Settings.AutoRead).IsEqualTo(_viewModel.IsSpeaking);
        }
        finally
        {
            // Cleanup
            Settings.AutoRead = originalValue;
        }
    }

    [TestMethod]
    public void OnSelectedVoiceChanged_UpdatesSettings()
    {
        // Arrange
        string originalVoice = Settings.Voice;
        var mockVoiceInfo = new Mock<VoiceInfo>();
        mockVoiceInfo.Setup(v => v.Name).Returns("TestVoice");

        try
        {
            // Act
            _viewModel.SelectedVoice = mockVoiceInfo.Object;

            // Assert
            Check.That(Settings.Voice).IsEqualTo("TestVoice");
        }
        finally
        {
            // Cleanup
            Settings.Voice = originalVoice;
        }
    }

    [TestMethod]
    public void OnSelectedMessageChanged_SendsMessage()
    {
        // Arrange
        bool messageReceived = false;
        WeakReferenceMessenger.Default.Register<MessageSelectedMessage>(this, (r, m) =>
        {
            messageReceived = true;
        });

        var message = new TextColloxMessage { Text = "Test" };

        // Act
        _viewModel.SelectedMessage = message;

        // Assert
        Check.That(messageReceived).IsTrue();

        // Cleanup
        WeakReferenceMessenger.Default.Unregister<MessageSelectedMessage>(this);
    }

    [TestMethod]
    public void Receive_TaskDoneMessage_RemovesTask()
    {
        // Arrange
        var task = new TaskViewModel { Name = "Test Task" };
        _viewModel.Tasks.Add(task);

        // Act
        _viewModel.Receive(new TaskDoneMessage(task));

        // Assert
        Check.That(_viewModel.Tasks).IsEmpty();
    }

    [TestMethod]
    public void OnAutoSuggestBoxQuerySubmitted_SelectsMessage()
    {
        // Arrange
        var message = new TextColloxMessage { Text = "Test message" };
        _viewModel.Messages.Add(message);
        var sender = new Mock<AutoSuggestBox>().Object;
        var args = new AutoSuggestBoxQuerySubmittedEventArgs
        {
            //QueryText = "Test message"
        };

        // Act
        _viewModel.OnAutoSuggestBoxQuerySubmitted(sender, args);

        // Assert
        Check.That(_viewModel.SelectedMessage).IsSameReferenceAs(message);
    }

    [TestMethod]
    public void StripMd_RemovesMarkdownFormatting()
    {
        // Use the internal StripMd method via reflection
        var method = typeof(WriteViewModel).GetMethod("StripMd", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        
        // Act
        var result = method.Invoke(null, new object[] { "# Header\n**Bold text**" }) as string;
        
        // Assert
        Check.That(result).Not.Contains("#");
        Check.That(result).Contains("Header");
        Check.That(result).Contains("Bold text");
        Check.That(result).Not.Contains("**");
    }
}
