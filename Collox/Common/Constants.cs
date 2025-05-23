﻿namespace Collox.Common;

public static class Constants
{
    public static readonly string RootDirectoryPath =
        Path.Combine(PathHelper.GetAppDataFolderPath(true), ProcessInfoHelper.ProductNameAndVersion);

    public static readonly string AppConfigPath = Path.Combine(RootDirectoryPath, "AppConfig.json");
    public static readonly string IntelligenceConfigPath = Path.Combine(RootDirectoryPath, "Intelligence.json");
}
