using Collox.ViewModels;
using Collox.ViewModels.Messages;
using Moq;
using NFluent;
using System.Collections.ObjectModel;

namespace Collox.Tests.ViewModels;

[TestClass]
public class MirrorViewModelTests
{
    private readonly MirrorViewModel _viewModel;
    private readonly Mock<Collox.ViewModels.ITimer> _timerMock = new();

    public MirrorViewModelTests()
    {
        _viewModel = new MirrorViewModel();
        _timerMock = new Mock<Collox.ViewModels.ITimer>();

        MessageRelativeTimeUpdater.CreateTimer = () => _timerMock.Object;
    }

    [TestMethod]
    public void Clear_ClearsFilteredMessages()
    {
        // Arrange
        _viewModel.FilteredMessages.Add(new TextColloxMessage { Text = "Message 1" });
        _viewModel.FilteredMessages.Add(new TextColloxMessage { Text = "Message 2" });

        // Act
        _viewModel.Clear();

        // Assert
        Check.That(_viewModel.FilteredMessages).IsEmpty();
    }

    [TestMethod]
    public void Receive_AddsMessageToCollections()
    {
        // Arrange
        var message = new TextSubmittedMessage(
            new TextColloxMessage() { Text = "Test Message", Context = "Test Context" });

        // Act
        _viewModel.Receive(message);

        // Assert
        Check.That(_viewModel.Messages).HasSize(1);
        Check.That(_viewModel.Messages[0].Text).IsEqualTo("Test Message");
        Check.That(_viewModel.FilteredMessages).HasSize(1);
        Check.That(_viewModel.FilteredMessages[0].Text).IsEqualTo("Test Message");
        Check.That(_viewModel.Contexts).Contains("Test Context");
    }

    [TestMethod]
    public void FilterMessages_FiltersBySelectedContexts()
    {
        // Arrange
        _viewModel.Messages.Add(new TextColloxMessage() { Text = "Message 1", Context = "Context 1" });
        _viewModel.Messages.Add(new TextColloxMessage() { Text = "Message 2", Context = "Context 2" });

        _viewModel.SelectedContexts = new ObservableCollection<string> { "Context 1" };

        // Act
        _viewModel.FilterMessages();

        // Assert
        Check.That(_viewModel.FilteredMessages).HasSize(1);
        Check.That(_viewModel.FilteredMessages[0].Text).IsEqualTo("Message 1");
    }

    [TestMethod]
    public void OnSelectedContextsChanged_UpdatesFilteredMessages()
    {
        // Arrange
        _viewModel.Messages.Add(new TextColloxMessage() { Text = "Message 1", Context = "Context 1" });
        _viewModel.Messages.Add(new TextColloxMessage() { Text = "Message 2", Context = "Context 2" });

        // Act
        _viewModel.SelectedContexts = new ObservableCollection<string> { "Context 2" };

        // Assert
        Check.That(_viewModel.FilteredMessages).HasSize(1);
        Check.That(_viewModel.FilteredMessages[0].Text).IsEqualTo("Message 2");
    }

    [TestMethod]
    public void Receive_WhenMessagesExceedTwenty_CapsAtTwenty()
    {
        // Arrange & Act — send 21 messages
        for (int i = 1; i <= 21; i++)
        {
            _viewModel.Receive(new TextSubmittedMessage(
                new TextColloxMessage { Text = $"Message {i}", Context = "ctx" }));
        }

        // Assert
        Check.That(_viewModel.Messages).HasSize(20);
        Check.That(_viewModel.FilteredMessages).HasSize(20);
        // The oldest message should have been dropped
        Check.That(_viewModel.Messages[0].Text).IsEqualTo("Message 2");
    }

    [TestMethod]
    public void FilterMessages_WithAllContextSelected_ShowsAllMessages()
    {
        // Arrange
        _viewModel.Messages.Add(new TextColloxMessage { Text = "Message 1", Context = "Context 1" });
        _viewModel.Messages.Add(new TextColloxMessage { Text = "Message 2", Context = "Context 2" });
        _viewModel.SelectedContexts = new ObservableCollection<string> { "All" };

        // Act
        _viewModel.FilterMessages();

        // Assert
        Check.That(_viewModel.FilteredMessages).HasSize(2);
    }

    [TestMethod]
    public void FilterMessages_WithNoMatchingContext_ShowsNoMessages()
    {
        // Arrange
        _viewModel.Messages.Add(new TextColloxMessage { Text = "Message 1", Context = "Context 1" });
        _viewModel.SelectedContexts = new ObservableCollection<string> { "NonExistent" };

        // Act
        _viewModel.FilterMessages();

        // Assert
        Check.That(_viewModel.FilteredMessages).IsEmpty();
    }

    [TestMethod]
    public void Receive_NewContext_AddsContextToContextsCollection()
    {
        // Act
        _viewModel.Receive(new TextSubmittedMessage(
            new TextColloxMessage { Text = "Hi", Context = "NewCtx" }));

        // Assert
        Check.That(_viewModel.Contexts).Contains("NewCtx");
    }

    [TestMethod]
    public void Receive_ExistingContext_DoesNotDuplicateInContextsCollection()
    {
        // Arrange
        _viewModel.Receive(new TextSubmittedMessage(
            new TextColloxMessage { Text = "First", Context = "Ctx" }));

        // Act
        _viewModel.Receive(new TextSubmittedMessage(
            new TextColloxMessage { Text = "Second", Context = "Ctx" }));

        // Assert
        Check.That(_viewModel.Contexts.Count(c => c == "Ctx")).IsEqualTo(1);
    }

    [TestMethod]
    public void Dispose_CanBeCalledMultipleTimes_WithoutException()
    {
        // Act & Assert - should not throw
        _viewModel.Dispose();
        _viewModel.Dispose();
    }
}
