using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            q.Enqueue( timestamp?.ToMdTimestamp());
            q.Enqueue(text.AsMdBq());

            if (DateTime.Now - lastSave >= TimeSpan.FromSeconds(30))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(currentFilename));
                File.AppendAllLines(currentFilename, q);
                lastSave = DateTime.Now;
                q.Clear();
            }
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
            return writer.ToString();
        }
    }

    public static string ToMdTimestamp(this DateTime dateTime)
    {
        return string.Concat("##### ",  dateTime.ToString("G"), Environment.NewLine);
    }
}
