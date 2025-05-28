using System.Collections.ObjectModel;
using Collox.Services;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Cottle;

namespace Collox.ViewModels;

public partial class Template : ObservableObject
{
    private readonly ITemplateService templateService = App.GetService<ITemplateService>();

    [ObservableProperty] public partial string Content { get; set; } = "# Generic template 01";
    [ObservableProperty] public partial string Name { get; set; } = "default";
    [RelayCommand]
    public async Task DeleteTemplate()
    {
        await templateService.DeleteTemplate(Name);
        WeakReferenceMessenger.Default.Send(new TemplateDeletedMessage(this));
    }

    [RelayCommand]
    public async Task DuplicateTemplate()
    {
        var duplicateName = $"{Name} - Duplicate";
        var duplicateContent = Content;
        var dt = new Template
        {
            Name = duplicateName,
            Content = duplicateContent
        };

        await templateService.SaveTemplate(dt.Name, dt.Content);

        WeakReferenceMessenger.Default.Send(new TemplateAddedMessage(dt));
    }

    [RelayCommand]
    public void EditTemplate()
    {
        WeakReferenceMessenger.Default.Send(new TemplateEditedMessage(this));
    }
}

public partial class TemplatesViewModel : ObservableObject
{
    private readonly ITemplateService templateService;

    public TemplatesViewModel(ITemplateService templateService)
    {
        this.templateService = templateService;
    }

    public TemplatesViewModel()
    {
        WeakReferenceMessenger.Default.Register<TemplateAddedMessage>(this, (r, m) => Templates.Add(m.Value));
        WeakReferenceMessenger.Default.Register<TemplateDeletedMessage>(this, (r, m) => Templates.Remove(m.Value));
        WeakReferenceMessenger.Default.Register<TemplateEditedMessage>(this, (r, m) =>
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
            var t = new Template
            {
                Name = templateEntry.Value.Name,
                Content = templateEntry.Value.Content
            };
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
            var t = new Template
            {
                Name = Name,
                Content = Content
            };
            Templates.Add(t);
        }

        Name = string.Empty;
        Content = string.Empty;
    }
}
