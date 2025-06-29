using Collox.Services;
using Collox.ViewModels;
using Moq;
using NFluent;
using System.Linq;

namespace Collox.Tests.ViewModels;

[TestClass]
public class TabWriteViewModelTests
{
    private readonly Mock<ITabContextService> _tabContextServiceMock;
    private readonly Mock<IAIService> _aiServiceMock;
    private readonly TabWriteViewModel _viewModel;

    public TabWriteViewModelTests()
    {
        _tabContextServiceMock = new Mock<ITabContextService>();
        _aiServiceMock = new Mock<IAIService>();
        _viewModel = new TabWriteViewModel(_tabContextServiceMock.Object, _aiServiceMock.Object);
    }

    [TestMethod]
    public void AddNewTab_AddsTabToTabsCollection()
    {
        // Act
        _viewModel.AddNewTab();

        // Assert
        Check.That(_viewModel.Tabs).HasSize(2);
        Check.That(_viewModel.Tabs[1].Context).IsEqualTo("Context 2");
        Check.That(_viewModel.Tabs[1].IsCloseable).IsTrue();
        Check.That(_viewModel.Tabs[1].IsEditing).IsTrue();
        _tabContextServiceMock.Verify(s => s.SaveNewTab(It.IsAny<TabContext>()), Times.Once);
    }

    [TestMethod]
    public void CloseSelectedTab_RemovesTabFromTabsCollection()
    {
        // Arrange
        _viewModel.AddNewTab();
        var tabToClose = _viewModel.Tabs[1];
        _viewModel.SelectedTab = tabToClose;

        // Act
        _viewModel.CloseSelectedTab();

        // Assert
        Check.That(_viewModel.Tabs).HasSize(1);
        Check.That(_viewModel.Tabs[0].Context).IsEqualTo("Default");
        _tabContextServiceMock.Verify(s => s.RemoveTab(It.IsAny<TabContext>()), Times.Once);
    }

    [TestMethod]
    public void LoadTabs_PopulatesTabsCollection()
    {
        // Arrange
        var mockTabContexts = new List<TabContext>
        {
            new TabContext { Name = "Tab1", IsCloseable = true },
            new TabContext { Name = "Tab2", IsCloseable = true }
        };

        _tabContextServiceMock.Setup(s => s.GetTabs()).Returns(mockTabContexts);

        // Act
        _viewModel.LoadTabs();

        // Assert
        Check.That(_viewModel.Tabs).HasSize(3); // Includes initialTab
        Check.That(_viewModel.Tabs[1].Context).IsEqualTo("Tab1");
        Check.That(_viewModel.Tabs[2].Context).IsEqualTo("Tab2");
    }

    [TestMethod]
    public void UpdateContext_UpdatesTabContext()
    {
        // Arrange
        _viewModel.AddNewTab();

        var tabData = _viewModel.Tabs[1];
        tabData.Context = "UpdatedTab";

        // Act
        _viewModel.UpdateContext(tabData);

        // Assert
        _tabContextServiceMock.Verify(s => s.NotifyTabUpdate(It.Is<TabContext>(tc => tc.Name == "UpdatedTab" && tc.IsCloseable)), Times.Once);
    }
}
