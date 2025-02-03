using Collox.Models;

namespace Collox.Services;
public interface IStoreService
{
    Task AppendParagraph(string text, string Context, DateTime? timestamp);

    Task SaveNow();

    Task<IDictionary<string, ICollection<MarkdownRecording>>> Load();

    string GetFilename();
}
