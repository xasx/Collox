using Collox.Services;
using Collox.ViewModels;
using Microsoft.UI.Xaml.Controls;
using Moq;
using NFluent;
using System.Collections.ObjectModel;

namespace Collox.Tests.ViewModels;

[TestClass]
public class CommandServiceTests
{
    private readonly CommandService _commandService;
    private readonly Mock<IStoreService> _storeServiceMock;
    private readonly Mock<IAudioService> _audioServiceMock;
    private readonly ObservableCollection<ColloxMessage> _messages;
    private readonly ObservableCollection<TaskViewModel> _tasks;
    private readonly TabData _conversationContext;
    private readonly CommandContext _commandContext;

    public CommandServiceTests()
    {
        _commandService = new CommandService();

        _storeServiceMock = new Mock<IStoreService>();
        _audioServiceMock = new Mock<IAudioService>();

        _messages = new ObservableCollection<ColloxMessage>();
        _tasks = new ObservableCollection<TaskViewModel>();
        _conversationContext = new TabData { Context = "Test", IsCloseable = true };

        _commandContext = new CommandContext
        {
            Messages = _messages,
            Tasks = _tasks,
            ConversationContext = _conversationContext,
            StoreService = _storeServiceMock.Object,
            AudioService = _audioServiceMock.Object
        };
    }

