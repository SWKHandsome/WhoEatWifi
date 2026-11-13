using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.NetworkInformation;
using System.Windows.Media;
using System.Windows.Threading;
using WhoEatWifi.Models;

namespace WhoEatWifi.Core
{
    public class NetworkStatsManager : IDisposable
    {
        private readonly DispatcherTimer _timer;
        private readonly SessionHistoryManager _historyManager;

        private readonly ObservableCollection<ProcessTrafficInfo> _processes;
        private readonly Dictionary<int, ProcessTrafficInfo> _processMap;
        private readonly Dictionary<int, Tuple<ulong, ulong>> _lastTcpEStats;

        private string _activeInterfaceName;
        private long _lastTotalBytesReceived;
        private long _lastTotalBytesSent;
        private bool _isFirstTick = true;

        private long _currentTotalDownloadSpeed;
        private long _currentTotalUploadSpeed;

        private WifiInfo _currentWifi;
        private long _budgetLimitBytes = 0; // 0 = no limit
        private bool _budgetExceededAlertTriggered = false;
        private string _statusMessage = "高性能网络监控已启动";

        public event EventHandler<string> StatusChanged;
        public event EventHandler<long> BudgetExceeded;

        public ObservableCollection<ProcessTrafficInfo> Processes
        {
            get { return _processes; }
        }

        public SessionHistoryManager HistoryManager
        {
            get { return _historyManager; }
        }

        public WifiInfo CurrentWifi
        {
            get { return _currentWifi; }
        }

        public long CurrentTotalDownloadSpeed
        {
            get { return _currentTotalDownloadSpeed; }
        }

        public long CurrentTotalUploadSpeed
        {
            get { return _currentTotalUploadSpeed; }
        }

        public string FormattedTotalDownloadSpeed
        {
            get { return ProcessTrafficInfo.FormatSpeed(_currentTotalDownloadSpeed); }
        }

        public string FormattedTotalUploadSpeed
        {
            get { return ProcessTrafficInfo.FormatSpeed(_currentTotalUploadSpeed); }
        }

        public string EtwStatus
        {
            get { return _statusMessage; }
        }

        public bool IsEtwRunning
        {
            get { return true; }
        }

        public long BudgetLimitBytes
        {
            get { return _budgetLimitBytes; }
            set
            {
                _budgetLimitBytes = value;
                _budgetExceededAlertTriggered = false;
            }
        }

        public NetworkStatsManager()
        {
            _processes = new ObservableCollection<ProcessTrafficInfo>();
            _processMap = new Dictionary<int, ProcessTrafficInfo>();
            _lastTcpEStats = new Dictionary<int, Tuple<ulong, ulong>>();
            _historyManager = new SessionHistoryManager();

            _currentWifi = WifiHelper.GetCurrentWifiInfo();
            _activeInterfaceName = _currentWifi.InterfaceName;
            _historyManager.InitializeFirstSession(_currentWifi.Ssid, _activeInterfaceName);

            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += OnTick;
        }

        public void Start()
        {
            _statusMessage = "● 高性能网络监控已就绪 (TCP/UDP 全协议活跃跟踪)";
            if (StatusChanged != null)
            {
                StatusChanged(this, _statusMessage);
            }

            _timer.Start();
        }

        public void Stop()
        {
            _timer.Stop();
        }

