using System;
using System.Collections.Generic;
using System.IO;

namespace WhoEatWifi.Core
{
    public enum AppLanguage
    {
        Chinese,
        English
    }

    public static class LanguageManager
    {
        private static AppLanguage _currentLanguage = AppLanguage.English;
        private static readonly Dictionary<string, string> _zh = new Dictionary<string, string>();
        private static readonly Dictionary<string, string> _en = new Dictionary<string, string>();
        private static readonly string SettingsFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.ini");

        public static event EventHandler LanguageChanged;

        public static AppLanguage CurrentLanguage
        {
            get { return _currentLanguage; }
            set
            {
                if (_currentLanguage != value)
                {
                    _currentLanguage = value;
                    SaveLanguagePreference(value);
                    if (LanguageChanged != null)
                    {
                        LanguageChanged(null, EventArgs.Empty);
                    }
                }
            }
        }

        static LanguageManager()
        {
            InitTranslations();
            LoadLanguagePreference();
        }

        public static string T(string key, params object[] args)
        {
            string val;
            Dictionary<string, string> dict = _currentLanguage == AppLanguage.English ? _en : _zh;
            if (!dict.TryGetValue(key, out val))
            {
                Dictionary<string, string> fallback = _currentLanguage == AppLanguage.English ? _zh : _en;
                if (!fallback.TryGetValue(key, out val))
                {
                    val = key;
                }
            }

            if (args != null && args.Length > 0)
            {
                try
                {
                    return string.Format(val, args);
                }
                catch
                {
                    return val;
                }
            }
            return val;
        }

