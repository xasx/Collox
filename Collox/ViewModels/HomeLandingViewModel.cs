using System.Collections.ObjectModel;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Collox.ViewModels;

public partial class HomeLandingViewModel : ObservableObject
{
    [ObservableProperty]
    public partial string RuntimeVersion { get; set; }

    [ObservableProperty]
    public partial string FrameworkDescription { get; set; }

    [ObservableProperty]
    public partial string OSDescription { get; set; }

    [ObservableProperty]
    public partial string OSArchitecture { get; set; }

    [ObservableProperty]
    public partial string ProcessArchitecture { get; set; }

    [ObservableProperty]
    public partial string TargetFramework { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<DependencyInfo> Dependencies { get; set; } = [];

    [ObservableProperty]
    public partial string AppVersion { get; set; }

    [ObservableProperty]
    public partial string AppName { get; set; }

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    public HomeLandingViewModel()
    {
        LoadRuntimeInformation();
    }

    private void LoadRuntimeInformation()
    {
        try
        {
            IsLoading = true;

            // Application information
            AppName = ProcessInfoHelper.ProductName;
            AppVersion = ProcessInfoHelper.VersionWithPrefix;

            // Runtime information
            RuntimeVersion = Environment.Version.ToString();
            FrameworkDescription = RuntimeInformation.FrameworkDescription;

            // OS information
            OSDescription = RuntimeInformation.OSDescription;
            OSArchitecture = RuntimeInformation.OSArchitecture.ToString();
            ProcessArchitecture = RuntimeInformation.ProcessArchitecture.ToString();

            // Target Framework
            var targetFrameworkAttribute = Assembly.GetExecutingAssembly()
                .GetCustomAttribute<TargetFrameworkAttribute>();
            TargetFramework = targetFrameworkAttribute?.FrameworkName ?? "Unknown";

            // Load dependencies
            LoadDependencies();
        }
        catch (Exception ex)
        {
            // Log error if needed
            System.Diagnostics.Debug.WriteLine($"Error loading runtime information: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void LoadDependencies()
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            var referencedAssemblies = assembly.GetReferencedAssemblies();

            Dependencies.Clear();

            foreach (var assemblyName in referencedAssemblies.OrderBy(a => a.Name))
            {
                Dependencies.Add(new DependencyInfo
                {
                    Name = assemblyName.Name,
                    Version = assemblyName.Version?.ToString() ?? "N/A",
                    Culture = string.IsNullOrEmpty(assemblyName.CultureName) ? "neutral" : assemblyName.CultureName,
                    PublicKeyToken = GetPublicKeyToken(assemblyName)
                });
            }

            // Add loaded assemblies information
            var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
                .OrderBy(a => a.GetName().Name);

            foreach (var loadedAssembly in loadedAssemblies)
            {
                var name = loadedAssembly.GetName();
                if (!Dependencies.Any(d => d.Name == name.Name))
                {
                    Dependencies.Add(new DependencyInfo
                    {
                        Name = name.Name,
                        Version = name.Version?.ToString() ?? "N/A",
                        Culture = string.IsNullOrEmpty(name.CultureName) ? "neutral" : name.CultureName,
                        PublicKeyToken = GetPublicKeyToken(name),
                        Location = loadedAssembly.Location
                    });
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading dependencies: {ex.Message}");
        }
    }

    private static string GetPublicKeyToken(AssemblyName assemblyName)
    {
        var publicKeyToken = assemblyName.GetPublicKeyToken();
        if (publicKeyToken == null || publicKeyToken.Length == 0)
            return "null";

        return string.Join("", publicKeyToken.Select(b => b.ToString("x2")));
    }

    [RelayCommand]
    private void CopyToClipboard()
    {
        var info = $"""
            Application Information:
            Name: {AppName}
            Version: {AppVersion}
            
            Runtime Information:
            .NET Version: {RuntimeVersion}
            Framework: {FrameworkDescription}
            Target Framework: {TargetFramework}
            
            System Information:
            OS: {OSDescription}
            OS Architecture: {OSArchitecture}
            Process Architecture: {ProcessArchitecture}
            
            Dependencies ({Dependencies.Count}):
            {string.Join(Environment.NewLine, Dependencies.Select(d => $"  - {d.Name} {d.Version}"))}
            """;

        var dataPackage = new Windows.ApplicationModel.DataTransfer.DataPackage();
        dataPackage.SetText(info);
        Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dataPackage);
    }

    [RelayCommand]
    private void Refresh()
    {
        LoadRuntimeInformation();
    }
}

public partial class DependencyInfo : ObservableObject
{
    [ObservableProperty]
    public partial string Name { get; set; }

    [ObservableProperty]
    public partial string Version { get; set; }

    [ObservableProperty]
    public partial string Culture { get; set; }

    [ObservableProperty]
    public partial string PublicKeyToken { get; set; }

    [ObservableProperty]
    public partial string Location { get; set; }
}
