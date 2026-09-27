using System.Collections.Concurrent;
using System.Diagnostics;
using System.Xml;

namespace CivicTimeChecker;

internal sealed record SearchResult
{
    public bool Found { get; init; }
    public string? FilePath { get; init; }
    public string? BindFilePath { get; init; }
    public DateTime? DispenseTime { get; init; }
    public string? HousingCode { get; init; }
    public string? MaterialCode { get; init; }
    public string? Error { get; init; }
    public int FilesScanned { get; init; }
    public long ElapsedMs { get; init; }
    public FolderProfile? Profile { get; init; }
}

internal static class RequestIndex
{
    private sealed class Entry
    {
        public long Length;
        public long WriteTicks;
        public bool Ok;
        public string Housing = "";
        public List<string> Items = [];
        public DateTimeOffset Stamp;
    }

    private static readonly ConcurrentDictionary<string, Entry> Cache = new(StringComparer.OrdinalIgnoreCase);
    private static long _lastFlush = Environment.TickCount64;
    private const int HardCap = 25000;

    public static int Count => Cache.Count;

    public static void Flush()
    {
        Cache.Clear();
        Volatile.Write(ref _lastFlush, Environment.TickCount64);
    }

    public static void MaybeFlush(int minutes)
    {
        var span = Math.Max(1, minutes) * 60_000L;
        var now = Environment.TickCount64;
        if (now - Volatile.Read(ref _lastFlush) < span)
            return;
        Flush();
    }

    public static bool TryRead(string path, out string housing, out List<string> itemIds, out DateTimeOffset stamp)
    {
        housing = "";
        itemIds = [];
        stamp = default;
        FileInfo info;
        try { info = new FileInfo(path); }
        catch { return false; }
        if (!info.Exists)
        {
            Cache.TryRemove(path, out _);
            return false;
        }

        if (Cache.TryGetValue(path, out var hit)
            && hit.Length == info.Length
            && hit.WriteTicks == info.LastWriteTimeUtc.Ticks)
        {
            if (!hit.Ok)
                return false;
            housing = hit.Housing;
            itemIds = hit.Items;
            stamp = hit.Stamp;
            return true;
        }

        var ok = RequestXmlSearch.TryReadRequestRaw(path, out housing, out itemIds, out stamp);
        Cache[path] = new Entry
        {
            Length = info.Length,
            WriteTicks = info.LastWriteTimeUtc.Ticks,
            Ok = ok,
            Housing = housing,
            Items = itemIds,
            Stamp = stamp
        };
        if (Cache.Count > HardCap)
            Flush();
        return ok;
    }
}

internal static class RequestXmlSearch
{
    public static SearchResult Find(string scannedCode, AppConfig config)
    {
        RequestIndex.MaybeFlush(config.CacheFlushMinutes);
        var sw = Stopwatch.StartNew();
        scannedCode = (scannedCode ?? "").Trim();
        if (string.IsNullOrWhiteSpace(scannedCode))
            return new SearchResult { Error = "扫码为空", ElapsedMs = sw.ElapsedMilliseconds };

        var dispense = config.GetDispenseProfiles();
        var bind = config.GetBindProfiles();
        if (dispense.Count == 0 && bind.Count == 0)
            return new SearchResult { Error = "未配置报文目录，请点左上角“配置”", ElapsedMs = sw.ElapsedMilliseconds };

        var scanned = 0;
        var missing = new List<string>();

        var housingHit = FindHousing(scannedCode, dispense, missing, ref scanned, sw, pair: null);
        if (housingHit != null)
            return housingHit;

        if (bind.Count == 0)
        {
            return NotFound(
                $"未在点胶站 Request*.xml 中找到 {scannedCode}。若这是 PCB / Heatsink 物料码，请先配置组装站目录。{MissingSuffix(missing)}",
                scanned, sw);
        }

        var bindHit = FindMaterial(scannedCode, bind, missing, ref scanned, sw);
        if (bindHit is null)
        {
            return NotFound(
                $"未找到 {scannedCode}（点胶站主码与组装站物料均未命中）{MissingSuffix(missing)}",
                scanned, sw);
        }

        var pair = bindHit.Profile?.Pair ?? Pairs.Housing25;
        var paired = dispense.Where(x => x.Pair == pair).ToList();
        var dispenseHit = FindHousing(bindHit.HousingCode!, paired, missing, ref scanned, sw, pair);
        if (dispenseHit is null)
        {
            return new SearchResult
            {
                Error = $"物料 {scannedCode} 已绑定主码 {bindHit.HousingCode}，但配对点胶站 {pair} 未找到点胶时间{MissingSuffix(missing)}",
                HousingCode = bindHit.HousingCode,
                MaterialCode = scannedCode,
                BindFilePath = bindHit.BindFilePath,
                FilesScanned = scanned,
                ElapsedMs = sw.ElapsedMilliseconds
            };
        }

        return new SearchResult
        {
            Found = true,
            FilePath = dispenseHit.FilePath,
            BindFilePath = bindHit.BindFilePath,
            DispenseTime = dispenseHit.DispenseTime,
            HousingCode = bindHit.HousingCode,
            MaterialCode = scannedCode,
            Profile = dispenseHit.Profile,
            FilesScanned = scanned,
            ElapsedMs = sw.ElapsedMilliseconds
        };
    }

