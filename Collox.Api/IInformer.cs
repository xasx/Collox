using System.Threading.Tasks;

namespace Collox.Api;

public interface IInformer
{
    string Name { get; }
    string Description { get; }

    Task<string> InformAsync(string input);
}
