using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using WhoEatWifi.Models;

namespace WhoEatWifi.Core
{
    public class SessionSwitchedEventArgs : EventArgs
    {
        public string OldSsid { get; set; }
        public string NewSsid { get; set; }
        public NetworkSession ArchivedSession { get; set; }
    }

    public class SessionHistoryManager
    {
        private readonly object _lock = new object();
        private NetworkSession _currentSession;
        private readonly ObservableCollection<NetworkSession> _historySessions;

        public event EventHandler<SessionSwitchedEventArgs> SessionSwitched;

        public NetworkSession CurrentSession
        {
            get { return _currentSession; }
        }

        public ObservableCollection<NetworkSession> HistorySessions
        {
            get { return _historySessions; }
        }

        public SessionHistoryManager()
        {
            _historySessions = new ObservableCollection<NetworkSession>();
            _currentSession = new NetworkSession();
        }

        public void InitializeFirstSession(string ssid, string interfaceName)
        {
            lock (_lock)
            {
                _currentSession.Ssid = !string.IsNullOrEmpty(ssid) ? ssid : "当前 Wi-Fi / 热点";
                _currentSession.InterfaceName = interfaceName;
                _currentSession.StartTime = DateTime.Now;
                _currentSession.IsActive = true;
            }
        }

        public bool CheckNetworkSwitch(string newSsid, string interfaceName, IEnumerable<ProcessTrafficInfo> activeProcesses)
        {
            if (string.IsNullOrEmpty(newSsid)) return false;

            NetworkSession oldSession = null;
            bool switched = false;

            lock (_lock)
            {
                if (_currentSession == null)
                {
                    _currentSession = new NetworkSession();
                    _currentSession.Ssid = newSsid;
                    _currentSession.InterfaceName = interfaceName;
                    return false;
                }

                // If SSID has changed (or from disconnected to connected)
                if (!string.Equals(_currentSession.Ssid, newSsid, StringComparison.OrdinalIgnoreCase))
                {
                    switched = true;
                    oldSession = _currentSession;
                    oldSession.EndTime = DateTime.Now;
                    oldSession.IsActive = false;

                    // Archive top apps consumed in this session
                    if (activeProcesses != null)
                    {
                        long appsSum = 0;
                        foreach (var p in activeProcesses) appsSum += p.TotalBytes;
                        long totalSessionBytes = Math.Max(oldSession.TotalBytes, appsSum);

                        var appRecords = new List<AppTrafficRecord>();
                        foreach (var p in activeProcesses)
                        {
                            if (p.TotalBytes > 0)
                            {
                                double pct = totalSessionBytes > 0 ? (double)p.TotalBytes / totalSessionBytes * 100.0 : 0.0;
                                appRecords.Add(new AppTrafficRecord
                                {
                                    ProcessId = p.ProcessId,
                                    ProcessName = p.ProcessName,
                                    DisplayName = p.DisplayName,
                                    ExecutablePath = p.ExecutablePath,
                                    TotalDownloadedBytes = p.TotalDownloadedBytes,
                                    TotalUploadedBytes = p.TotalUploadedBytes,
                                    UsagePercentage = pct
                                });
                            }
                        }
                        oldSession.AppRecords = appRecords.OrderByDescending(a => a.TotalBytes).ToList();
                    }

                    // Prepend to history
                    _historySessions.Insert(0, oldSession);

                    // Create fresh session for the new network
                    _currentSession = new NetworkSession();
                    _currentSession.Ssid = newSsid;
                    _currentSession.InterfaceName = interfaceName;
                    _currentSession.StartTime = DateTime.Now;
                    _currentSession.IsActive = true;

                    // Prune old history (> 1 hour)
                    PruneOldHistoryInternal();
                }
            }

            if (switched && SessionSwitched != null)
            {
                SessionSwitched(this, new SessionSwitchedEventArgs
                {
                    OldSsid = oldSession.Ssid,
                    NewSsid = newSsid,
                    ArchivedSession = oldSession
                });
            }

            return switched;
        }

        public void ManualResetSession(IEnumerable<ProcessTrafficInfo> activeProcesses)
        {
            lock (_lock)
            {
                if (_currentSession != null)
                {
                    _currentSession.EndTime = DateTime.Now;
                    _currentSession.IsActive = false;

                    if (activeProcesses != null)
                    {
                        long appsSum = 0;
                        foreach (var p in activeProcesses) appsSum += p.TotalBytes;
                        long totalSessionBytes = Math.Max(_currentSession.TotalBytes, appsSum);

                        var appRecords = new List<AppTrafficRecord>();
                        foreach (var p in activeProcesses)
                        {
                            if (p.TotalBytes > 0)
                            {
                                double pct = totalSessionBytes > 0 ? (double)p.TotalBytes / totalSessionBytes * 100.0 : 0.0;
                                appRecords.Add(new AppTrafficRecord
                                {
                                    ProcessId = p.ProcessId,
                                    ProcessName = p.ProcessName,
                                    DisplayName = p.DisplayName,
                                    ExecutablePath = p.ExecutablePath,
                                    TotalDownloadedBytes = p.TotalDownloadedBytes,
                                    TotalUploadedBytes = p.TotalUploadedBytes,
                                    UsagePercentage = pct
                                });
                            }
                        }
                        _currentSession.AppRecords = appRecords.OrderByDescending(a => a.TotalBytes).ToList();
                    }

                    _historySessions.Insert(0, _currentSession);

                    string currentSsid = _currentSession.Ssid;
                    string currentIf = _currentSession.InterfaceName;

                    _currentSession = new NetworkSession();
                    _currentSession.Ssid = currentSsid;
                    _currentSession.InterfaceName = currentIf;
                    _currentSession.StartTime = DateTime.Now;
                    _currentSession.IsActive = true;

                    PruneOldHistoryInternal();
                }
            }
        }

        public void PruneOldHistory()
        {
            lock (_lock)
            {
                PruneOldHistoryInternal();
            }
        }

        private void PruneOldHistoryInternal()
        {
            DateTime cutoff = DateTime.Now.AddHours(-1);
            for (int i = _historySessions.Count - 1; i >= 0; i--)
            {
                var s = _historySessions[i];
                if (s.EndTime.HasValue && s.EndTime.Value < cutoff)
                {
                    _historySessions.RemoveAt(i);
                }
            }
        }
    }
}