    [TestMethod]
    public async Task ProcessCommandAsync_Clear_ClearsMessagesAndSaves()
    {
        // Arrange
        _messages.Add(new TextColloxMessage { Text = "Message 1" });
        _messages.Add(new TextColloxMessage { Text = "Message 2" });
        _storeServiceMock.Setup(s => s.SaveNow(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Act
        var result = await _commandService.ProcessCommandAsync("clear", _commandContext);

        // Assert
        Check.That(result.Success).IsTrue();
        Check.That(_messages).IsEmpty();
        _storeServiceMock.Verify(s => s.SaveNow(It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task ProcessCommandAsync_Clear_WithExtraArgs_StillClearsMessages()
    {
        // Arrange
        _messages.Add(new TextColloxMessage { Text = "Message 1" });
        _storeServiceMock.Setup(s => s.SaveNow(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Act
        var result = await _commandService.ProcessCommandAsync("clear all", _commandContext);

        // Assert
        Check.That(result.Success).IsTrue();
        Check.That(_messages).IsEmpty();
    }

    [TestMethod]
    public async Task ProcessCommandAsync_Save_SavesWithoutClearingMessages()
    {
        // Arrange
        _messages.Add(new TextColloxMessage { Text = "Message 1" });
        _storeServiceMock.Setup(s => s.SaveNow(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Act
        var result = await _commandService.ProcessCommandAsync("save", _commandContext);

        // Assert
        Check.That(result.Success).IsTrue();
        Check.That(_messages).HasSize(1);
        _storeServiceMock.Verify(s => s.SaveNow(It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task ProcessCommandAsync_Time_ReturnsTimeMessage()
    {
        // Act
        var result = await _commandService.ProcessCommandAsync("time", _commandContext);

        // Assert
        Check.That(result.Success).IsTrue();
        Check.That(result.ResultMessage).IsInstanceOf<TimeColloxMessage>();
    }

    [TestMethod]
    public async Task ProcessCommandAsync_Help_ReturnsInformationalHelpMessage()
    {
        // Act
        var result = await _commandService.ProcessCommandAsync("help", _commandContext);

        // Assert
        Check.That(result.Success).IsTrue();
        Check.That(result.ResultMessage).IsInstanceOf<InternalColloxMessage>();
        var helpMessage = (InternalColloxMessage)result.ResultMessage;
        Check.That(helpMessage.Severity).IsEqualTo(InfoBarSeverity.Informational);
        Check.That(helpMessage.Message).Contains("clear");
        Check.That(helpMessage.Message).Contains("save");
        Check.That(helpMessage.Message).Contains("time");
    }

    [TestMethod]
    public async Task ProcessCommandAsync_Pin_SetsIsCloseableToFalse()
    {
        // Arrange
        _conversationContext.IsCloseable = true;

        // Act
        var result = await _commandService.ProcessCommandAsync("pin", _commandContext);

        // Assert
        Check.That(result.Success).IsTrue();
        Check.That(_conversationContext.IsCloseable).IsFalse();
    }

    [TestMethod]
    public async Task ProcessCommandAsync_Unpin_SetsIsCloseableToTrue()
    {
        // Arrange
        _conversationContext.IsCloseable = false;

        // Act
        var result = await _commandService.ProcessCommandAsync("unpin", _commandContext);

        // Assert
        Check.That(result.Success).IsTrue();
        Check.That(_conversationContext.IsCloseable).IsTrue();
    }

    [TestMethod]
    public async Task ProcessCommandAsync_Task_AddsMultiWordTaskToCollection()
    {
        // Act
        var result = await _commandService.ProcessCommandAsync("task Write unit tests", _commandContext);

        // Assert
        Check.That(result.Success).IsTrue();
        Check.That(_tasks).HasSize(1);
        Check.That(_tasks[0].Name).IsEqualTo("Write unit tests");
        Check.That(_tasks[0].IsDone).IsFalse();
    }

    [TestMethod]
    public async Task ProcessCommandAsync_Task_AddsSingleWordTaskToCollection()
    {
        // Act
        var result = await _commandService.ProcessCommandAsync("task MyTask", _commandContext);

        // Assert
        Check.That(result.Success).IsTrue();
        Check.That(_tasks).HasSize(1);
        Check.That(_tasks[0].Name).IsEqualTo("MyTask");
    }

    [TestMethod]
    public async Task ProcessCommandAsync_Task_AddsTaskWithEmptyName_WhenNoNameProvided()
    {
        // Act
        var result = await _commandService.ProcessCommandAsync("task", _commandContext);

        // Assert
        Check.That(result.Success).IsTrue();
        Check.That(_tasks).HasSize(1);
        Check.That(_tasks[0].Name).IsEqualTo(string.Empty);
    }

    [TestMethod]
    public async Task ProcessCommandAsync_UnknownCommand_ReturnsFailureWithErrorMessage()
    {
        // Act
        var result = await _commandService.ProcessCommandAsync("unknown", _commandContext);

        // Assert
        Check.That(result.Success).IsFalse();
        Check.That(result.ErrorMessage).Contains("unknown");
        Check.That(result.ResultMessage).IsNull();
    }

    [TestMethod]
    public async Task ProcessCommandAsync_Speak_WithTextMessages_CallsAudioService()
    {
        // Arrange
        _messages.Add(new TextColloxMessage { Text = "Hello world" });
        _audioServiceMock.Setup(a => a.ReadTextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _commandService.ProcessCommandAsync("speak", _commandContext);

        // Assert
        Check.That(result.Success).IsTrue();
        _audioServiceMock.Verify(
            a => a.ReadTextAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [TestMethod]
    public async Task ProcessCommandAsync_Speak_WithNoMessages_SucceedsWithoutCallingAudioService()
    {
        // Act
        var result = await _commandService.ProcessCommandAsync("speak", _commandContext);

        // Assert
        Check.That(result.Success).IsTrue();
        _audioServiceMock.Verify(
            a => a.ReadTextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [TestMethod]
    public async Task ProcessCommandAsync_Speak_WithOnlyNonTextMessages_SucceedsWithoutCallingAudioService()
    {
        // Arrange
        _messages.Add(new TimeColloxMessage { Time = TimeSpan.FromHours(12) });

        // Act
        var result = await _commandService.ProcessCommandAsync("speak", _commandContext);

        // Assert
        Check.That(result.Success).IsTrue();
        _audioServiceMock.Verify(
            a => a.ReadTextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [TestMethod]
    public async Task ProcessCommandAsync_Speak_SpeaksLastTextMessageWhenMultipleExist()
    {
        // Arrange
        _messages.Add(new TextColloxMessage { Text = "First message" });
        _messages.Add(new TextColloxMessage { Text = "Last message" });

        string spokenText = null;
        _audioServiceMock.Setup(a => a.ReadTextAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
            .Callback<string, string, CancellationToken>((text, voice, ct) => spokenText = text)
            .Returns(Task.CompletedTask);

        // Act
        await _commandService.ProcessCommandAsync("speak", _commandContext);

        // Assert
        Check.That(spokenText).IsEqualTo("Last message");
    }
}
