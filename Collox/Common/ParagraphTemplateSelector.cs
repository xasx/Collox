using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Collox.Common;

public class ParagraphTemplateSelector : DataTemplateSelector
{
    public DataTemplate DefaultTemplate { get; set; }
    public DataTemplate TextTemplate { get; set; }
    public DataTemplate TimeTemplate { get; set; }
    protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
    {
        if (item is TextColloxMessage)
        {
            return TextTemplate;
        }
        else if (item is TimeColloxMessage)
        {
            return TimeTemplate;
        }
        return DefaultTemplate;
    }
}