        private static void LoadLanguagePreference()
        {
            try
            {
                if (File.Exists(SettingsFile))
                {
                    string content = File.ReadAllText(SettingsFile).Trim();
                    if (content.IndexOf("Language=en", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        _currentLanguage = AppLanguage.English;
                    }
                    else if (content.IndexOf("Language=zh", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        _currentLanguage = AppLanguage.Chinese;
                    }
                }
            }
            catch { }
        }

        private static void SaveLanguagePreference(AppLanguage lang)
        {
            try
            {
                string val = lang == AppLanguage.English ? "Language=en" : "Language=zh";
                File.WriteAllText(SettingsFile, val);
            }
            catch { }
        }

        private static void InitTranslations()
        {
            // === Brand & Top ===
            Add("AppTitle", "WhoEatWifi (谁吃了我的 Wi-Fi) - 实时流量监控与防偷跑卫士", "WhoEatWifi - Wi-Fi & Hotspot Data Guard");
            Add("AppSubtitle", "专为 Wi-Fi 与移动热点打造的高精度网络流量监控与防偷跑卫士", "Real-time per-app network monitor & data saver for mobile hotspots");
            Add("BadgeTitle", "谁吃了我的 Wi-Fi", "Who Eat My Wi-Fi?");
            Add("AlwaysOnTop", "窗口置顶", "Always on Top");
            Add("MinToTray", "📥 最小化到托盘", "📥 Minimize to Tray");
            Add("OpenWifiSettings", "打开 Windows 网络设置", "Windows Network Settings");
            Add("Refresh", "🔄 刷新", "🔄 Refresh");
            Add("Language", "语言 / Language", "Language / 语言");

            // === Card 1: Network & SSID ===
            Add("CardNetworkTitle", "当前连接网络", "Connected Network");
            Add("AdapterLabel", "网卡: {0} ({1})", "Adapter: {0} ({1})");
            Add("Checking", "检测中...", "Checking...");
            Add("Disconnected", "未连接网络", "Disconnected");
            Add("MeteredOn", "按流量计费已开启 (防偷跑模式)", "Metered Mode Active (Data Saver)");
            Add("MeteredOff", "普通宽带 (按流量计费未开启)", "Unmetered (Normal Mode)");
            Add("ToggleMeteredBtn", "切换计费", "Toggle Metered");

            // === Card 2: Speeds ===
            Add("CardSpeedTitle", "实时传输速率", "Real-time Speeds");

            // === Card 3: Session Data ===
            Add("CardSessionTitle", "本次连接已用流量", "Current Session Usage");
            Add("ResetBtn", "归零重算", "Reset to 0");
            Add("SessionBreakdown", "下载: {0} | 上传: {1}", "Down: {0} | Up: {1}");
            Add("SessionDuration", "连接时长: {0}", "Duration: {0}");

            // === Card 4: Budget ===
            Add("CardBudgetTitle", "热点预算限额报警", "Hotspot Budget Alert");
            Add("BudgetLabel", "设定额度: ", "Budget Limit: ");
            Add("BudgetNoLimit", "不限额", "No Limit");
            Add("BudgetTip", "💡 提示: 设为计费网络可自动暂停 Windows 更新与 OneDrive 后台下载", "💡 Tip: Metered mode automatically pauses Windows Updates & OneDrive sync");

            // === Tab 1: Real-time Monitor ===
            Add("TabRealtime", "📊 实时流量监控 (Real-time)", "📊 Real-time Monitor");
            Add("BreakdownTitle", "本次连接各 App 流量占比分布 (Traffic Breakdown)", "App Traffic Breakdown (% of Total Usage)");
            Add("BreakdownSubtitle", "按各应用累计总消耗计算占比", "Calculated by cumulative data usage");
            Add("BreakdownWaiting", "等待产生网络流量...", "Waiting for network traffic...");
            Add("BreakdownSummary", "已统计 {0} 个活跃应用 · 最大消耗: {1} ({2})", "{0} active apps · Top consumer: {1} ({2})");
            Add("OtherApps", "其他应用", "Other Apps");
            Add("SearchPlaceholder", "搜索应用名称或 PID...", "Search app name or PID...");
            Add("SortLabel", "排序方式: ", "Sort by: ");
            Add("SortByPercentage", "按累计流量占比 % (默认)", "By Data Usage % (Default)");
            Add("SortBySpeed", "按实时下载速度", "By Download Speed");
            Add("SortByName", "按应用名称", "By App Name");

            // === Table Columns ===
            Add("ColApp", "应用程序 / 进程", "Application / Process");
            Add("ColPercentage", "本次流量占比 %", "Data Usage %");
            Add("ColDownSpeed", "实时下载速度", "Live Download");
            Add("ColUpSpeed", "实时上传速度", "Live Upload");
            Add("ColTotalBytes", "本次总消耗", "Total Data");
            Add("ColActions", "快捷操作", "Actions");
            Add("BtnKill", "结束进程", "End Task");
            Add("BtnLocate", "定位目录", "Open Location");

            // === Tab 2: 1-Hour History ===
            Add("TabHistory", "🕒 近 1 小时历史记录 (1-Hour History)", "🕒 1-Hour History");
            Add("HistoryTip", "💡 系统会自动在您切换 Wi-Fi/热点时将上一网络的消耗归档。此页面保存最近 1 小时内的历史记录，超期记录自动淘汰释放。", "💡 Previous sessions are archived when switching Wi-Fi/hotspot. Stores up to 1 hour; older sessions are pruned automatically.");
            Add("HistoryListTitle", "过去 1 小时内的网络会话", "Sessions in the Last 1 Hour");
            Add("HistoryDetailPlaceholder", "请选择左侧历史会话以查看明细", "Select a session on the left to view details");
            Add("HistoryDetailTitle", "网络「{0}」流量明细", "Traffic breakdown for \"{0}\"");
            Add("HistoryDetailSubtitle", "连接时间: {0} · 持续时长: {1} · 总消耗: {2} (下行: {3} | 上行: {4})", "Time: {0} · Duration: {1} · Total: {2} (Down: {3} | Up: {4})");
            Add("ColHistoryApp", "应用 / 进程名称", "App / Process Name");
            Add("ColHistoryPct", "该网络占比 %", "Network %");
            Add("ColHistoryDown", "下载消耗", "Downloaded");
            Add("ColHistoryUp", "上传消耗", "Uploaded");
            Add("ColHistoryTotal", "该会话总计", "Session Total");

            // === Status Bar ===
            Add("StatusMonitorReady", "● 高性能网络监控已就绪 (TCP/UDP 全协议活跃跟踪)", "● Network monitor active (TCP/UDP socket tracking)");
            Add("StatusAppsCount", "监控中应用: {0}", "Active Apps: {0}");
            Add("StatusBrand", "WhoEatWifi · 专为 Windows 打造", "WhoEatWifi · Built for Windows");

            // === Tray Menu & Tooltip ===
            Add("TrayTitle", "WhoEatWifi 流量卫士", "WhoEatWifi Data Guard");
            Add("TrayMenuShow", "显示主界面 (&O)", "Show Dashboard (&O)");
            Add("TrayMenuNetwork", "当前网络: {0}", "Current Network: {0}");
            Add("TrayMenuMeteredOn", "开启【按流量计费】防偷跑 (&M)", "Enable Metered Mode (Data Saver) (&M)");
            Add("TrayMenuMeteredOff", "关闭【按流量计费】(恢复普通模式)", "Disable Metered Mode (Normal Mode)");
            Add("TrayMenuReset", "归零重算本次流量 (&R)", "Reset Current Session (&R)");
            Add("TrayMenuExit", "退出程序 (&X)", "Exit (&X)");
            Add("TrayNoticeTitle", "WhoEatWifi 流量卫士", "WhoEatWifi Data Guard");
            Add("TrayNoticeText", "程序已最小化到右下角托盘继续后台守护，双击托盘图标可随时打开主界面。", "Running minimized in system tray. Double-click tray icon anytime to open dashboard.");

            // === Notifications & Alerts ===
            Add("AlertNetworkSwitched", "🔔 网络已自动切换：已从「{0}」切换至「{1}」。当前网络流量已归零重算，历史记录已封存至近1小时历史记录中。", "🔔 Network switched from \"{0}\" to \"{1}\". Current data reset to zero; previous session archived to 1-hour history.");
            Add("AlertBudgetExceeded", "⚠️ 流量预警：当前连接消耗已达到 {0}，已超出设定的预算限额！请检查是否有大流量应用在偷跑。", "⚠️ Data Budget Alert: Current connection has used {0}, exceeding your limit! Check for bandwidth-heavy apps.");
            Add("NotifyNetworkSwitchedTitle", "📶 Wi-Fi 切换感知", "📶 Wi-Fi Switched");
            Add("NotifyNetworkSwitchedMsg", "网络已切换至「{0}」，上一热点数据已归档，流量已归零重算。", "Switched to \"{0}\". Previous hotspot data archived, counter reset to 0.");
            Add("NotifyBudgetTitle", "⚠️ 流量超额警报", "⚠️ Hotspot Budget Alert");
            Add("NotifyBudgetMsg", "当前连接流量已达 {0}，已超出设定的预算限额！请留意偷跑程序。", "Data usage reached {0}, exceeding your budget limit! Check for bandwidth hogs.");

            // === Confirmations ===
            Add("ConfirmResetTitle", "确认重置", "Confirm Reset");
            Add("ConfirmResetMsg", "确定要重置当前网络的累计流量统计吗？\n当前统计数据将自动封存至近1小时历史记录中。", "Are you sure you want to reset current network data?\nCurrent stats will be archived to 1-hour history.");
            Add("ConfirmKillTitle", "结束偷跑进程确认", "Confirm End Task");
            Add("ConfirmKillMsg", "确定要强行结束进程「{0}」(PID: {1}) 吗？\n该操作将立即切断其网络连接。", "Are you sure you want to terminate \"{0}\" (PID: {1})?\nThis will disconnect its network immediately.");
            Add("KillSuccess", "进程「{0}」已终止。", "Process \"{0}\" terminated.");
            Add("KillFailed", "结束进程失败: {0}", "Failed to terminate process: {0}");
            Add("LocateFailed", "无法定位该程序的可执行文件路径（可能是系统受保护进程）。", "Unable to locate executable path (protected system process).");
            Add("LocateError", "打开目录失败: {0}", "Failed to open directory: {0}");
            Add("MeteredUpdatedTitle", "计费模式已更新", "Metered Mode Updated");
            Add("MeteredUpdatedOn", "已开启【按流量计费】（防偷跑模式，系统将暂停大文件同步与更新）", "Metered mode ENABLED (Data saver active: Windows updates and OneDrive paused)");
            Add("MeteredUpdatedOff", "已关闭【按流量计费】（恢复普通网络模式）", "Metered mode DISABLED (Normal network mode restored)");
            Add("MeteredGuideTitle", "设置引导", "Settings Guide");
            Add("MeteredGuideMsg", "系统接口未能直接更改计费设置，是否前往 Windows 网络设置手动开启“按流量计费的网络”？", "Could not toggle metered mode directly. Open Windows Network Settings to toggle manually?");
        }

        private static void Add(string key, string zh, string en)
        {
            _zh[key] = zh;
            _en[key] = en;
        }
    }
}