        private void OnTick(object sender, EventArgs e)
        {
            try
            {
                // 1. Check Wi-Fi / Hotspot status & SSID change
                WifiInfo latestWifi = WifiHelper.GetCurrentWifiInfo();
                _currentWifi = latestWifi;

                // Detect network switch
                bool switched = _historyManager.CheckNetworkSwitch(latestWifi.Ssid, latestWifi.InterfaceName, _processes);
                if (switched)
                {
                    // Reset per-process session totals on network switch
                    foreach (var p in _processes)
                    {
                        p.TotalDownloadedBytes = 0;
                        p.TotalUploadedBytes = 0;
                        p.DownloadSpeedBps = 0;
                        p.UploadSpeedBps = 0;
                    }
                    _isFirstTick = true;
                    _budgetExceededAlertTriggered = false;
                    _lastTcpEStats.Clear();
                }

                // 2. Query Adapter Statistics
                long currentBytesRecv = 0;
                long currentBytesSent = 0;

                try
                {
                    NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();
                    NetworkInterface targetNi = null;

                    // First find wireless interface that is Up
                    foreach (var ni in interfaces)
                    {
                        if (ni.OperationalStatus == OperationalStatus.Up &&
                            (ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 ||
                             ni.Description.IndexOf("Wi-Fi", StringComparison.OrdinalIgnoreCase) >= 0 ||
                             ni.Name.IndexOf("Wi-Fi", StringComparison.OrdinalIgnoreCase) >= 0))
                        {
                            targetNi = ni;
                            break;
                        }
                    }

                    // If not found, pick first Up interface
                    if (targetNi == null)
                    {
                        foreach (var ni in interfaces)
                        {
                            if (ni.OperationalStatus == OperationalStatus.Up &&
                                ni.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                            {
                                targetNi = ni;
                                break;
                            }
                        }
                    }

                    if (targetNi != null)
                    {
                        _activeInterfaceName = targetNi.Name;
                        IPv4InterfaceStatistics stats = targetNi.GetIPv4Statistics();
                        currentBytesRecv = stats.BytesReceived;
                        currentBytesSent = stats.BytesSent;
                    }
                }
                catch
                {
                    // Ignore adapter stats failure
                }

                long downDelta = 0;
                long upDelta = 0;

                if (_isFirstTick)
                {
                    _lastTotalBytesReceived = currentBytesRecv;
                    _lastTotalBytesSent = currentBytesSent;
                    _currentTotalDownloadSpeed = 0;
                    _currentTotalUploadSpeed = 0;
                    _isFirstTick = false;
                }
                else
                {
                    downDelta = currentBytesRecv - _lastTotalBytesReceived;
                    upDelta = currentBytesSent - _lastTotalBytesSent;

                    if (downDelta < 0) downDelta = 0;
                    if (upDelta < 0) upDelta = 0;

                    _lastTotalBytesReceived = currentBytesRecv;
                    _lastTotalBytesSent = currentBytesSent;

                    _currentTotalDownloadSpeed = downDelta;
                    _currentTotalUploadSpeed = upDelta;

                    if (_historyManager.CurrentSession != null)
                    {
                        _historyManager.CurrentSession.TotalDownloadedBytes += downDelta;
                        _historyManager.CurrentSession.TotalUploadedBytes += upDelta;
                    }
                }

                // 3. Scan Active Network Sockets & Per-Process Attribution
                Dictionary<int, PidNetworkActivity> activities = IphlpNetworkMonitor.GetActiveNetworkActivity();

                // Reset speeds to 0 for all existing processes
                foreach (var p in _processes)
                {
                    p.DownloadSpeedBps = 0;
                    p.UploadSpeedBps = 0;
                }

                long remainingDown = downDelta;
                long remainingUp = upDelta;
                int totalSockets = 0;

                // First pass: Calculate direct EStats deltas if available
                var directDeltas = new Dictionary<int, Tuple<long, long>>();

                foreach (var kvp in activities)
                {
                    int pid = kvp.Key;
                    PidNetworkActivity act = kvp.Value;
                    totalSockets += act.TotalSocketCount;

                    if (act.TcpBytesIn > 0 || act.TcpBytesOut > 0)
                    {
                        Tuple<ulong, ulong> last;
                        if (_lastTcpEStats.TryGetValue(pid, out last))
                        {
                            long estatsDown = (long)(act.TcpBytesIn >= last.Item1 ? act.TcpBytesIn - last.Item1 : 0);
                            long estatsUp = (long)(act.TcpBytesOut >= last.Item2 ? act.TcpBytesOut - last.Item2 : 0);

                            if (estatsDown > 0 || estatsUp > 0)
                            {
                                directDeltas[pid] = Tuple.Create(estatsDown, estatsUp);
                                remainingDown -= estatsDown;
                                remainingUp -= estatsUp;
                            }
                        }
                        _lastTcpEStats[pid] = Tuple.Create(act.TcpBytesIn, act.TcpBytesOut);
                    }
                }

                if (remainingDown < 0) remainingDown = 0;
                if (remainingUp < 0) remainingUp = 0;

                // Ensure all active networking processes exist in the UI model
                foreach (var kvp in activities)
                {
                    int pid = kvp.Key;
                    PidNetworkActivity act = kvp.Value;

                    ProcessTrafficInfo info;
                    if (!_processMap.TryGetValue(pid, out info))
                    {
                        ProcessMeta meta = ProcessResolver.Resolve(pid);
                        info = new ProcessTrafficInfo
                        {
                            ProcessId = pid,
                            ProcessName = meta.ProcessName,
                            DisplayName = meta.DisplayName,
                            ExecutablePath = meta.ExecutablePath,
                            Icon = meta.Icon
                        };
                        _processMap[pid] = info;
                        _processes.Add(info);
                    }

                    long pidDown = 0;
                    long pidUp = 0;

                    // Direct EStats
                    Tuple<long, long> direct;
                    if (directDeltas.TryGetValue(pid, out direct))
                    {
                        pidDown += direct.Item1;
                        pidUp += direct.Item2;
                    }

                    // Distribute remaining adapter bandwidth proportionally by socket count
                    if (totalSockets > 0 && (remainingDown > 0 || remainingUp > 0))
                    {
                        double weight = (double)act.TotalSocketCount / totalSockets;
                        pidDown += (long)(remainingDown * weight);
                        pidUp += (long)(remainingUp * weight);
                    }

                    info.DownloadSpeedBps = pidDown;
                    info.UploadSpeedBps = pidUp;
                    info.TotalDownloadedBytes += pidDown;
                    info.TotalUploadedBytes += pidUp;
                }

                // 4. Recalculate usage percentages across all processes
                long totalTrackedBytes = 0;
                foreach (var p in _processes) totalTrackedBytes += p.TotalBytes;

                long sessionTotal = _historyManager.CurrentSession != null ? _historyManager.CurrentSession.TotalBytes : 0;
                long denom = Math.Max(totalTrackedBytes, sessionTotal);

                foreach (var p in _processes)
                {
                    if (denom > 0 && p.TotalBytes > 0)
                    {
                        p.UsagePercentage = Math.Min(100.0, (double)p.TotalBytes / denom * 100.0);
                    }
                    else
                    {
                        p.UsagePercentage = 0.0;
                    }
                }

                // Check budget alert
                if (_budgetLimitBytes > 0 && !_budgetExceededAlertTriggered)
                {
                    if (_historyManager.CurrentSession != null &&
                        _historyManager.CurrentSession.TotalBytes >= _budgetLimitBytes)
                    {
                        _budgetExceededAlertTriggered = true;
                        if (BudgetExceeded != null)
                        {
                            BudgetExceeded(this, _historyManager.CurrentSession.TotalBytes);
                        }
                    }
                }
            }
            catch
            {
                // Prevent unhandled exception in timer tick
            }
        }

        public void ResetCurrentSession()
        {
            _historyManager.ManualResetSession(_processes);
            foreach (var p in _processes)
            {
                p.TotalDownloadedBytes = 0;
                p.TotalUploadedBytes = 0;
                p.DownloadSpeedBps = 0;
                p.UploadSpeedBps = 0;
            }
            _lastTcpEStats.Clear();
            _isFirstTick = true;
            _budgetExceededAlertTriggered = false;
        }

        private static readonly Brush[] SegmentBrushes = new Brush[]
        {
            new SolidColorBrush(Color.FromRgb(37, 99, 235)),  // #2563EB Blue
            new SolidColorBrush(Color.FromRgb(124, 58, 237)), // #7C3AED Purple
            new SolidColorBrush(Color.FromRgb(6, 182, 212)),  // #06B6D4 Cyan
            new SolidColorBrush(Color.FromRgb(245, 158, 11)), // #F59E0B Amber
            new SolidColorBrush(Color.FromRgb(236, 72, 153)), // #EC4899 Pink
            new SolidColorBrush(Color.FromRgb(148, 163, 184)) // #94A3B8 Slate (Others)
        };

        static NetworkStatsManager()
        {
            foreach (var b in SegmentBrushes) b.Freeze();
        }

        public List<TrafficSegment> GetTopBreakdownSegments()
        {
            var list = new List<TrafficSegment>();
            long totalTracked = 0;
            foreach (var p in _processes) totalTracked += p.TotalBytes;
            if (totalTracked <= 0) return list;

            var sorted = _processes.Where(p => p.TotalBytes > 0).OrderByDescending(p => p.TotalBytes).ToList();
            if (sorted.Count == 0) return list;

            double othersPct = 0;
            long othersBytes = 0;

            int maxTop = 4;
            for (int i = 0; i < sorted.Count; i++)
            {
                var proc = sorted[i];
                double pct = (double)proc.TotalBytes / totalTracked * 100.0;

                if (i < maxTop && pct >= 0.5)
                {
                    list.Add(new TrafficSegment
                    {
                        Name = proc.DisplayName,
                        Percentage = pct,
                        Bytes = proc.TotalBytes,
                        ColorBrush = SegmentBrushes[i % (SegmentBrushes.Length - 1)],
                        FormattedPercentage = pct.ToString("F1") + "%",
                        FormattedBytes = AppTrafficRecord.FormatBytes(proc.TotalBytes)
                    });
                }
                else
                {
                    othersPct += pct;
                    othersBytes += proc.TotalBytes;
                }
            }

            if (othersBytes > 0)
            {
                list.Add(new TrafficSegment
                {
                    Name = LanguageManager.T("OtherApps"),
                    Percentage = othersPct,
                    Bytes = othersBytes,
                    ColorBrush = SegmentBrushes[SegmentBrushes.Length - 1],
                    FormattedPercentage = othersPct.ToString("F1") + "%",
                    FormattedBytes = AppTrafficRecord.FormatBytes(othersBytes)
                });
            }

            return list;
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
