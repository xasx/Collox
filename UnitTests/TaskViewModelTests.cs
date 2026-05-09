using Collox.ViewModels;
using Collox.ViewModels.Messages;
using CommunityToolkit.Mvvm.Messaging;
using NFluent;

namespace Collox.Tests.ViewModels;

[TestClass]
[DoNotParallelize]
public class TaskViewModelTests
{
    [TestMethod]
    public void ImplicitConversion_FromString_CreatesTaskWithNameAndNotDone()
    {
        // Act
        TaskViewModel task = "Write tests";

        // Assert
        Check.That(task.Name).IsEqualTo("Write tests");
        Check.That(task.IsDone).IsFalse();
    }

    [TestMethod]
    public void IsDone_WhenSetToTrue_SendsTaskDoneMessage()
    {
        // Arrange
        var task = new TaskViewModel { Name = "Test task", IsDone = false };
        TaskDoneMessage receivedMessage = null;
        WeakReferenceMessenger.Default.Register<TaskDoneMessage>(
            this, (r, m) => receivedMessage = m);

        try
        {
            // Act
            task.IsDone = true;

            // Assert
            Check.That(receivedMessage).IsNotNull();
            Check.That(receivedMessage.Value).IsEqualTo(task);
        }
        finally
        {
            WeakReferenceMessenger.Default.Unregister<TaskDoneMessage>(this);
        }
    }

    [TestMethod]
    public void IsDone_WhenSetToFalse_DoesNotSendTaskDoneMessage()
    {
        // Arrange
        var task = new TaskViewModel { Name = "Test task", IsDone = false };
        bool messageReceived = false;
        WeakReferenceMessenger.Default.Register<TaskDoneMessage>(
            this, (r, m) => messageReceived = true);

        try
        {
            // Act
            task.IsDone = false;

            // Assert
            Check.That(messageReceived).IsFalse();
        }
        finally
        {
            WeakReferenceMessenger.Default.Unregister<TaskDoneMessage>(this);
        }
    }

    [TestMethod]
    public void IsDone_WhenSetFromTrueToFalse_DoesNotSendAnotherMessage()
    {
        // Arrange
        var task = new TaskViewModel { Name = "Test task", IsDone = true };
        int messageCount = 0;
        WeakReferenceMessenger.Default.Register<TaskDoneMessage>(
            this, (r, m) => messageCount++);

        try
        {
            // Act
            task.IsDone = false;

            // Assert
            Check.That(messageCount).IsEqualTo(0);
        }
        finally
        {
            WeakReferenceMessenger.Default.Unregister<TaskDoneMessage>(this);
        }
    }

    [TestMethod]
    public void Name_SetterUpdatesProperty()
    {
        // Arrange
        var task = new TaskViewModel();

        // Act
        task.Name = "Updated Task Name";

        // Assert
        Check.That(task.Name).IsEqualTo("Updated Task Name");
    }

    [TestMethod]
    public void Constructor_DefaultValues_NameIsNullAndIsDoneIsFalse()
    {
        // Act
        var task = new TaskViewModel();

        // Assert
        Check.That(task.Name).IsNull();
        Check.That(task.IsDone).IsFalse();
    }
}
