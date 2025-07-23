using Markdig;
using NLog;

namespace Collox.Services;

public class CommandService : ICommandService
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private static readonly Lazy<MarkdownPipeline> _markdownPipeline = new(
        () => new MarkdownPipelineBuilder().UseAdvancedExtensions().Build());

    public async Task<CommandResult> ProcessCommandAsync(string command, CommandContext context)
    {
        Logger.Info("Processing command: {Command}", command);

        var tokens = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        try
        {
            return tokens switch
            {
                ["clear", ..] => await HandleClearCommand(context),
                ["save", ..] => await HandleSaveCommand(context),
                ["speak", ..] => await HandleSpeakCommand(context),
                ["time", ..] => HandleTimeCommand(context),
                ["pin", ..] => HandlePinCommand(context),
                ["unpin", ..] => HandleUnpinCommand(context),
                ["help", ..] => HandleHelpCommand(),
                ["task", .. var taskName] => HandleTaskCommand(taskName, context),
                _ => new CommandResult { Success = false, ErrorMessage = $"Unknown command: {command}" }
            };
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error processing command: {Command}", command);
            return new CommandResult { Success = false, ErrorMessage = ex.Message };
        }
    }

    private async Task<CommandResult> HandleClearCommand(CommandContext context)
    {
        Logger.Debug("Executing clear command");
        context.Messages.Clear();
        await context.StoreService.SaveNow().ConfigureAwait(false);
        return new CommandResult { Success = true };
    }

    private async Task<CommandResult> HandleSaveCommand(CommandContext context)
    {
        Logger.Debug("Executing save command");
        await context.StoreService.SaveNow().ConfigureAwait(false);
        return new CommandResult { Success = true };
    }

    private async Task<CommandResult> HandleSpeakCommand(CommandContext context)
    {
        Logger.Debug("Executing speak command");
        
        if (context.Messages.Count > 0)
        {
            var lastTextMessage = context.Messages.OfType<TextColloxMessage>().LastOrDefault();
            if (lastTextMessage != null)
            {
                var textToSpeak = StripMd(lastTextMessage.Text);
                await context.AudioService.ReadTextAsync(textToSpeak).ConfigureAwait(false);
                Logger.Debug("Speaking last message with length: {Length}", textToSpeak.Length);
            }
        }
        
        return new CommandResult { Success = true };
    }

    private CommandResult HandleTimeCommand(CommandContext context)
    {
        Logger.Debug("Executing time command");
        var timestampMessage = new TimeColloxMessage { Time = DateTime.Now.TimeOfDay };
        return new CommandResult { Success = true, ResultMessage = timestampMessage };
    }

    private CommandResult HandlePinCommand(CommandContext context)
    {
        Logger.Debug("Executing pin command");
        context.ConversationContext.IsCloseable = false;
        return new CommandResult { Success = true };
    }

    private CommandResult HandleUnpinCommand(CommandContext context)
    {
        Logger.Debug("Executing unpin command");
        context.ConversationContext.IsCloseable = true;
        return new CommandResult { Success = true };
    }

    private CommandResult HandleHelpCommand()
    {
        Logger.Debug("Executing help command");
        var helpMessage = new InternalColloxMessage
        {
            Message = "Available commands: clear, save, speak, time, pin, unpin, task",
            Severity = InfoBarSeverity.Informational
        };
        return new CommandResult { Success = true, ResultMessage = helpMessage };
    }

    private CommandResult HandleTaskCommand(string[] taskNameTokens, CommandContext context)
    {
        var taskName = string.Join(" ", taskNameTokens);
        Logger.Debug("Executing task command: {TaskName}", taskName);
        context.Tasks.Add(new TaskViewModel { Name = taskName, IsDone = false });
        return new CommandResult { Success = true };
    }

    private static string StripMd(string mdText)
    {
        try
        {
            return Markdown.ToPlainText(mdText, _markdownPipeline.Value);
        }
        catch (Exception ex)
        {
            Logger.Warn(ex, "Failed to strip markdown from text: {Text}", mdText);
            return mdText;
        }
    }
}
