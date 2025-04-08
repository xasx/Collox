namespace Collox.Common;

public partial class MessageTemplateSelector : DataTemplateSelector
{
    public DataTemplate DefaultTemplate { get; set; }
    public DataTemplate TextTemplate { get; set; }
    public DataTemplate TimeTemplate { get; set; }
    public DataTemplate InternalTemplate { get; set; }

    protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
    {
        if (item is TextColloxMessage)
        {
            return TextTemplate;
        }

        if (item is TimeColloxMessage)
        {
            return TimeTemplate;
        }

        if (item is InternalColloxMessage)
        {
            return InternalTemplate;
        }

        return DefaultTemplate;
    }
}
