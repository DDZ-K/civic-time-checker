using System.Text;

namespace CivicRequestExtractor;

public static class SelfTest
{
    public static int Run()
    {
        var root = Path.Combine(Path.GetTempPath(), "civic-req-extract-" + Guid.NewGuid().ToString("N"));
        var watchHousing = Path.Combine(root, "watch-housing");
        var watchCover = Path.Combine(root, "watch-cover");
        var save25 = Path.Combine(root, "save-25");
        var save30 = Path.Combine(root, "save-30");
        var save60 = Path.Combine(root, "save-60");
        var save70 = Path.Combine(root, "save-70");
        var save100 = Path.Combine(root, "save-100");
        var save110 = Path.Combine(root, "save-110");
        var today = DateTime.Now.Date;
        string Day(string watch) => Path.Combine(watch, today.ToString("yyyy"), today.ToString("MM"), today.ToString("dd"));
        var st25 = Path.Combine(Day(watchHousing), "0025_1_1_1_1", "partProcessed");
        var st30 = Path.Combine(Day(watchHousing), "0030_1_1_1_1");
        var st60 = Path.Combine(Day(watchHousing), "0060_1_1_1_1", "partProcessed");
        var st70 = Path.Combine(Day(watchHousing), "0070_1_1_1_1");
        var st100 = Path.Combine(Day(watchCover), "0100_1_1_1_1", "partProcessed");
        var st110 = Path.Combine(Day(watchCover), "0110_1_1_1_1");
        var stOther = Path.Combine(Day(watchHousing), "0041_1_1_1_1");
        foreach (var dir in new[] { st25, st30, st60, st70, st100, st110, stOther, save25, save30, save60, save70, save100, save110 })
            Directory.CreateDirectory(dir);

        var sessionOld = Path.Combine(st25, "Request_session_old.xml");
        File.WriteAllText(sessionOld, "<header timeStamp=\"2026-08-01T00:00:00+08:00\"/><partProcessed identifier=\"OLD\"/>");
        var thirtySixHoursAgo = DateTime.Now.AddHours(-36);
        File.SetCreationTime(sessionOld, thirtySixHoursAgo);
        File.SetLastWriteTime(sessionOld, thirtySixHoursAgo);

        File.WriteAllText(Path.Combine(st25, "Response_nope.xml"), "<response/>");
        File.WriteAllText(Path.Combine(stOther, "Request_other.xml"), "<header timeStamp=\"2026-09-23T15:26:41+08:00\"/><partProcessed identifier=\"OTHER\"/>");

        var cfg = new AppConfig
        {
            WatchRoot1 = watchHousing,
            WatchRoot2 = watchCover,
            OutputRoot = Path.Combine(root, "unused-root"),
            DispenseItems =
            [
                new StationTarget { Station = "25", SaveFolder = save25 },
                new StationTarget { Station = "0060", SaveFolder = save60 },
                new StationTarget { Station = "0100", SaveFolder = save100 }
            ],
            BindItems =
            [
                new StationTarget { Station = "0030", SaveFolder = save30 },
                new StationTarget { Station = "70", SaveFolder = save70 },
                new StationTarget { Station = "0110", SaveFolder = save110 }
            ],
            PollIntervalSec = 1,
            LookbackDays = 2,
            KeepDays = 1,
            OnlyNewAfterStart = true,
            NewFileGraceSec = 1,
            MinFileBytes = 8,
            SizeStableChecks = 0,
            SizeStableIntervalMs = 0,
            CacheFlushMinutes = 1,
            HandledSoftCap = 8
        };
        cfg.Normalize();

        var engine = new ExtractEngine(cfg);
        engine.PrepareSession();

        File.WriteAllText(Path.Combine(st25, "Request_new25.xml"), "<header timeStamp=\"2026-09-23T15:26:41+08:00\"/><partProcessed identifier=\"NEW25\"/>");
        File.WriteAllText(Path.Combine(st30, "Request_new30.xml"), "<header timeStamp=\"2026-09-23T15:27:55+08:00\"/><partProcessed identifier=\"NEW30\"/>");
        File.WriteAllText(Path.Combine(st60, "Request_new60.xml"), "<header timeStamp=\"2026-09-23T15:29:39+08:00\"/><partProcessed identifier=\"NEW60\"/>");
        File.WriteAllText(Path.Combine(st70, "Request_new70.xml"), "<header timeStamp=\"2026-09-23T15:31:36+08:00\"/><partProcessed identifier=\"NEW70\"/>");
        File.WriteAllText(Path.Combine(st100, "Request_new100.xml"), "<header timeStamp=\"2026-09-23T15:27:35+08:00\"/><partProcessed identifier=\"NEW100\"/>");
        File.WriteAllText(Path.Combine(st110, "Request_new110.xml"), "<header timeStamp=\"2026-09-23T15:28:22+08:00\"/><partProcessed identifier=\"NEW110\"/>");

        var first = engine.ScanOnce();
        var dest25 = Path.Combine(save25, "Request_new25.xml");
        var dest30 = Path.Combine(save30, "Request_new30.xml");
        var dest60 = Path.Combine(save60, "Request_new60.xml");
        var dest70 = Path.Combine(save70, "Request_new70.xml");
        var dest100 = Path.Combine(save100, "Request_new100.xml");
        var dest110 = Path.Combine(save110, "Request_new110.xml");
        var dest25InBind = Path.Combine(save30, "Request_new25.xml");
        var dest100InHousing = Path.Combine(save25, "Request_new100.xml");
        var destStationFolder = Path.Combine(save25, "0025_1_1_1_1", "Request_new25.xml");
        var destDateFolder = Path.Combine(save25, today.ToString("yyyy"), today.ToString("MM"), today.ToString("dd"), "Request_new25.xml");
        var destSessionOld = Path.Combine(save25, "Request_session_old.xml");
        var destResp = Path.Combine(save25, "Response_nope.xml");
        var destOther = Path.Combine(save25, "Request_other.xml");
        var existed25 = File.Exists(dest25);
        var existed30 = File.Exists(dest30);
        var existed60 = File.Exists(dest60);
        var existed70 = File.Exists(dest70);
        var existed100 = File.Exists(dest100);
        var existed110 = File.Exists(dest110);

        File.Delete(dest25);
        var afterDelete = engine.ScanOnce();

        var staleLocal = Path.Combine(save25, "Request_stale_local.xml");
        File.WriteAllText(staleLocal, "<header timeStamp=\"2026-08-01T00:00:00+08:00\"/><partProcessed identifier=\"STALE\"/>");
        var twoDaysAgo = DateTime.Now.AddHours(-36);
        File.SetCreationTime(staleLocal, twoDaysAgo);
        File.SetLastWriteTime(staleLocal, twoDaysAgo);
        var freshLocal = Path.Combine(save25, "Request_fresh_local.xml");
        File.WriteAllText(freshLocal, "<header timeStamp=\"2026-09-23T15:26:41+08:00\"/><partProcessed identifier=\"FRESH\"/>");

        var staleSource = Path.Combine(st25, "Request_stale_source.xml");
        File.WriteAllText(staleSource, "<header timeStamp=\"2026-08-01T00:00:00+08:00\"/><partProcessed identifier=\"STALESRC\"/>");
        File.SetCreationTime(staleSource, twoDaysAgo);
        File.SetLastWriteTime(staleSource, twoDaysAgo);

        cfg.OnlyNewAfterStart = false;
        var retention = engine.ScanOnce();
        var destStaleSource = Path.Combine(save25, "Request_stale_source.xml");

        var filterDir = Path.Combine(root, "save-filter");
        Directory.CreateDirectory(filterDir);
        var es2 = Path.Combine(Day(watchHousing), "0025_1_1_1_1", "partProcessed", "ES 2");
        var recvDir = Path.Combine(Day(watchHousing), "0025_1_1_1_1", "partReceived");
        Directory.CreateDirectory(es2);
        Directory.CreateDirectory(recvDir);
        File.WriteAllText(Path.Combine(es2, "Request_es2.xml"), "<header timeStamp=\"2026-09-23T15:26:41+08:00\"/><partProcessed identifier=\"ES2\"/>");
        File.WriteAllText(Path.Combine(recvDir, "Request_recv.xml"), "<header timeStamp=\"2026-09-23T15:26:41+08:00\"/><partProcessed identifier=\"RECV\"/>");
        var filterCfg = new AppConfig
        {
            WatchRoot1 = watchHousing,
            WatchRoot2 = watchCover,
            DispenseItems =
            [
                new StationTarget
                {
                    Station = "25",
                    SaveFolder = filterDir,
                    SubFolders = ["partProcessed"]
                }
            ],
            PollIntervalSec = 1,
            LookbackDays = 2,
            KeepDays = 1,
            OnlyNewAfterStart = false,
            MinFileBytes = 8,
            SizeStableChecks = 0,
            SizeStableIntervalMs = 0
        };
        filterCfg.Normalize();
        using var filterEngine = new ExtractEngine(filterCfg);
        filterEngine.PrepareSession();
        filterEngine.ScanOnce();
        var hasEs2 = File.Exists(Path.Combine(filterDir, "Request_es2.xml"));
        var hasRecv = File.Exists(Path.Combine(filterDir, "Request_recv.xml"));
        var discovered = StationSubFolders.DiscoverMany([watchHousing, watchCover], "25", 2);

        File.Delete(Path.Combine(st25, "Request_new25.xml"));
        engine.PruneHandled();
        var prunedAfterDelete = engine.HandledCount;

        var oneRootMissing = new AppConfig
        {
            WatchRoot1 = watchHousing,
            WatchRoot2 = Path.Combine(root, "missing-cover"),
            DispenseItems = [new StationTarget { Station = "25", SaveFolder = save25 }],
            OnlyNewAfterStart = false,
            MinFileBytes = 8,
            SizeStableChecks = 0
        };
        oneRootMissing.Normalize();
        using var halfEngine = new ExtractEngine(oneRootMissing);
        halfEngine.PrepareSession();
        File.WriteAllText(Path.Combine(st25, "Request_half.xml"), "<header timeStamp=\"2026-09-23T16:00:00+08:00\"/><partProcessed identifier=\"HALF\"/>");
        var half = halfEngine.ScanOnce();

        var sb = new StringBuilder();
        sb.AppendLine($"copied={first.Copied}");
        sb.AppendLine($"skipped={first.Skipped}");
        sb.AppendLine($"failed={first.Failed}");
        sb.AppendLine($"error={first.Error ?? ""}");
        sb.AppendLine($"hasNew25={existed25}");
        sb.AppendLine($"hasNew30={existed30}");
        sb.AppendLine($"hasNew60={existed60}");
        sb.AppendLine($"hasNew70={existed70}");
        sb.AppendLine($"hasNew100={existed100}");
        sb.AppendLine($"hasNew110={existed110}");
        sb.AppendLine($"has25InBind={File.Exists(dest25InBind)}");
        sb.AppendLine($"has100InHousing={File.Exists(dest100InHousing)}");
        sb.AppendLine($"hasStationFolder={File.Exists(destStationFolder)}");
        sb.AppendLine($"hasDateFolder={File.Exists(destDateFolder)}");
        sb.AppendLine($"hasSessionOld={File.Exists(destSessionOld)}");
        sb.AppendLine($"hasResponse={File.Exists(destResp)}");
        sb.AppendLine($"hasOther={File.Exists(destOther)}");
        sb.AppendLine($"recopyAfterDelete={afterDelete.CopiedNames.Contains("Request_new25.xml")}");
        sb.AppendLine($"dest25ExistsAfterDelete={File.Exists(dest25)}");
        sb.AppendLine($"purgedStaleLocal={!File.Exists(staleLocal)}");
        sb.AppendLine($"keptFreshLocal={File.Exists(freshLocal)}");
        sb.AppendLine($"skippedStaleSource={!File.Exists(destStaleSource)}");
        sb.AppendLine($"deletedNames={string.Join(",", retention.DeletedNames)}");
        sb.AppendLine($"hasEs2={hasEs2}");
        sb.AppendLine($"hasRecv={hasRecv}");
        sb.AppendLine($"discovered={string.Join("|", discovered)}");
        sb.AppendLine($"prunedAfterDelete={prunedAfterDelete}");
        sb.AppendLine($"halfError={half.Error ?? ""}");
        sb.AppendLine($"halfCopied={half.CopiedNames.Contains("Request_half.xml")}");

        var ok = first.Error is null
                 && first.Copied == 6
                 && existed25 && existed30 && existed60 && existed70 && existed100 && existed110
                 && first.CopiedNames.Contains("Request_new25.xml")
                 && first.CopiedNames.Contains("Request_new100.xml")
                 && !File.Exists(dest25InBind)
                 && !File.Exists(dest100InHousing)
                 && !File.Exists(destStationFolder)
                 && !File.Exists(destDateFolder)
                 && !File.Exists(destSessionOld)
                 && !File.Exists(destResp)
                 && !File.Exists(destOther)
                 && !afterDelete.CopiedNames.Contains("Request_new25.xml")
                 && !File.Exists(dest25)
                 && !File.Exists(staleLocal)
                 && File.Exists(freshLocal)
                 && !File.Exists(destStaleSource)
                 && hasEs2
                 && !hasRecv
                 && discovered.Any(x => x.Contains("partProcessed", StringComparison.OrdinalIgnoreCase))
                 && half.Error is null
                 && half.CopiedNames.Contains("Request_half.xml");

        sb.AppendLine($"ok={ok}");
        Console.WriteLine(sb.ToString());
        try { Directory.Delete(root, true); } catch { }
        return ok ? 0 : 2;
    }
}
