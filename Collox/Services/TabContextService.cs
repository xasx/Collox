using Windows.Data.Json;

namespace Collox.Services;

public class TabContextService : ITabContextService
{
    private readonly List<TabContext> _tabs = [];

    private readonly string _tabsFilePath = Path.Combine(Constants.RootDirectoryPath, "tabs.json");

    public IList<TabContext> GetTabs()
    {
        if (_tabs.Count == 0)
        {
            LoadTabs();
        }

        return _tabs;
    }

    public void NotifyTabUpdate(TabContext context)
    {
        if (!_tabs.Contains(context))
        {
            // for the default tab, we need to add it to the list
            _tabs.Add(context);
        }
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
        {
            return;
        }

        _tabs.Clear();

        // Load tabs from disk
        var jsonString = File.ReadAllText(_tabsFilePath);
        // Parse the string to a JsonArray
        var tabs = JsonArray.Parse(jsonString);
        // Deserialize the JsonArray to a list of TabContext
        foreach (var tab in tabs)
        {
            if (tab.GetObject() == null)
            {
                continue;
            }
            if (!tab.GetObject().ContainsKey("Name") ||
                !tab.GetObject().ContainsKey("IsCloseable"))
            {
                continue;
            }
            var activeProcessors = tab.GetObject().ContainsKey("ActiveProcessors")
                ? tab.GetObject()["ActiveProcessors"].GetArray().Select(x => Guid.Parse(x.GetString())).ToList()
                : [];

            _tabs.Add(new TabContext
            {
                Name = tab.GetObject()["Name"].GetString(),
                IsCloseable = tab.GetObject()["IsCloseable"].GetBoolean(),
                ActiveProcessors = activeProcessors,
            });
        }
    }

    private void SaveTabs()
    {
        var tabs = new JsonArray();
        foreach (var tab in _tabs)
        {
            var activeProcessorsArray = new JsonArray();
            foreach (var processor in tab.ActiveProcessors)
            {
                activeProcessorsArray.Add(JsonValue.CreateStringValue(processor.ToString()));
            }
            var tabObject = new JsonObject
            {
                ["Name"] = JsonValue.CreateStringValue(tab.Name),
                ["IsCloseable"] = JsonValue.CreateBooleanValue(tab.IsCloseable),
                ["ActiveProcessors"] = activeProcessorsArray
            };
            tabs.Add(tabObject);
        }
        // Serialize the JsonArray to a string
        var jsonString = tabs.Stringify();

        // Save the string to a file (example path)
        File.WriteAllText(_tabsFilePath, jsonString);
    }
}
