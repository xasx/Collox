using Collox.Models;
using Collox.Services;
using Collox.ViewModels;
using Microsoft.Extensions.AI;
using Moq;
using NFluent; // Added for NFluent assertions

namespace Collox.Tests.ViewModels;

[TestClass]
public class WriteViewModelAITests
{
    private readonly Mock<IStoreService> _storeServiceMock;
    private readonly Mock<IAIService> _aiServiceMock;
    private readonly Mock<IChatClient> _chatClientMock;
    private readonly WriteViewModel _viewModel;

    private readonly Mock<Collox.ViewModels.ITimer> _timerMock;
    public WriteViewModelAITests()
    {
        _storeServiceMock = new Mock<IStoreService>();
        _aiServiceMock = new Mock<IAIService>();
        _chatClientMock = new Mock<IChatClient>();

        _aiServiceMock.Setup(a => a.GetChatClient(It.IsAny<AIProvider>(), It.IsAny<string>()))
            .Returns(_chatClientMock.Object);

        _viewModel = new WriteViewModel(_storeServiceMock.Object, _aiServiceMock.Object);
        _viewModel.ConversationContext = new TabData();
        _timerMock = new Mock<Collox.ViewModels.ITimer>();
        MessageRelativeTimeUpdater.CreateTimer = () => _timerMock.Object;
    }

    [TestMethod]
    public async Task CreateComment_AddsCommentToMessage()
    {
        // Arrange
        var processor = new IntelligentProcessor
        {
            Id = Guid.NewGuid(),
            Name = "Test Processor",
            Prompt = "Comment on: {0}",
            Target = Target.Comment
        };

        var message = new TextColloxMessage { Text = "Test message" };

        // Setup streaming response
        _chatClientMock.Setup(c => c.GetStreamingResponseAsync(
            It.IsAny<IEnumerable<ChatMessage>>(),
            It.IsAny<ChatOptions>(),
            It.IsAny<CancellationToken>()
        )).Returns(GetMockStreamingResponse("This is a comment"));

        // Use reflection to call the private method
        var method = typeof(WriteViewModel).GetMethod("CreateComment",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        // Act
        var task = (Task<string>)method.Invoke(null, new object[] { message, processor, _chatClientMock.Object });
        await task;

        // Assert
        Check.That(message.Comments).HasSize(1);
        Check.That(message.Comments[0].Comment).IsEqualTo("This is a comment");
        Check.That(message.Comments[0].GeneratorId).IsEqualTo(processor.Id);
    }

    [TestMethod]
    public async Task CreateTask_AddsTaskToCollection()
    {
        // Arrange
        var processor = new IntelligentProcessor
        {
            Id = Guid.NewGuid(),
            Name = "Test Processor",
            Prompt = "Create task: {0}",
            Target = Target.Task
        };

        var message = new TextColloxMessage { Text = "Create a task" };

        // Fix for CS0854: Replace the invocation with a lambda expression to avoid using optional arguments in expression trees.
        _chatClientMock.Setup(c => c.GetResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), null, default))
            .ReturnsAsync(new ChatResponse (new ChatMessage(ChatRole.Assistant, "New task")));

        // Use reflection to call the private method
        var method = typeof(WriteViewModel).GetMethod("CreateTask",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        // Act
        var task = (Task<string>)method.Invoke(_viewModel, new object[]
            { message, processor.Prompt, _chatClientMock.Object });
        await task;

        // Assert
        Check.That(_viewModel.Tasks).HasSize(1);
        Check.That(_viewModel.Tasks[0].Name).IsEqualTo("New task");
        Check.That(_viewModel.Tasks[0].IsDone).IsFalse();
    }

    [TestMethod]
    public async Task CreateMessage_AddsGeneratedMessage()
    {
        // Arrange
        var processor = new IntelligentProcessor
        {
            Id = Guid.NewGuid(),
            Name = "Test Processor",
            Prompt = "You are a helpful assistant",
            Target = Target.Chat
        };

        var textColloxMessage = new TextColloxMessage { Text = "Hello" };
        _viewModel.Messages.Add(textColloxMessage);
        var existingMessages = new List<TextColloxMessage>
        {
            textColloxMessage
        };

        // Setup streaming response
        _chatClientMock.Setup(c => c.GetStreamingResponseAsync(
            It.IsAny<IEnumerable<ChatMessage>>(),
            It.IsAny<ChatOptions>(),
            It.IsAny<CancellationToken>()
        )).Returns(GetMockStreamingResponse("Hi there!"));

        // Use reflection to call the private method
        var method = typeof(WriteViewModel).GetMethod("CreateMessage",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        // Act
        var task = (Task<string>)method.Invoke(_viewModel, new object[]
            { existingMessages, processor, _chatClientMock.Object });
        var result = await task;

        // Assert
        Check.That(result).IsEqualTo("Hi there!");
        Check.That(_viewModel.Messages).HasSize(2); // Original + generated

        var generatedMessage = _viewModel.Messages[1] as TextColloxMessage;
        Check.That(generatedMessage).IsNotNull();
        Check.That(generatedMessage.Text).IsEqualTo("Hi there!");
        Check.That(generatedMessage.IsGenerated).IsTrue();
        Check.That(generatedMessage.GeneratorId).IsEqualTo(processor.Id);
        Check.That(generatedMessage.IsLoading).IsFalse();
    }

    // Helper method to simulate streaming responses
    private async IAsyncEnumerable<ChatResponseUpdate> GetMockStreamingResponse(string text)
    {
        foreach (var character in text)
        {
            yield return new ChatResponseUpdate(ChatRole.Assistant, $"{character}");
            await Task.Delay(1); // Keep a small delay for async behavior
        }
    }
}
