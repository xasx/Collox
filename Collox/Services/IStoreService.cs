namespace Collox.Services;
public interface IStoreService
{
    Task AppendParagraph(string text, DateTime? timestamp);
    Task SaveNow();
}
