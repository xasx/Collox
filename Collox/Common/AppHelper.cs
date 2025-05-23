﻿using Nucs.JsonSettings;
using Nucs.JsonSettings.Fluent;
using Nucs.JsonSettings.Modulation;
using Nucs.JsonSettings.Modulation.Recovery;

namespace Collox.Common;

public static class AppHelper
{
    public static readonly AppConfig Settings = JsonSettings.Configure<AppConfig>()
        .WithRecovery(RecoveryAction.RenameAndLoadDefault)
        .WithVersioning(VersioningResultAction.RenameAndLoadDefault)
        .LoadNow();
}
