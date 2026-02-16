using System.Diagnostics;
using Collox.Models;

namespace Collox.Services;

public class TemplateService : ITemplateService
{
    private readonly string templatesDir = Path.Combine(
        Settings.BaseFolder, "Templates");

    private Dictionary<string, MarkdownTemplate> cache = new();

    public async Task DeleteTemplate(string name)
    {
        await Task.Run(() =>
        {
            var filename = DetermineFilename(name);
            if (File.Exists(filename))
            {
                File.Delete(filename);
            }
        }).ConfigureAwait(false);
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

        await File.WriteAllTextAsync(filename!, content).ConfigureAwait(false);
        cache.Add(name, new MarkdownTemplate
        {
            Name = name, FileName = filename, Content = content
        });
    }

    public async Task<IDictionary<string, MarkdownTemplate>> LoadTemplates()
    {
        Directory.CreateDirectory(templatesDir);
        var filenames = Directory.GetFiles(templatesDir);
        Dictionary<string, MarkdownTemplate> templates = [];
        foreach (var filename in filenames)
        {
            var name = Path.GetFileNameWithoutExtension(filename);
            var content = await File.ReadAllTextAsync(filename).ConfigureAwait(false);
            var t = new MarkdownTemplate { Name = name, Content = content, FileName = filename };
            templates.Add(name, t);
        }

        cache = templates;
        return templates;
    }

    public async Task EditTemplate(string originalName, string newName, string newContent)
    {
        var templateEntry = cache[originalName];
        var fn = templateEntry.FileName;

        if (originalName == newName)
        {
            Debug.Assert(newName == Path.GetFileNameWithoutExtension(fn));

            await File.WriteAllTextAsync(fn, newContent).ConfigureAwait(false);
            cache[newName].Content = newContent;
        }
        else
        {
            var newFn = DetermineFilename(newName);
            File.Move(fn, newFn);

            await File.WriteAllTextAsync(newFn, newContent).ConfigureAwait(false);

            cache.Remove(originalName);

            templateEntry.Name = newName;
            templateEntry.FileName = newFn;
            templateEntry.Content = newContent;

            cache.Add(newName, templateEntry);
        }
    }

    private string DetermineFilename(string name) => Path.Combine(templatesDir, $"{name}.md");
}
