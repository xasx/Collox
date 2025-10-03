﻿using System.Collections.ObjectModel;
using ModelContextProtocol.Client;
using Serilog;

namespace Collox.Services;

internal class McpService : IMcpService, IDisposable
{
    private readonly McpClient mcpClient;
    private static readonly ILogger Logger = Log.ForContext<McpService>();
    private bool _disposed;

    public McpService()
    {
        Logger.Debug("Initializing McpService");
        try
        {
            mcpClient = McpClient.CreateAsync(new ClientQueueTransport()).Result;
            Logger.Information("McpService initialized successfully");
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to initialize McpService");
            throw;
        }
    }

    public async ValueTask<IList<McpClientTool>> GetTools()
    {
        Logger.Debug("Retrieving MCP tools");
        try
        {
            var tools = await mcpClient.ListToolsAsync();
            Logger.Information("Successfully retrieved {ToolCount} MCP tools", tools.Count);
            foreach (var tool in tools)
            {
                Logger.Debug("Tool: {ToolName}, Description: {ToolDescription}", tool.Name, tool.Description);
                var res = await tool.CallAsync(arguments: ReadOnlyDictionary<string, object>.Empty);
                Logger.Debug("Tool {ToolName} executed with result: {Result}", tool.Name, res.Content);
            }
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
        if (_disposed)
            return;

        Logger.Debug("Disposing McpService");

        if (mcpClient is IDisposable disposable)
        {
            disposable.Dispose();
        }

        _disposed = true;
        Logger.Information("McpService disposed successfully");
    }
}
