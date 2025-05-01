namespace Collox.Services;

public interface ITabContextService
{
    void SaveNewTab(TabContext tabContext);

    void RemoveTab(TabContext tabContext);

    void RemoveTab(string tabContext);

    IList<TabContext> GetTabs();

    void NotifyTabUpdate(TabContext tabContext);
}

public class TabContext
{
    public string Name { get; set; }

    public bool IsCloseable { get; set; }

    public List<Guid> ActiveProcessors { get; set; } = new List<Guid>();
}
