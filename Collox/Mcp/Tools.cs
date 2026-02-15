using System.ComponentModel;
using ModelContextProtocol.Server;

namespace Collox.Mcp;

[McpServerToolType]
public class Tools
{
    //add serilog logger
    private static readonly Serilog.ILogger Logger = Serilog.Log.ForContext<Tools>();

    [McpServerTool, Description("Create a new task")]
    public static void CreateTask(string name)
    {
        Logger.Information("Adding task: {TaskName}", name);
        // Add your task creation logic here

    }
}
