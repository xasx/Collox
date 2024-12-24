using Collox.Models;

namespace Collox.Services;

public interface ITemplateService
{
    Task SaveTemplate(string name, string content);

    Task DeleteTemplate(string Name);

    Task<IDictionary<string, MarkdownTemplate>> LoadTemplates();

    Task EditTemplate(string originalName, string newName, string newContent);
}
