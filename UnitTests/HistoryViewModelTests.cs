using Collox.Models;
using Collox.Services;
using Collox.ViewModels;
using Moq;
using NFluent;

namespace Collox.Tests.ViewModels;

[TestClass]
public class HistoryViewModelTests
{
    private readonly Mock<IStoreService> _storeServiceMock;
    private readonly HistoryViewModel _viewModel;

    public HistoryViewModelTests()
    {
        _storeServiceMock = new Mock<IStoreService>();
        _viewModel = new HistoryViewModel(_storeServiceMock.Object);
    }

    [TestMethod]
    public async Task LoadHistory_PopulatesHistories()
    {
        // Arrange
        var mockData = new Dictionary<string, ICollection<MarkdownRecording>>
        {
            {
                "January", new List<MarkdownRecording>
                {
                    new MarkdownRecording
                    {
                        Date = new DateOnly(2023, 1, 1),
                        Preview = "Preview 1",
                        Content = () => "Content 1"
                    },
                    new MarkdownRecording
                    {
                        Date = new DateOnly(2023, 1, 2),
                        Preview = "Preview 2",
                        Content = () => "Content 2"
                    }
                }
            }
        };

        _storeServiceMock.Setup(s => s.Load(It.IsAny<CancellationToken>())).ReturnsAsync(mockData);

        // Act
        await _viewModel.LoadHistory();

        // Assert
        Check.That(_viewModel.Histories).HasSize(1);
        Check.That(_viewModel.Histories[0].Key).IsEqualTo("January");
        Check.That(_viewModel.Histories[0]).HasSize(2);

        // the order is reversed
        Check.That(_viewModel.Histories[0][0].Preview).IsEqualTo("Preview 2");
        Check.That(_viewModel.Histories[0][1].Preview).IsEqualTo("Preview 1");
    }

    [TestMethod]
    public void SelectedHistoryEntry_SetterUpdatesProperty()
    {
        // Arrange
        var historyEntry = new HistoryEntry
        {
            Day = new DateOnly(2023, 1, 1),
            Preview = "Preview",
            Content = new Lazy<string>(() => "Content")
        };

        // Act
        _viewModel.SelectedHistoryEntry = historyEntry;

        // Assert
        Check.That(_viewModel.SelectedHistoryEntry).IsEqualTo(historyEntry);
    }

    [TestMethod]
    public async Task LoadHistory_WithEmptyData_LeavesHistoriesEmpty()
    {
        // Arrange
        _storeServiceMock.Setup(s => s.Load(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, ICollection<MarkdownRecording>>());

        // Act
        await _viewModel.LoadHistory();

        // Assert
        Check.That(_viewModel.Histories).IsEmpty();
    }

    [TestMethod]
    public async Task LoadHistory_WithMultipleMonths_GroupsAndReversesSections()
    {
        // Arrange
        var mockData = new Dictionary<string, ICollection<MarkdownRecording>>
        {
            {
                "January", new List<MarkdownRecording>
                {
                    new MarkdownRecording { Date = new DateOnly(2023, 1, 1), Preview = "Jan 1", Content = () => "" }
                }
            },
            {
                "February", new List<MarkdownRecording>
                {
                    new MarkdownRecording { Date = new DateOnly(2023, 2, 1), Preview = "Feb 1", Content = () => "" }
                }
            }
        };

        _storeServiceMock.Setup(s => s.Load(It.IsAny<CancellationToken>())).ReturnsAsync(mockData);

        // Act
        await _viewModel.LoadHistory();

        // Assert — dictionary is reversed, so February comes first
        Check.That(_viewModel.Histories).HasSize(2);
        Check.That(_viewModel.Histories[0].Key).IsEqualTo("February");
        Check.That(_viewModel.Histories[1].Key).IsEqualTo("January");
    }

    [TestMethod]
    public async Task LoadHistory_CalledTwice_DoesNotDuplicateEntries()
    {
        // Arrange
        var mockData = new Dictionary<string, ICollection<MarkdownRecording>>
        {
            {
                "January", new List<MarkdownRecording>
                {
                    new MarkdownRecording { Date = new DateOnly(2023, 1, 1), Preview = "Preview", Content = () => "" }
                }
            }
        };
        _storeServiceMock.Setup(s => s.Load(It.IsAny<CancellationToken>())).ReturnsAsync(mockData);

        // Act
        await _viewModel.LoadHistory();
        await _viewModel.LoadHistory();

        // Assert
        Check.That(_viewModel.Histories).HasSize(1);
        Check.That(_viewModel.Histories[0]).HasSize(1);
    }
}
