using Collox.Models;
using Collox.Services;
using Collox.ViewModels;
using Moq;
using NFluent;

namespace Collox.Tests.ViewModels;

[TestClass]
public class TemplatesViewModelTests
{
    private readonly Mock<ITemplateService> _templateServiceMock;
    private readonly TemplatesViewModel _viewModel;

    public TemplatesViewModelTests()
    {
        _templateServiceMock = new Mock<ITemplateService>();
        _viewModel = new TemplatesViewModel(_templateServiceMock.Object);
    }

    [TestMethod]
    public async Task LoadTemplates_PopulatesTemplates()
    {
        // Arrange
        var mockTemplates = new Dictionary<string, MarkdownTemplate>
        {
            { "Template1", new MarkdownTemplate { Name = "Template1", Content = "Content1" } },
            { "Template2", new MarkdownTemplate { Name = "Template2", Content = "Content2" } }
        };

        _templateServiceMock.Setup(s => s.LoadTemplates()).ReturnsAsync(mockTemplates);

        // Act
        await _viewModel.LoadTemplates();

        // Assert
        Check.That(_viewModel.Templates).HasSize(2);
        Check.That(_viewModel.Templates[0].Name).IsEqualTo("Template1");
        Check.That(_viewModel.Templates[0].Content).IsEqualTo("Content1");
        Check.That(_viewModel.Templates[1].Name).IsEqualTo("Template2");
        Check.That(_viewModel.Templates[1].Content).IsEqualTo("Content2");
    }

    [TestMethod]
    public async Task SaveTemplate_AddsNewTemplate()
    {
        // Arrange
        _viewModel.Name = "NewTemplate";
        _viewModel.Content = "NewContent";

        // Act
        await _viewModel.SaveTemplate();

        // Assert
        Check.That(_viewModel.Templates).HasSize(1);
        Check.That(_viewModel.Templates[0].Name).IsEqualTo("NewTemplate");
        Check.That(_viewModel.Templates[0].Content).IsEqualTo("NewContent");
        _templateServiceMock.Verify(s => s.SaveTemplate("NewTemplate", "NewContent"), Times.Once);
    }

    [TestMethod]
    public async Task SaveTemplate_EditsExistingTemplate()
    {
        // Arrange
        var existingTemplate = new Template { Name = "ExistingTemplate", Content = "OldContent" };
        _viewModel.Templates.Add(existingTemplate);
        _viewModel.TemplateToEdit = existingTemplate;
        _viewModel.IsEditing = true;
        _viewModel.Name = "UpdatedTemplate";
        _viewModel.Content = "UpdatedContent";

        // Act
        await _viewModel.SaveTemplate();

        // Assert
        Check.That(_viewModel.Templates).HasSize(1);
        Check.That(_viewModel.Templates[0].Name).IsEqualTo("UpdatedTemplate");
        Check.That(_viewModel.Templates[0].Content).IsEqualTo("UpdatedContent");
        _templateServiceMock.Verify(s => s.EditTemplate("ExistingTemplate", "UpdatedTemplate", "UpdatedContent"), Times.Once);
    }
}
