using System.Speech.Recognition;
using Nucs.JsonSettings.Examples;
using Nucs.JsonSettings.Modulation;

namespace Collox.Common;

[GenerateAutoSaveOnChange]
public partial class AppConfig : NotifiyingJsonSettings, IVersionable
{
    private string fileName { get; set; } = Constants.AppConfigPath;

    private string lastUpdateCheck { get; set; }

    private string baseFolder { get; set; }
        = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Collox");

    private string voice { get; set; }

    private bool autoRead { get; set; }

    private bool autoBeep { get; set; }

    private bool customRotation { get; set; }

    private TimeOnly rollOverTime { get; set; }

    private bool writeDelimiters { get; set; } = true;

    private bool deferredWrite { get; set; } = true;

    private bool enableAI { get; set; } = true;

    private bool persistMessages { get; set; } = true;

    private bool ollamaEnabled { get; set; }

    private string ollamaEndpoint { get; set; }

    private string ollamaApiKey { get; set; }

    private string ollamaModelId { get; set; }

    private bool openAIEnabled { get; set; }

    private string openAIEndpoint { get; set; }

    private string openAIApiKey { get; set; }

    private string openAIModelId { get; set; }



    [EnforcedVersion("1.0.0.0")] public Version Version { get; set; } = new(1, 0, 0, 0);

    // Docs: https://github.com/Nucs/JsonSettings
}
