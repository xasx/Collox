using Collox.Models;

namespace Collox.Services;

public interface IStoreService
{
    Task Append(SingleMessage message);

    Task SaveNow();

    Task<IDictionary<string, ICollection<MarkdownRecording>>> Load();

    string GetFilename();
}
