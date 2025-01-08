using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Collox.Models;

namespace Collox.Services;

internal class StoreService : IStoreService
{
    private string currentFilename;

    private DateTime lastSave = DateTime.MinValue;

    private Queue<string> q = new Queue<string>();

    public StoreService()
    {
        DateTime now = DateTime.Now;
        string cfn = $"{now.ToString("yyyy-MM-dd")}.md";
        string fn = $"{now.ToString("yyyy-MM_MMMM")}";
        currentFilename = Path.Combine(AppHelper.Settings.BaseFolder, fn, cfn);
    }

    public Task AppendParagraph(string text, DateTime? timestamp)
    {
        return Task.Run(() =>
        {
            q.Enqueue($"<!-- collox.bop:{Guid.NewGuid()} -->");
            q.Enqueue(timestamp?.ToMdTimestamp());
            q.Enqueue(text.AsMdBq());

            if (DateTime.Now - lastSave >= TimeSpan.FromSeconds(30))
            {
                Save();
            }
        });
    }

    private void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(currentFilename));
        File.AppendAllLines(currentFilename, q);
        lastSave = DateTime.Now;
        q.Clear();
    }

    public Task SaveNow()
    {
        return Task.Run(() =>
        {
            Save();
        });
    }

    public Task<IDictionary<string, ICollection<MarkdownRecording>>> Load()
    {
        return Task.Run<IDictionary<string, ICollection<MarkdownRecording>>>(async () =>
        {
            var di = new DirectoryInfo(AppHelper.Settings.BaseFolder);
            var dict = new Dictionary<string, ICollection<MarkdownRecording>>();
            foreach (var d in di.EnumerateDirectories(@"????-??_*"))
            {
                //if (d.Name.Equals("Templates")) continue;
                var files = d.EnumerateFiles("*.md");
                var list = new List<MarkdownRecording>();
                foreach (var f in files)
                {
                    var lines = string.Empty;
                    using (var sr = f.OpenText())
                    {
                        lines = await sr.ReadToEndAsync();
                    }
                    var date = DateOnly.Parse(f.Name.Substring(0, 10));
                    var rec = new MarkdownRecording()
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
}

public static class Extensions
{
    public static string AsMdBq(this string text)
    {
        using (StringWriter writer = new StringWriter())
        using (StringReader reader = new StringReader(text))
        {
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                writer.Write("> ");
                writer.WriteLine(line);

            }
            writer.WriteLine("<!-- collox.eop -->");
            return writer.ToString();
        }
    }

    public static string ToMdTimestamp(this DateTime dateTime)
    {
        return string.Concat("##### ", dateTime.ToString("G"), Environment.NewLine);
    }
}
