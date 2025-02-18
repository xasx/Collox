using System.Collections.ObjectModel;
using Collox.Services;

namespace Collox.ViewModels;

public partial class TemplatesViewModel : ObservableObject
{
    private readonly ITemplateService templateService = App.GetService<ITemplateService>();

    [ObservableProperty] public partial string Name { get; set; }

    [ObservableProperty] public partial string Content { get; set; }

    [ObservableProperty] public partial Template SelectedTemplate { get; set; }

    [ObservableProperty] public partial ObservableCollection<Template> Templates { get; set; } = [];

    public bool IsEditing { get; set; }

    public Template TemplateToEdit { get; set; }

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

    [RelayCommand]
    public async Task DeleteTemplate()
    {
        await templateService.DeleteTemplate(SelectedTemplate.Name);
        Templates.Remove(SelectedTemplate);
        SelectedTemplate = null;
    }

    [RelayCommand]
    public void EditTemplate()
    {
        Name = SelectedTemplate.Name;
        Content = SelectedTemplate.Content;
        TemplateToEdit = SelectedTemplate;
        IsEditing = true;
    }

    [RelayCommand]
    public async Task DuplicateTemplate()
    {
        var dt = new Template
        {
            Name = SelectedTemplate.Name + " - Duplicate",
            Content = SelectedTemplate.Content
        };

        await templateService.SaveTemplate(dt.Name, dt.Content);

        Templates.Add(dt);
    }

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
}

public partial class Template : ObservableObject
{
    [ObservableProperty] public partial string Name { get; set; } = "default";

    [ObservableProperty] public partial string Content { get; set; } = "# Generic template 01";
}
