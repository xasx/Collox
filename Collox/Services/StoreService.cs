using System.Globalization;
using Collox.Models;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Collox.Services;

internal class StoreService : IStoreService
{
    private readonly Queue<string> q = new();
    private string currentFilename;

    private DateTime lastROD = DateTime.MinValue;

    private DateTime lastSave = DateTime.MinValue;

    private bool newday;


    public StoreService()
    {
        currentFilename = GenerateCurrentFilename(DateTime.Now);
    }

    public Task AppendParagraph(string text, string context, DateTime? timestamp)
    {
        return Task.Run(() =>
        {
            q.EnqueueIf(Settings.WriteDelimiters, $"<!-- collox.bop:{Guid.NewGuid()} -->");
            q.Enqueue(timestamp?.ToMdTimestamp());
            q.EnqueueIf(context != "Default", $"_{context}_");
            q.Enqueue(Environment.NewLine);
            q.Enqueue(text.AsMdBq());
            q.EnqueueIf(Settings.WriteDelimiters, "<!-- collox.eop -->");
            if (!Settings.DeferredWrite || DateTime.Now - lastSave >= TimeSpan.FromSeconds(30))
            {
                Save();
            }
        });
    }

    public string GetFilename()
    {
        return currentFilename;
    }

    public Task SaveNow()
    {
        return Task.Run(Save);
    }

    public Task<IDictionary<string, ICollection<MarkdownRecording>>> Load()
    {
        return Task.Run<IDictionary<string, ICollection<MarkdownRecording>>>(async () =>
        {
            var di = new DirectoryInfo(Settings.BaseFolder);
            var dict = new Dictionary<string, ICollection<MarkdownRecording>>();
            foreach (var d in di.EnumerateDirectories(@"????-??_*"))
            {
                //if (d.Name.Equals("Templates")) continue;
                var files = d.EnumerateFiles("*.md");
                var list = new List<MarkdownRecording>();
                foreach (var f in files)
                {
                    string lines;
                    using (var sr = f.OpenText())
                    {
                        lines = await sr.ReadToEndAsync();
                    }

                    var date = DateOnly.Parse(f.Name.Substring(0, 10));

                    var rec = new MarkdownRecording
                    {
                        Date = date,
                        Preview = lines
                    };

                    list.Add(rec);
                }

                var dtm = DateTime.ParseExact(d.Name, "yyyy-MM_MMMM", CultureInfo.CurrentCulture);
                dict.Add(dtm.ToString("MMMM yyyy"), list);
            }

            return dict;
        });
    }

    private string GenerateCurrentFilename(DateTime now)
    {
        var cfn = $"{now:yyyy-MM-dd}.md";
        var fn = $"{now:yyyy-MM_MMMM}";
        return Path.Combine(Settings.BaseFolder, fn, cfn);
    }

    private void Save()
    {
        var fn = CheckFilename();
        Directory.CreateDirectory(Path.GetDirectoryName(fn)!);
        File.AppendAllLines(fn, q);
        lastSave = DateTime.Now;
        q.Clear();
    }

    private string CheckFilename()
    {
        if (Settings.CustomRotation)
        {
            var now = DateTime.Now;
            if (newday || DateOnly.FromDateTime(now) > DateOnly.FromDateTime(lastROD))
            {
                newday = true; // save some comparisons
                if (TimeOnly.FromDateTime(now) >= Settings.RollOverTime)
                {
                    var oldfn = currentFilename;
                    currentFilename = GenerateCurrentFilename(now);
                    lastROD = now;
                    WeakReferenceMessenger.Default.Send(
                        new PropertyChangedMessage<string>(this, "Filename", oldfn, currentFilename));
                    newday = false;
                }
            }
        }

        return currentFilename;
    }
}

public static class Extensions
{
    public static string AsMdBq(this string text)
    {
        using var writer = new StringWriter();
        using var reader = new StringReader(text);
        while (reader.ReadLine() is { } line)
        {
            writer.Write("> ");
            writer.WriteLine(line);
        }

        return writer.ToString();
    }

    public static string ToMdTimestamp(this DateTime dateTime)
    {
        return $"**{dateTime:G}**";
    }

    public static void EnqueueIf<T>(this Queue<T> queue, bool condition, T element)
    {
        if (condition)
        {
            queue.Enqueue(element);
        }
    }
}
