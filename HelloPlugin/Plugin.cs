using System;
using System.Threading;
using System.Threading.Tasks;
using Collox.Api;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

[assembly: Plugin("HelloPlugin", "1.0.0", "A simple Hello World plugin.")]

namespace HelloPlugin;

public partial class Plugin : IPlugin
{
    public async Task InitializeAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        // Initialization logic here
    }

    public async Task ShutdownAsync(CancellationToken cancellationToken = default)
    {
        // Shutdown logic here
    }
}
