using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;
using Windows.Services.Maps.LocalSearch;
using Windows.System.Implementation.FileExplorer;

namespace Collox.Services;

public class TemplateService : ITemplateService
{
    private string templatesDir = Path.Combine(
        AppHelper.Settings.BaseFolder, "Templates");
    public TemplateService()
    {
        
    }
    public async Task DeleteTemplate(string name)
    {
        await Task.Run(() =>
        {
            var filename = DetermineFilename(name);
            if (File.Exists(filename))
            {
                File.Delete(filename);
            }
        });
        
    }

    private string DetermineFilename(string name)
    {
        return Path.Combine(templatesDir, name + ".md");
    }

    public async Task SaveTemplate(string name, string content)
    {
        Directory.CreateDirectory(templatesDir);
        var filename = DetermineFilename(name);
        if (File.Exists(filename))
        {
            throw new Exception($"The template \"{name}\" already exists.");
        }

        await File.AppendAllTextAsync(filename, content);
    }

    public async Task<IEnumerable<Tuple<string, string>>> LoadTemplates()
    {
        // TODO cache respecting adds and deletes or have the view model do it

        var filenames = Directory.GetFiles(templatesDir);
        List<Tuple<string, string>> templates = [];
        foreach (var filename in filenames)
        {
            var fullfn = Path.Combine(templatesDir, filename);
            var name = Path.GetFileNameWithoutExtension(filename);
            var content = await File.ReadAllTextAsync(fullfn);
            var t = Tuple.Create<string, string>(name, content);
            templates.Add(t);
        }
        return (IEnumerable<Tuple<string,string>>)templates;
    }
}
