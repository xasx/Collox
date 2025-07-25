﻿using System.Globalization;
using System.Collections.Concurrent;
using Collox.Models;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using NLog;

namespace Collox.Services;

internal class StoreService : IStoreService
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private readonly ConcurrentQueue<string> q = new();
    private string currentFilename;

    private DateTime lastROD = DateTime.MinValue;

    private DateTime lastSave = DateTime.MinValue;

    private bool isNewDay;

    public StoreService()
    {
        currentFilename = GenerateCurrentFilename(DateTime.Now);
    }

    public async Task Append(SingleMessage singleMessage)
    {
        await Task.Run(() =>
        {
            q.EnqueueIf(Settings.WriteDelimiters, $"<!-- collox.bop:{Guid.NewGuid()} -->");
            q.Enqueue(singleMessage.Timestamp?.ToMdTimestamp());
            q.EnqueueIf(singleMessage.Context != "Default", $"_{singleMessage.Context}_");
            q.Enqueue(Environment.NewLine);
            q.Enqueue(singleMessage.Text.AsMdBq());
            q.EnqueueIf(Settings.WriteDelimiters, "<!-- collox.eop -->");
            if (!Settings.DeferredWrite || DateTime.Now - lastSave >= TimeSpan.FromSeconds(30))
            {
                Save();
            }
        }).ConfigureAwait(false);
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
            if (!di.Exists)
            {
                throw new DirectoryNotFoundException($"Base folder '{Settings.BaseFolder}' does not exist.");
            }

            var dict = new Dictionary<string, ICollection<MarkdownRecording>>();
            foreach (var d in di.EnumerateDirectories("????-??_*"))
            {
                var files = d.EnumerateFiles("*.md");
                var list = new List<MarkdownRecording>();
                foreach (var f in files)
                {
                    var lines = string.Empty;
                    try
                    {
                        using (var sr = f.OpenText())
                        {
                            for (var i = 0; i < 5; i++)
                            {
                                var line = await sr.ReadLineAsync().ConfigureAwait(false);
                                if (line != null)
                                {
                                    lines += line + Environment.NewLine;
                                }
                            }
                        }

                        var date = DateOnly.Parse(f.Name[..10]);

                        var rec = new MarkdownRecording
                        {
                            Date = date,
                            Preview = lines,
                            Content = () => File.ReadAllText(f.FullName)
                        };

                        list.Add(rec);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex, "Error processing file '{FileName}'", f.FullName);
                    }
                }

                try
                {
                    var dtm = DateTime.ParseExact(d.Name, "yyyy-MM_MMMM", CultureInfo.CurrentCulture);
                    dict.Add(dtm.ToString("MMMM yyyy"), list);
                }
                catch (FormatException ex)
                {
                    Logger.Error(ex, "Error parsing directory name '{DirectoryName}'", d.Name);
                }
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
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(fn)!);
            while (q.TryDequeue(out var line))
            {
                File.AppendAllText(fn, line + Environment.NewLine);
            }
            lastSave = DateTime.Now;
            Logger.Debug("File saved successfully: {FileName}", fn);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error saving to file '{FileName}'", fn);
        }
    }

    private string CheckFilename()
    {
        if (Settings.CustomRotation)
        {
            var now = DateTime.Now;
            if (isNewDay || DateOnly.FromDateTime(now) > DateOnly.FromDateTime(lastROD))
            {
                isNewDay = true; // save some comparisons
                if (TimeOnly.FromDateTime(now) >= Settings.RollOverTime)
                {
                    var oldfn = currentFilename;
                    currentFilename = GenerateCurrentFilename(now);
                    lastROD = now;
                    WeakReferenceMessenger.Default.Send(
                        new PropertyChangedMessage<string>(this, "Filename", oldfn, currentFilename));
                    isNewDay = false;
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

    public static void EnqueueIf<T>(this ConcurrentQueue<T> queue, bool condition, T element)
    {
        if (condition)
        {
            queue.Enqueue(element);
        }
    }
}
