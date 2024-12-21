using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Collox.Services;

public interface ITemplateService
{
    Task SaveTemplate(string name, string content);

    Task DeleteTemplate(string Name);

    Task<IEnumerable<Tuple<string, string>>> LoadTemplates();
}
