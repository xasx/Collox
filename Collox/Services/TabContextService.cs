using Newtonsoft.Json;

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
        var tabToRemove = _tabs.FirstOrDefault(x => x.Name == tabContext);
        if (tabToRemove != null)
        {
            _tabs.Remove(tabToRemove);
        }
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

        try
        {
            var jsonString = File.ReadAllText(_tabsFilePath);
            var loadedTabs = JsonConvert.DeserializeObject<List<TabContext>>(jsonString);

            if (loadedTabs != null)
            {
                _tabs.AddRange(loadedTabs);
            }
        }
        catch (Exception)
        {
            // Handle JSON parsing and file I/O errors gracefully
        }
    }

    private void SaveTabs()
    {
        var jsonString = JsonConvert.SerializeObject(_tabs, Formatting.Indented);
        File.WriteAllText(_tabsFilePath, jsonString);
    }
}
