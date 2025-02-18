using Nucs.JsonSettings.Examples;
using Nucs.JsonSettings.Modulation;

namespace Collox.Common;

[GenerateAutoSaveOnChange]
public partial class AppConfig : NotifiyingJsonSettings, IVersionable
{
    public string fileName { get; set; } = Constants.AppConfigPath;

    public string lastUpdateCheck { get; set; }

    public string baseFolder { get; set; }
        = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Collox");

    public string voice { get; set; }

    public bool autoRead { get; set; }

    public bool autoBeep { get; set; }

    public bool customRotation { get; set; }

    public TimeOnly rollOverTime { get; set; }

    public bool writeDelimiters { get; set; } = true;

    public bool deferredWrite { get; set; } = true;

    public bool enableAI { get; set; } = true;

    [EnforcedVersion("1.0.0.0")] public Version Version { get; set; } = new(1, 0, 0, 0);

    // Docs: https://github.com/Nucs/JsonSettings
}
