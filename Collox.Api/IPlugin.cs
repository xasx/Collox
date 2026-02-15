using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace Collox.Api;

[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
public interface IPlugin
{
    Task InitializeAsync(IServiceProvider services, CancellationToken cancellationToken = default);
    Task ShutdownAsync(CancellationToken cancellationToken = default);
}

[AttributeUsage(AttributeTargets.Assembly)]
public class PluginAttribute : Attribute
{
    public string Name { get; }
    public string Version { get; }
    public string Description { get; }
    public string Author { get; }
    public string Id { get; }

    public PluginAttribute(string id, string name, string version, string author, string description)
    {
        Id = id;
        Name = name;
        Version = version;
        Author = author;
        Description = description;
    }

}