    private static SearchResult? FindHousing(
        string housingCode,
        IReadOnlyList<FolderProfile> profiles,
        List<string> missing,
        ref int scanned,
        Stopwatch sw,
        string? pair)
    {
        SearchResult? best = null;
        foreach (var profile in profiles)
        {
            if (pair != null && profile.Pair != pair)
                continue;
            if (string.IsNullOrWhiteSpace(profile.MessageFolder) || !Directory.Exists(profile.MessageFolder))
            {
                missing.Add(profile.Name);
                continue;
            }

            var found = ScanFolder(profile, housingCode, matchMaterial: false, out var scannedHere);
            scanned += scannedHere;
            if (found is null)
                continue;
            if (best is null || found.DispenseTime > best.DispenseTime)
                best = found with { ElapsedMs = sw.ElapsedMilliseconds, FilesScanned = scanned };
        }
        return best;
    }

    private static SearchResult? FindMaterial(
        string materialCode,
        IReadOnlyList<FolderProfile> profiles,
        List<string> missing,
        ref int scanned,
        Stopwatch sw)
    {
        SearchResult? best = null;
        foreach (var profile in profiles)
        {
            if (string.IsNullOrWhiteSpace(profile.MessageFolder) || !Directory.Exists(profile.MessageFolder))
            {
                missing.Add(profile.Name);
                continue;
            }

            var found = ScanFolder(profile, materialCode, matchMaterial: true, out var scannedHere);
            scanned += scannedHere;
            if (found is null)
                continue;
            if (best is null || found.DispenseTime > best.DispenseTime)
                best = found with { ElapsedMs = sw.ElapsedMilliseconds, FilesScanned = scanned };
        }
        return best;
    }

    private static SearchResult? ScanFolder(FolderProfile profile, string code, bool matchMaterial, out int scanned)
    {
        scanned = 0;
        IEnumerable<string> files;
        try
        {
            files = Directory.EnumerateFiles(profile.MessageFolder, "Request*.xml", SearchOption.AllDirectories);
        }
        catch
        {
            return null;
        }

        var hits = new ConcurrentBag<(string path, string housing, DateTimeOffset time)>();
        var scannedHere = 0;
        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = Math.Clamp(Environment.ProcessorCount, 2, 8)
        };

        try
        {
            Parallel.ForEach(files, options, path =>
            {
                Interlocked.Increment(ref scannedHere);
                if (!TryReadRequest(path, out var housing, out var items, out var stamp))
                    return;
                if (matchMaterial)
                {
                    if (!items.Any(x => x.Equals(code, StringComparison.OrdinalIgnoreCase)))
                        return;
                    if (string.IsNullOrWhiteSpace(housing))
                        return;
                }
                else if (!housing.Equals(code, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                hits.Add((path, housing, stamp));
            });
        }
        catch (UnauthorizedAccessException)
        {
            scanned = scannedHere;
            return null;
        }
        catch (DirectoryNotFoundException)
        {
            scanned = scannedHere;
            return null;
        }

        scanned = scannedHere;
        if (hits.IsEmpty)
            return null;

        var best = hits.MaxBy(x => x.time);
        return new SearchResult
        {
            Found = !matchMaterial,
            FilePath = matchMaterial ? null : best.path,
            BindFilePath = matchMaterial ? best.path : null,
            DispenseTime = best.time.LocalDateTime,
            HousingCode = best.housing,
            MaterialCode = matchMaterial ? code : null,
            Profile = profile,
            FilesScanned = scannedHere
        };
    }

    internal static bool TryReadRequest(string path, out string housing, out List<string> itemIds, out DateTimeOffset stamp)
        => RequestIndex.TryRead(path, out housing, out itemIds, out stamp);

    internal static bool TryReadRequestRaw(
        string path,
        out string housing,
        out List<string> itemIds,
        out DateTimeOffset stamp)
    {
        housing = "";
        itemIds = [];
        stamp = default;
        try
        {
            var settings = new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                IgnoreComments = true,
                IgnoreWhitespace = true,
                IgnoreProcessingInstructions = true,
                CloseInput = true
            };
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = XmlReader.Create(stream, settings);
            while (reader.Read())
            {
                if (reader.NodeType != XmlNodeType.Element)
                    continue;

                if (reader.Name == "header")
                {
                    var raw = reader.GetAttribute("timeStamp");
                    if (!DateTimeOffset.TryParse(raw, out stamp))
                        return false;
                }
                else if (reader.Name == "partProcessed")
                {
                    housing = reader.GetAttribute("identifier")?.Trim() ?? "";
                }
                else if (reader.Name == "item" && reader.HasAttributes)
                {
                    while (reader.MoveToNextAttribute())
                    {
                        if (IsSchemaAttribute(reader.Name))
                            continue;
                        var value = reader.Value.Trim();
                        if (value.Length > 0)
                            itemIds.Add(value);
                    }
                    reader.MoveToElement();
                }
            }
            return housing.Length > 0 && stamp != default;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsSchemaAttribute(string name) =>
        name is "name" or "dataType" or "typeNo" or "typeVar" or "batch" or "class" or "vendor" or "qty";

    private static SearchResult NotFound(string error, int scanned, Stopwatch sw)
    {
        return new SearchResult
        {
            Error = error,
            FilesScanned = scanned,
            ElapsedMs = sw.ElapsedMilliseconds
        };
    }

    private static string MissingSuffix(List<string> missing)
        => missing.Count == 0 ? "" : $"；不可用目录：{string.Join("、", missing)}";
}
