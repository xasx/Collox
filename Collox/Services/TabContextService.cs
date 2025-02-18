using Windows.Data.Json;

namespace Collox.Services;

public class TabContextService : ITabContextService
{
    private readonly List<TabContext> _tabs = [];

    private readonly string _tabsFilePath = Path.Combine(Constants.RootDirectoryPath, "tabs.json");

    public IList<TabContext> GetTabs()
    {
        if (_tabs.Count == 0)
            LoadTabs();

        return _tabs;
    }

    public void NotifyTabUpdate(TabContext _)
    {
        SaveTabs();
    }

    public void RemoveTab(TabContext tabContext)
    {
        _tabs.Remove(tabContext);
        SaveTabs();
    }

    public void RemoveTab(string tabContext)
    {
        _tabs.Remove(_tabs.FirstOrDefault(x => x.Name == tabContext));
        SaveTabs();
    }

    public void SaveNewTab(TabContext tabContext)
    {
        _tabs.Add(tabContext);
        SaveTabs();
    }
    private void LoadTabs()
    {
        if (!File.Exists(_tabsFilePath))
            return;

        _tabs.Clear();

        // Load tabs from disk
        string jsonString = File.ReadAllText(_tabsFilePath);
        // Parse the string to a JsonArray
        JsonArray tabs = JsonArray.Parse(jsonString);
        // Deserialize the JsonArray to a list of TabContext
        _tabs.AddRange(tabs.Select(tab => new TabContext
        {
            Name = tab.GetObject()["Name"].GetString(),
            IsCloseable = tab.GetObject()["IsCloseable"].GetBoolean()
        }));
    }

    private void SaveTabs()
    {
        // Save tabs to disk

        JsonArray tabs =
        [
            .. _tabs.Select(tab => new JsonObject
            {
                ["Name"] = JsonValue.CreateStringValue(tab.Name),
                ["IsCloseable"] = JsonValue.CreateBooleanValue(tab.IsCloseable)
            }),
        ];

        // Serialize the JsonArray to a string
        string jsonString = tabs.Stringify();

        // Save the string to a file (example path)
        File.WriteAllText(_tabsFilePath, jsonString);
    }
}
