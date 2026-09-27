using System.Text;

namespace CivicTimeChecker;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        if (args.Any(a => string.Equals(a, "--self-test", StringComparison.OrdinalIgnoreCase)))
        {
            Environment.ExitCode = RunSelfTest();
            return;
        }

        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        AntdUI.Localization.DefaultLanguage = "zh-CN";
        AntdUI.Config.TextRenderingHighQuality = true;
        AntdUI.Config.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
        Application.Run(new MainForm());
    }

    private static int RunSelfTest()
    {
        var root = Path.Combine(Path.GetTempPath(), "civic-time-" + Guid.NewGuid().ToString("N"));
        var d25 = Path.Combine(root, "25");
        var d30 = Path.Combine(root, "30");
        var d60 = Path.Combine(root, "60");
        var d70 = Path.Combine(root, "70");
        var d100 = Path.Combine(root, "100");
        var d110 = Path.Combine(root, "110");
        foreach (var dir in new[] { d25, d30, d60, d70, d100, d110 })
            Directory.CreateDirectory(dir);

        const string housing = "009756186180205070044250001";
        const string cover = "184898886180155130032250002";
        const string mmb = "69165000000480022608318629503833";
        const string heatsink = "408665086180160350040250002";
        const string bbb = "69165000000470022609028629503832";
        const string csb = "69157000000410022608318629503837";
        const string emc = "369450SAMPLE0605183";

        static string DispenseXml(string code, string stamp) => $"""
            <?xml version="1.0" encoding="UTF-8"?>
            <root>
              <header timeStamp="{stamp}" />
              <event>
                <partProcessed identifier="{code}" />
              </event>
              <body>
                <structArrays>
                  <array name="components">
                    <values>
                      <item matID="GLUA" batch="H122Q1K037" />
                    </values>
                  </array>
                </structArrays>
              </body>
            </root>
            """;

        File.WriteAllText(Path.Combine(d25, "Request_h25.xml"), DispenseXml(housing, "2026-09-23T15:26:41.0129711+08:00"));
        File.WriteAllText(Path.Combine(d60, "Request_h60.xml"), DispenseXml(housing, "2026-09-23T15:29:39.9882131+08:00"));
        File.WriteAllText(Path.Combine(d100, "Request_c100.xml"), DispenseXml(cover, "2026-09-23T15:27:35.6283484+08:00"));

        File.WriteAllText(Path.Combine(d30, "Request_bind30.xml"), """
            <?xml version="1.0" encoding="UTF-8"?>
            <root>
              <header timeStamp="2026-09-23T15:27:55.4783482+08:00" />
              <event>
                <partProcessed identifier="009756186180205070044250001" />
              </event>
              <body>
                <processData>
                  <item name="Heatsink" value="408665086180160350040250002" dataType="8" />
                  <item name="PCB" value="69165000000480022608318629503833" dataType="8" />
                </processData>
                <structArrays>
                  <array name="components">
                    <values>
                      <item identifier="69165000000480022608318629503833" typeNo="7517402383" class="NA" />
                    </values>
                  </array>
                </structArrays>
              </body>
            </root>
            """);

        File.WriteAllText(Path.Combine(d70, "Request_bind70.xml"), """
            <?xml version="1.0" encoding="UTF-8"?>
            <root>
              <header timeStamp="2026-09-23T15:31:36.8049703+08:00" />
              <event>
                <partProcessed identifier="009756186180205070044250001" />
              </event>
              <body>
                <processData>
                  <item name="EMCShield" value="369450SAMPLE0605183" dataType="8" />
                  <item name="PCB" value="69165000000470022609028629503832" dataType="8" />
                </processData>
              </body>
            </root>
            """);

        File.WriteAllText(Path.Combine(d110, "Request_bind110.xml"), """
            <?xml version="1.0" encoding="UTF-8"?>
            <root>
              <header timeStamp="2026-09-23T15:28:22.9866941+08:00" />
              <event>
                <partProcessed identifier="184898886180155130032250002" />
              </event>
              <body>
                <processData>
                  <item name="PCB" value="69157000000410022608318629503837" dataType="8" />
                </processData>
              </body>
            </root>
            """);

        var cfg = new AppConfig
        {
            Folders =
            [
                new FolderProfile { Name = "25点检", MessageFolder = d25, Role = Roles.Dispense, Pair = Pairs.Housing25, ClearGlueMinutes = 15 },
                new FolderProfile { Name = "30组装", MessageFolder = d30, Role = Roles.Bind, Pair = Pairs.Housing25 },
                new FolderProfile { Name = "60点胶", MessageFolder = d60, Role = Roles.Dispense, Pair = Pairs.Housing60, ClearGlueMinutes = 20 },
                new FolderProfile { Name = "70组装", MessageFolder = d70, Role = Roles.Bind, Pair = Pairs.Housing60 },
                new FolderProfile { Name = "100点胶", MessageFolder = d100, Role = Roles.Dispense, Pair = Pairs.Cover100, ClearGlueMinutes = 30 },
                new FolderProfile { Name = "110组装", MessageFolder = d110, Role = Roles.Bind, Pair = Pairs.Cover100 }
            ]
        };
        cfg.Normalize();

        var byHousing = RequestXmlSearch.Find(housing, cfg);
        var byCover = RequestXmlSearch.Find(cover, cfg);
        var byMmb = RequestXmlSearch.Find(mmb, cfg);
        var byHeat = RequestXmlSearch.Find(heatsink, cfg);
        var byBbb = RequestXmlSearch.Find(bbb, cfg);
        var byCsb = RequestXmlSearch.Find(csb, cfg);
        var unknown = RequestXmlSearch.Find("NO_SUCH_CODE", cfg);

        var bindOnly = new AppConfig
        {
            Folders = [new FolderProfile { Name = "仅组装", MessageFolder = d30, Role = Roles.Bind, Pair = Pairs.Housing25 }]
        };
        bindOnly.Normalize();
        var materialNoDispense = RequestXmlSearch.Find(mmb, bindOnly);

        var waitOk = WaitRules.CanClearGlue(TimeSpan.FromMinutes(15), 15)
                     && !WaitRules.CanClearGlue(TimeSpan.FromMinutes(14.9), 15)
                     && WaitRules.CanClearGlue(TimeSpan.FromMinutes(15.1), 15);

        var housingPicks60 = byHousing.Found
                             && byHousing.Profile?.Name == "60点胶"
                             && byHousing.DispenseTime is DateTime ht
                             && ht.Hour == 15 && ht.Minute == 29
                             && byHousing.Profile.ClearGlueMinutes == 20;
        var mmbPicks25 = byMmb.Found
                         && byMmb.MaterialCode == mmb
                         && byMmb.HousingCode == housing
                         && byMmb.Profile?.Name == "25点检"
                         && byMmb.DispenseTime is DateTime mt
                         && mt.Minute == 26
                         && byMmb.Profile.ClearGlueMinutes == 15;
        var heatPicks25 = byHeat.Found && byHeat.Profile?.Name == "25点检" && byHeat.HousingCode == housing;
        var bbbPicks60 = byBbb.Found && byBbb.Profile?.Name == "60点胶" && byBbb.HousingCode == housing;
        var coverOk = byCover.Found && byCover.Profile?.Name == "100点胶" && byCover.HousingCode == cover;
        var csbOk = byCsb.Found && byCsb.Profile?.Name == "100点胶" && byCsb.HousingCode == cover;

        var store = Path.Combine(root, "rework.json");
        var recHouse = ReworkTracker.TryRecord(byHousing, cfg, store);
        var recHouseDup = ReworkTracker.TryRecord(byHousing, cfg, store);
        var recMmb = ReworkTracker.TryRecord(byMmb, cfg, store);
        var recCover = ReworkTracker.TryRecord(byCover, cfg, store);
        var recUnknown = ReworkTracker.TryRecord(unknown, cfg, store);
        var recNoDisp = ReworkTracker.TryRecord(materialNoDispense, cfg, store);
        var snap = ReworkTracker.Snapshot(cfg, store);
        var s25 = snap.First(x => x.Name == "25点检");
        var s60 = snap.First(x => x.Name == "60点胶");
        var s100 = snap.First(x => x.Name == "100点胶");
        File.Delete(Path.Combine(d60, "Request_h60.xml"));
        RequestIndex.Flush();
        var snapGone = ReworkTracker.Snapshot(cfg, store);
        var s60Gone = snapGone.First(x => x.Name == "60点胶");
        var s25Gone = snapGone.First(x => x.Name == "25点检");

        var reworkOk = recHouse && !recHouseDup && recMmb && recCover && !recUnknown && !recNoDisp
                       && s25.Produced == 1 && s25.Rework == 1
                       && s60.Produced == 1 && s60.Rework == 1
                       && s100.Produced == 1 && s100.Rework == 1
                       && s60Gone.Produced == 0 && s60Gone.Rework == 0
                       && s25Gone.Produced == 1 && s25Gone.Rework == 1;

        RequestIndex.Flush();
        var flushed = RequestIndex.Count == 0;

        var ok = housingPicks60 && mmbPicks25 && heatPicks25 && bbbPicks60 && coverOk && csbOk
                 && !unknown.Found
                 && !materialNoDispense.Found
                 && waitOk && reworkOk && flushed;

        var sb = new StringBuilder();
        sb.AppendLine($"housingFound={byHousing.Found} profile={byHousing.Profile?.Name} time={byHousing.DispenseTime:HH:mm:ss} min={byHousing.Profile?.ClearGlueMinutes}");
        sb.AppendLine($"coverFound={byCover.Found} profile={byCover.Profile?.Name}");
        sb.AppendLine($"mmbFound={byMmb.Found} profile={byMmb.Profile?.Name} time={byMmb.DispenseTime:HH:mm:ss}");
        sb.AppendLine($"heatFound={byHeat.Found} profile={byHeat.Profile?.Name}");
        sb.AppendLine($"bbbFound={byBbb.Found} profile={byBbb.Profile?.Name}");
        sb.AppendLine($"csbFound={byCsb.Found} profile={byCsb.Profile?.Name}");
        sb.AppendLine($"unknownFound={unknown.Found}");
        sb.AppendLine($"materialNoDispenseFound={materialNoDispense.Found}");
        sb.AppendLine($"waitOk={waitOk}");
        sb.AppendLine($"reworkOk={reworkOk}");
        sb.AppendLine($"s25={s25.Produced}/{s25.Rework}");
        sb.AppendLine($"s60={s60.Produced}/{s60.Rework}");
        sb.AppendLine($"s100={s100.Produced}/{s100.Rework}");
        sb.AppendLine($"s60gone={s60Gone.Produced}/{s60Gone.Rework}");
        sb.AppendLine($"emcWouldHit={RequestXmlSearch.Find(emc, cfg).Found}");
        sb.AppendLine($"flushed={flushed}");
        sb.AppendLine($"ok={ok}");
        Console.WriteLine(sb.ToString());
        try { Directory.Delete(root, true); } catch { }
        return ok ? 0 : 2;
    }
}
