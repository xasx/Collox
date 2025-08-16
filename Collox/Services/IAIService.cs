using Collox.Models;

namespace Collox.Services;
public interface IAIService
{
    IEnumerable<IntelligenceApiProvider> GetAllApiProviders();
    void Add(IntelligentProcessor intelligentProcessor);
    void Add(IntelligenceApiProvider intelligenceApiProvider);
    IEnumerable<IntelligentProcessor> Get(Func<IntelligentProcessor, bool> filter);
    IEnumerable<IntelligentProcessor> GetAll();
    void Init();
    void Load();
    void Remove(IntelligentProcessor intelligentProcessor);
    void Remove(IntelligenceApiProvider intelligenceApiProvider);
    void Save();
}
