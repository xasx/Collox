using Collox.Models;
using Collox.Services;
using Collox.ViewModels;
using Collox.ViewModels.Messages;
using CommunityToolkit.Mvvm.Messaging;
using Moq;
using NFluent;

namespace Collox.Tests.ViewModels;

[TestClass]
[DoNotParallelize]
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
    public async Task LoadTemplates_WhenEmpty_ClearsExistingTemplates()
    {
        // Arrange
        _viewModel.Templates.Add(new Template { Name = "Old", Content = "OldContent" });
        _templateServiceMock.Setup(s => s.LoadTemplates())
            .ReturnsAsync(new Dictionary<string, MarkdownTemplate>());

        // Act
        await _viewModel.LoadTemplates();

        // Assert
        Check.That(_viewModel.Templates).IsEmpty();
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
    public async Task SaveTemplate_WithEmptyName_DoesNotSave()
    {
        // Arrange
        _viewModel.Name = string.Empty;
        _viewModel.Content = "SomeContent";

        // Act
        await _viewModel.SaveTemplate();

        // Assert
        Check.That(_viewModel.Templates).IsEmpty();
        _templateServiceMock.Verify(s => s.SaveTemplate(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task SaveTemplate_WithWhitespaceName_DoesNotSave()
    {
        // Arrange
        _viewModel.Name = "   ";
        _viewModel.Content = "SomeContent";

        // Act
        await _viewModel.SaveTemplate();

        // Assert
        Check.That(_viewModel.Templates).IsEmpty();
        _templateServiceMock.Verify(s => s.SaveTemplate(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task SaveTemplate_AfterSaving_ClearsNameAndContent()
    {
        // Arrange
        _viewModel.Name = "TemplateName";
        _viewModel.Content = "TemplateContent";

        // Act
        await _viewModel.SaveTemplate();

        // Assert
        Check.That(_viewModel.Name).IsEmpty();
        Check.That(_viewModel.Content).IsEmpty();
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

    [TestMethod]
    public async Task SaveTemplate_AfterEditing_ResetsEditingState()
    {
        // Arrange
        var existingTemplate = new Template { Name = "Template", Content = "Content" };
        _viewModel.Templates.Add(existingTemplate);
        _viewModel.TemplateToEdit = existingTemplate;
        _viewModel.IsEditing = true;
        _viewModel.Name = "Updated";
        _viewModel.Content = "UpdatedContent";

        // Act
        await _viewModel.SaveTemplate();

        // Assert
        Check.That(_viewModel.IsEditing).IsFalse();
        Check.That(_viewModel.TemplateToEdit).IsNull();
    }

    [TestMethod]
    public async Task TemplateDeletedMessage_RemovesTemplateAndCallsService()
    {
        // Arrange
        var template = new Template { Name = "ToDelete", Content = "Content" };
        _viewModel.Templates.Add(template);
        _templateServiceMock.Setup(s => s.DeleteTemplate("ToDelete")).Returns(Task.CompletedTask);

        // Act
        WeakReferenceMessenger.Default.Send(new TemplateDeletedMessage(template));
        await Task.Yield(); // allow async handler to run

        // Assert
        Check.That(_viewModel.Templates).IsEmpty();
        _templateServiceMock.Verify(s => s.DeleteTemplate("ToDelete"), Times.Once);
    }

    [TestMethod]
    public void TemplateEditedMessage_SetsEditingState()
    {
        // Arrange
        var template = new Template { Name = "ToEdit", Content = "OldContent" };

        // Act
        WeakReferenceMessenger.Default.Send(new TemplateEditedMessage(template));

        // Assert
        Check.That(_viewModel.IsEditing).IsTrue();
        Check.That(_viewModel.TemplateToEdit).IsEqualTo(template);
        Check.That(_viewModel.Name).IsEqualTo("ToEdit");
        Check.That(_viewModel.Content).IsEqualTo("OldContent");
    }

    [TestMethod]
    public void Dispose_CanBeCalledMultipleTimes_WithoutException()
    {
        // Act & Assert - should not throw
        _viewModel.Dispose();
        _viewModel.Dispose();
    }
}
