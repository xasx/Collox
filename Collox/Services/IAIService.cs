using Collox.Models;
using Microsoft.Extensions.AI;

namespace Collox.Services;
public interface IAIService
{
    void Add(IntelligentProcessor intelligentProcessor);
    IEnumerable<IntelligentProcessor> Get(Func<IntelligentProcessor, bool> filter);
    IEnumerable<IntelligentProcessor> GetAll();
    IChatClient GetChatClient(AIProvider apiType, string modelId);
    void Init();
    void Load();
    void Remove(IntelligentProcessor intelligentProcessor);
    void Save();
}
