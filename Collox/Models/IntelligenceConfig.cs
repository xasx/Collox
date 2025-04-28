using System.Collections.ObjectModel;
using Nucs.JsonSettings.Examples;
using Nucs.JsonSettings.Modulation;

namespace Collox.Models;

[GenerateAutoSaveOnChange]
public partial class IntelligenceConfig : NotifiyingJsonSettings, IVersionable
{
    private string fileName { get; set; } = Constants.IntelligenceConfigPath;

    [EnforcedVersion("1.0.0.0")] public Version Version { get; set; } = new(1, 0, 0, 0);

    private List<IntelligentProcessor> processors { get; set; } = [];

}
