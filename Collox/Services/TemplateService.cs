using System.Diagnostics;
using Collox.Models;

namespace Collox.Services;

public class TemplateService : ITemplateService
{
    private readonly string templatesDir = Path.Combine(
        Settings.BaseFolder, "Templates");

    private IDictionary<string, MarkdownTemplate> cache;

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
        cache.Remove(name);
    }

    public async Task SaveTemplate(string name, string content)
    {
        Directory.CreateDirectory(templatesDir);
        var filename = DetermineFilename(name);
        if (File.Exists(filename))
        {
            throw new Exception($"The template \"{name}\" already exists.");
        }

        await File.WriteAllTextAsync(filename!, content);
        cache.Add(name, new MarkdownTemplate
        {
            Name = name, FileName = filename, Content = content
        });
    }

    public async Task<IDictionary<string, MarkdownTemplate>> LoadTemplates()
    {
        var filenames = Directory.GetFiles(templatesDir);
        IDictionary<string, MarkdownTemplate> templates = new Dictionary<string, MarkdownTemplate>();
        foreach (var filename in filenames)
        {
            var name = Path.GetFileNameWithoutExtension(filename);
            var content = await File.ReadAllTextAsync(filename);
            var t = new MarkdownTemplate { Name = name, Content = content, FileName = filename };
            templates.Add(name, t);
        }

        cache = templates;
        return templates;
    }

    public async Task EditTemplate(string originalName, string newName, string newContent)
    {
        var templ = cache[originalName];
        var fn = templ.FileName;

        if (originalName == newName)
        {
            Debug.Assert(newName == Path.GetFileNameWithoutExtension(fn));

            await File.WriteAllTextAsync(fn, newContent);
            cache[newName].Content = newContent;
        }
        else
        {
            var newFn = DetermineFilename(newName);
            File.Move(fn, newFn);


            await File.WriteAllTextAsync(newFn, newContent);

            cache.Remove(originalName);

            templ.Name = newName;
            templ.FileName = newFn;
            templ.Content = newContent;

            cache.Add(newName, templ);
        }
    }

    private string DetermineFilename(string name)
    {
        return Path.Combine(templatesDir, name + ".md");
    }
}
