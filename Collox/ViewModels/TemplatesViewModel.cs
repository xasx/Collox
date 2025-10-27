using System.Collections.ObjectModel;
using Collox.Services;
using Collox.ViewModels.Messages;
using CommunityToolkit.Mvvm.Messaging;

namespace Collox.ViewModels;

public partial class TemplatesViewModel : ObservableObject, IDisposable
{
    private readonly ITemplateService templateService;
    private bool disposed;

    public TemplatesViewModel(ITemplateService templateService)
    {
        this.templateService = templateService;


        WeakReferenceMessenger.Default
            .Register<TemplateAddedMessage>(
                this,
                async (r, m) =>
                {
                    Templates.Add(m.Value);
                    await templateService.SaveTemplate(m.Value.Name, m.Value.Content);
                });
        WeakReferenceMessenger.Default
            .Register<TemplateDeletedMessage>(
                this,
                async (r, m) =>
                {
                    Templates.Remove(m.Value);
                    await templateService.DeleteTemplate(m.Value.Name);
                });
        WeakReferenceMessenger.Default
            .Register<TemplateEditedMessage>(
                this,
                (r, m) =>
                {
                    Name = m.Value.Name;
                    Content = m.Value.Content;
                    TemplateToEdit = m.Value;
                    IsEditing = true;
                });
    }

    [ObservableProperty] public partial string Content { get; set; }

    public bool IsEditing { get; set; }

    [ObservableProperty] public partial string Name { get; set; }

    [ObservableProperty] public partial Template SelectedTemplate { get; set; }

    [ObservableProperty] public partial ObservableCollection<Template> Templates { get; set; } = [];

    public Template TemplateToEdit { get; set; }

    [RelayCommand]
    public async Task LoadTemplates()
    {
        var templates = await templateService.LoadTemplates();
        Templates.Clear();
        foreach (var templateEntry in templates)
        {
            var t = new Template { Name = templateEntry.Value.Name, Content = templateEntry.Value.Content };
            Templates.Add(t);
        }
    }

    [RelayCommand]
    public async Task SaveTemplate()
    {
        if (Name.IsWhiteSpace() || Name?.Length == 0)
        {
            return;
        }

        if (IsEditing)
        {
            await templateService.EditTemplate(TemplateToEdit.Name, Name, Content);
            //Templates.Remove(TemplateToEdit);
            TemplateToEdit.Name = Name;
            TemplateToEdit.Content = Content;

            TemplateToEdit = null;
            IsEditing = false;
        }
        else
        {
            await templateService.SaveTemplate(Name, Content);
            var t = new Template { Name = Name, Content = Content };
            Templates.Add(t);
        }

        Name = string.Empty;
        Content = string.Empty;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposed)
        {
            if (disposing)
            {
                WeakReferenceMessenger.Default.Unregister<TemplateAddedMessage>(this);
                WeakReferenceMessenger.Default.Unregister<TemplateDeletedMessage>(this);
                WeakReferenceMessenger.Default.Unregister<TemplateEditedMessage>(this);
            }

            disposed = true;
        }
    }
}
