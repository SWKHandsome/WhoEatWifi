using System;
using System.Collections.Generic;
using WhoEatWifi.Core;
using WhoEatWifi.Models;

namespace WhoEatWifi.Tests
{
    public class CoreTests
    {
        public static int Main()
        {
            int failed = 0;
            Console.WriteLine("=== WhoEatWifi Core Unit Verification ===");

            // Test 1: Byte & Speed Formatting
            failed += Assert("FormatBytes(0)", AppTrafficRecord.FormatBytes(0) == "0 B");
            failed += Assert("FormatBytes(1024)", AppTrafficRecord.FormatBytes(1024) == "1.0 KB");
            failed += Assert("FormatBytes(1048576)", AppTrafficRecord.FormatBytes(1048576) == "1.0 MB");
            failed += Assert("FormatBytes(1073741824)", AppTrafficRecord.FormatBytes(1073741824) == "1.00 GB");

            // Test 2: NetworkSession Duration & TimeRange
            NetworkSession s = new NetworkSession();
            s.Ssid = "Test-Hotspot";
            s.StartTime = DateTime.Now.AddMinutes(-25);
            s.EndTime = DateTime.Now;
            failed += Assert("Duration calculation", s.Duration.TotalMinutes >= 24 && s.Duration.TotalMinutes <= 26);
            failed += Assert("FormattedDuration contains 分钟", s.FormattedDuration.IndexOf("分") >= 0);

            // Test 3: SessionHistoryManager Switch
            SessionHistoryManager mgr = new SessionHistoryManager();
            mgr.InitializeFirstSession("Hotspot_A", "Wi-Fi");

            var dummyApps = new List<ProcessTrafficInfo>
            {
                new ProcessTrafficInfo { ProcessId = 1234, ProcessName = "chrome", DisplayName = "Google Chrome", TotalDownloadedBytes = 50000000, TotalUploadedBytes = 2000000 },
                new ProcessTrafficInfo { ProcessId = 5678, ProcessName = "onedrive", DisplayName = "Microsoft OneDrive", TotalDownloadedBytes = 150000000, TotalUploadedBytes = 500000 }
            };

            bool switched = mgr.CheckNetworkSwitch("Home_Wi-Fi_5G", "Wi-Fi", dummyApps);
            failed += Assert("Network switch detected", switched);
            failed += Assert("History has 1 archived session", mgr.HistorySessions.Count == 1);
            failed += Assert("Archived SSID is Hotspot_A", mgr.HistorySessions[0].Ssid == "Hotspot_A");
            failed += Assert("Archived session has 2 apps", mgr.HistorySessions[0].AppRecords.Count == 2);
            failed += Assert("Top consumer is OneDrive", mgr.HistorySessions[0].AppRecords[0].ProcessName == "onedrive");
            failed += Assert("OneDrive percentage > 70%", mgr.HistorySessions[0].AppRecords[0].UsagePercentage > 70.0);
            failed += Assert("Chrome percentage > 20%", mgr.HistorySessions[0].AppRecords[1].UsagePercentage > 20.0);
            failed += Assert("Current session SSID is Home_Wi-Fi_5G", mgr.CurrentSession.Ssid == "Home_Wi-Fi_5G");

            // Test 4: History Pruning (1-hour cutoff)
            // Inject an old session (> 1 hour ago)
            NetworkSession oldSession = new NetworkSession
            {
                Ssid = "Old_Coffee_Shop",
                StartTime = DateTime.Now.AddHours(-3),
                EndTime = DateTime.Now.AddMinutes(-70)
            };
            mgr.HistorySessions.Add(oldSession);
            failed += Assert("History has 2 before prune", mgr.HistorySessions.Count == 2);

            mgr.PruneOldHistory();
            failed += Assert("History has 1 after prune", mgr.HistorySessions.Count == 1);
            failed += Assert("Old_Coffee_Shop was pruned", mgr.HistorySessions[0].Ssid == "Hotspot_A");

            // Test 5: ProcessResolver system process
            ProcessMeta idleMeta = ProcessResolver.Resolve(0);
            failed += Assert("PID 0 is Idle", idleMeta.ProcessName == "System Idle Process");
            ProcessMeta sysMeta = ProcessResolver.Resolve(4);
            failed += Assert("PID 4 is System", sysMeta.ProcessName == "System");

            // Test 6: LanguageManager bilingual translation & toggle
            LanguageManager.CurrentLanguage = AppLanguage.English;
            failed += Assert("LanguageManager default English", LanguageManager.CurrentLanguage == AppLanguage.English);
            failed += Assert("LanguageManager English T('MinToTray')", LanguageManager.T("MinToTray") == "📥 Minimize to Tray");
            failed += Assert("LanguageManager English T('BtnKill')", LanguageManager.T("BtnKill") == "End Task");

            LanguageManager.CurrentLanguage = AppLanguage.Chinese;
            failed += Assert("LanguageManager switch to Chinese", LanguageManager.CurrentLanguage == AppLanguage.Chinese);
            failed += Assert("LanguageManager Chinese T('MinToTray')", LanguageManager.T("MinToTray") == "📥 最小化到托盘");
            failed += Assert("LanguageManager Chinese T('BtnKill')", LanguageManager.T("BtnKill") == "结束进程");

            // Restore English as default per user requirement
            LanguageManager.CurrentLanguage = AppLanguage.English;
            failed += Assert("LanguageManager restore to English", LanguageManager.CurrentLanguage == AppLanguage.English);

            Console.WriteLine(string.Format("=== Test Finished: {0} Failed ===", failed));
            return failed == 0 ? 0 : 1;
        }

        private static int Assert(string testName, bool condition)
        {
            if (condition)
            {
                Console.WriteLine(string.Format("  [PASS] {0}", testName));
                return 0;
            }
            else
            {
                Console.WriteLine(string.Format("  [FAIL] {0}", testName));
                return 1;
            }
        }
    }
}
