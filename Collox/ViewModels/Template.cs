using Collox.ViewModels.Messages;
using CommunityToolkit.Mvvm.Messaging;

namespace Collox.ViewModels;

public partial class Template : ObservableObject
{
    [ObservableProperty] public partial string Content { get; set; } = "# Generic template 01";

    [ObservableProperty] public partial string Name { get; set; } = "default";

    [RelayCommand]
    public void DeleteTemplate() { WeakReferenceMessenger.Default.Send(new TemplateDeletedMessage(this)); }

    [RelayCommand]
    public void DuplicateTemplate()
    {
        var duplicateName = $"{Name} - Duplicate";
        var duplicateContent = Content;
        var dt = new Template { Name = duplicateName, Content = duplicateContent };

        WeakReferenceMessenger.Default.Send(new TemplateAddedMessage(dt));
    }

    [RelayCommand]
    public void EditTemplate() { WeakReferenceMessenger.Default.Send(new TemplateEditedMessage(this)); }
}
