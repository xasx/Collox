using Collox.Models;

namespace Collox.Services;

public interface IStoreService
{
    Task Append(SingleMessage message, CancellationToken cancellationToken = default);

    Task SaveNow(CancellationToken cancellationToken = default);

    Task<IDictionary<string, ICollection<MarkdownRecording>>> Load(CancellationToken cancellationToken = default);

    string GetFilename();
}
