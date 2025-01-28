

using Collox.Models;

namespace Collox.Services;
public interface IStoreService
{
    Task AppendParagraph(string text, DateTime? timestamp);
    Task SaveNow();

    Task<IDictionary<string, ICollection<MarkdownRecording>>> Load();

    string GetFilename();
}
