using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Intrinsics.X86;
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


    public PluginAttribute(string name, string version, string description)
    {
        Name = name;
        Version = version;
        Description = description;
    }

}
