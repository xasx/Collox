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

    [EnforcedVersion("1.0.0.0")] public Version Version { get; set; } = new(1, 0, 0, 0);

    // Docs: https://github.com/Nucs/JsonSettings
}
