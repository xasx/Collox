using Collox.Models;

namespace Collox.Services;
public interface IAIService
{
    IEnumerable<IntelligenceApiProvider> GetAllApiProviders();
    void Add(IntelligentProcessor intelligentProcessor);
    void Add(IntelligenceApiProvider intelligenceApiProvider);
    IEnumerable<IntelligentProcessor> GetAllProcessors();
    void Load();
    void Remove(IntelligentProcessor intelligentProcessor);
    void Remove(IntelligenceApiProvider intelligenceApiProvider);
    void Save();
}
