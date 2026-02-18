﻿using ModelContextProtocol.Client;
using Serilog;

namespace Collox.Services;

internal class McpService : IMcpService, IDisposable
{
    private readonly McpClient mcpClient;
    private static readonly ILogger Logger = Log.ForContext<McpService>();
    private int _disposed;

    private McpService(McpClient client)
    {
        mcpClient = client;
        Logger.Information("McpService initialized successfully");
    }

    public static async Task<IMcpService> CreateAsync()
    {
        Logger.Debug("Initializing McpService");
        try
        {
            var client = await McpClient.CreateAsync(new ClientQueueTransport()).ConfigureAwait(false);
            return new McpService(client);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to initialize McpService");
            throw;
        }
    }

    public async ValueTask<IList<McpClientTool>> GetTools(CancellationToken cancellationToken = default)
    {
        Logger.Debug("Retrieving MCP tools");
        try
        {
            var tools = await mcpClient.ListToolsAsync(cancellationToken: cancellationToken);
            Logger.Information("Successfully retrieved {ToolCount} MCP tools", tools.Count);
            return tools;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to retrieve MCP tools");
            throw;
        }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return;

        Logger.Debug("Disposing McpService");

        if (mcpClient is IDisposable disposable)
        {
            disposable.Dispose();
        }

        Logger.Information("McpService disposed successfully");
    }
}
