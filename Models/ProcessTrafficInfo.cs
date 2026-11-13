using System;
using System.ComponentModel;
using System.Windows.Media;

namespace WhoEatWifi.Models
{
    public class ProcessTrafficInfo : INotifyPropertyChanged
    {
        private int _processId;
        private string _processName;
        private string _displayName;
        private string _executablePath;
        private ImageSource _icon;
        private long _downloadSpeedBps;
        private long _uploadSpeedBps;
        private long _totalDownloadedBytes;
        private long _totalUploadedBytes;
        private double _usagePercentage;

        public event PropertyChangedEventHandler PropertyChanged;

        public int ProcessId
        {
            get { return _processId; }
            set { _processId = value; OnPropertyChanged("ProcessId"); }
        }

        public string ProcessName
        {
            get { return _processName; }
            set { _processName = value; OnPropertyChanged("ProcessName"); }
        }

        public string DisplayName
        {
            get { return _displayName; }
            set { _displayName = value; OnPropertyChanged("DisplayName"); }
        }

        public string ExecutablePath
        {
            get { return _executablePath; }
            set { _executablePath = value; OnPropertyChanged("ExecutablePath"); }
        }

        public ImageSource Icon
        {
            get { return _icon; }
            set { _icon = value; OnPropertyChanged("Icon"); }
        }

        public long DownloadSpeedBps
        {
            get { return _downloadSpeedBps; }
            set
            {
                if (_downloadSpeedBps != value)
                {
                    _downloadSpeedBps = value;
                    OnPropertyChanged("DownloadSpeedBps");
                    OnPropertyChanged("FormattedDownloadSpeed");
                    OnPropertyChanged("IsHeavyUser");
                }
            }
        }

        public long UploadSpeedBps
        {
            get { return _uploadSpeedBps; }
            set
            {
                if (_uploadSpeedBps != value)
                {
                    _uploadSpeedBps = value;
                    OnPropertyChanged("UploadSpeedBps");
                    OnPropertyChanged("FormattedUploadSpeed");
                }
            }
        }

        public long TotalDownloadedBytes
        {
            get { return _totalDownloadedBytes; }
            set
            {
                if (_totalDownloadedBytes != value)
                {
                    _totalDownloadedBytes = value;
                    OnPropertyChanged("TotalDownloadedBytes");
                    OnPropertyChanged("FormattedTotalDownloaded");
                    OnPropertyChanged("TotalBytes");
                    OnPropertyChanged("FormattedTotalBytes");
                }
            }
        }

        public long TotalUploadedBytes
        {
            get { return _totalUploadedBytes; }
            set
            {
                if (_totalUploadedBytes != value)
                {
                    _totalUploadedBytes = value;
                    OnPropertyChanged("TotalUploadedBytes");
                    OnPropertyChanged("FormattedTotalUploaded");
                    OnPropertyChanged("TotalBytes");
                    OnPropertyChanged("FormattedTotalBytes");
                }
            }
        }

        public long TotalBytes
        {
            get { return _totalDownloadedBytes + _totalUploadedBytes; }
        }

        public double UsagePercentage
        {
            get { return _usagePercentage; }
            set
            {
                if (Math.Abs(_usagePercentage - value) > 0.01)
                {
                    _usagePercentage = value;
                    OnPropertyChanged("UsagePercentage");
                    OnPropertyChanged("FormattedUsagePercentage");
                    OnPropertyChanged("UsagePercentageInt");
                }
            }
        }

        public int UsagePercentageInt
        {
            get { return (int)Math.Round(_usagePercentage); }
        }

        public string FormattedUsagePercentage
        {
            get
            {
                if (_usagePercentage <= 0.0) return "0.0%";
                return _usagePercentage.ToString("F1") + "%";
            }
        }

        public bool IsHeavyUser
        {
            get { return _downloadSpeedBps > 102400 || TotalBytes > 104857600; } // >100KB/s or >100MB
        }

        public string FormattedDownloadSpeed
        {
            get { return FormatSpeed(_downloadSpeedBps); }
        }

        public string FormattedUploadSpeed
        {
            get { return FormatSpeed(_uploadSpeedBps); }
        }

        public string FormattedTotalDownloaded
        {
            get { return AppTrafficRecord.FormatBytes(_totalDownloadedBytes); }
        }

        public string FormattedTotalUploaded
        {
            get { return AppTrafficRecord.FormatBytes(_totalUploadedBytes); }
        }

        public string FormattedTotalBytes
        {
            get { return AppTrafficRecord.FormatBytes(TotalBytes); }
        }

        public string KillButtonText
        {
            get { return WhoEatWifi.Core.LanguageManager.T("BtnKill"); }
        }

        public string LocateButtonText
        {
            get { return WhoEatWifi.Core.LanguageManager.T("BtnLocate"); }
        }

        public void RefreshLanguage()
        {
            OnPropertyChanged("KillButtonText");
            OnPropertyChanged("LocateButtonText");
        }

        public ProcessTrafficInfo()
        {
            _processName = "Unknown";
            _displayName = "Unknown";
            _executablePath = string.Empty;
        }

        public static string FormatSpeed(long bytesPerSec)
        {
            if (bytesPerSec <= 0) return "0 B/s";
            if (bytesPerSec < 1024) return bytesPerSec + " B/s";
            double kb = bytesPerSec / 1024.0;
            if (kb < 1024) return kb.ToString("F1") + " KB/s";
            double mb = kb / 1024.0;
            if (mb < 1024) return mb.ToString("F1") + " MB/s";
            double gb = mb / 1024.0;
            return gb.ToString("F2") + " GB/s";
        }

        private void OnPropertyChanged(string propName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propName));
            }
        }
    }
}
