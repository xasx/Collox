using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Collox.Services;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Collox.ViewModels;
public partial class WriteViewModel : ObservableObject
{

    private IStoreService storeService;
    public WriteViewModel()
    {
        storeService = App.GetService<IStoreService>();
    }
    [ObservableProperty]
    public partial string LastParagraph { get; set; } = string.Empty;

    [ObservableProperty]
    public partial ObservableCollection<Paragraph> Paragraphs { get; set; } = [];


    [RelayCommand]
    private async Task SubmitAsync()
    {
        var paragraph = new Paragraph()
        {
            Text = LastParagraph,
            Timestamp = DateTime.Now
        };
        Paragraphs.Add(paragraph);
        await storeService.AppendParagraph(paragraph.Text, paragraph.Timestamp);
        WeakReferenceMessenger.Default.Send(new TextSubmittedMessage(LastParagraph));
        LastParagraph = string.Empty;

    }
}

public class Paragraph
{
    public string Text { get; set; }

    public DateTime Timestamp { get; set; }
}


public class TextSubmittedMessage : ValueChangedMessage<string>
{
    public TextSubmittedMessage(string value) : base(value)
    {
    }
}
