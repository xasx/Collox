using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Collox.Services;
using CommunityToolkit.WinUI.Media;

namespace Collox.ViewModels;

public partial class TemplatesViewModel : ObservableObject
{
    [ObservableProperty]
    public partial string Name { get; set; }

    [ObservableProperty]
    public partial string Content { get; set; }

    [ObservableProperty]
    public partial Template SelectedTemplate { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<Template> Templates { get; set; } = [];

    public bool IsEditing { get; set; }

    public Template TemplateToEdit { get; set; }

    private ITemplateService templateService = App.GetService<ITemplateService>();

    [RelayCommand]
    public async Task SaveTemplate()
    {
        if (IsEditing)
        {
            await templateService.SaveTemplate(Name, Content);
            //Templates.Remove(TemplateToEdit);
            TemplateToEdit.Name = Name;
            TemplateToEdit.Content = Content;
            
            TemplateToEdit = null;
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
    public async Task EditTemplate()
    {
        Name = SelectedTemplate.Name;
        Content = SelectedTemplate.Content;
        TemplateToEdit = SelectedTemplate;
        IsEditing = true;
    }

    [RelayCommand]
    public async Task DuplicateTemplate()
    {
        var dt = new Template()
        {
            Name = SelectedTemplate.Name + " - Duplicate",
            Content = SelectedTemplate.Content
        };

        
        await templateService.SaveTemplate(dt.Name, dt.Content);

        Templates.Add(dt);
    }
}

public partial class Template : ObservableObject
{
    [ObservableProperty]
    public partial string Name { get; set; } = "default";

    [ObservableProperty]
    public partial string Content { get; set; } = "# Generic template 01";

}
