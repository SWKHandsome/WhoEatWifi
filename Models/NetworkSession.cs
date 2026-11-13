using System;
using System.Collections.Generic;

namespace WhoEatWifi.Models
{
    public class NetworkSession
    {
        public string SessionId { get; set; }
        public string Ssid { get; set; }
        public string InterfaceName { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public long TotalDownloadedBytes { get; set; }
        public long TotalUploadedBytes { get; set; }
        public bool IsActive { get; set; }
        public List<AppTrafficRecord> AppRecords { get; set; }

        public NetworkSession()
        {
            SessionId = Guid.NewGuid().ToString();
            Ssid = "未知 Wi-Fi";
            InterfaceName = "Wi-Fi";
            StartTime = DateTime.Now;
            EndTime = null;
            IsActive = true;
            AppRecords = new List<AppTrafficRecord>();
        }

        public long TotalBytes
        {
            get { return TotalDownloadedBytes + TotalUploadedBytes; }
        }

        public TimeSpan Duration
        {
            get
            {
                DateTime end = EndTime.HasValue ? EndTime.Value : DateTime.Now;
                return end - StartTime;
            }
        }

        public string FormattedDuration
        {
            get
            {
                TimeSpan d = Duration;
                bool isEn = WhoEatWifi.Core.LanguageManager.CurrentLanguage == WhoEatWifi.Core.AppLanguage.English;
                if (d.TotalHours >= 1)
                {
                    return isEn ?
                        string.Format("{0}h {1}m", (int)d.TotalHours, d.Minutes) :
                        string.Format("{0} 小时 {1} 分钟", (int)d.TotalHours, d.Minutes);
                }
                if (d.TotalMinutes >= 1)
                {
                    return isEn ?
                        string.Format("{0}m {1}s", (int)d.TotalMinutes, d.Seconds) :
                        string.Format("{0} 分 {1} 秒", (int)d.TotalMinutes, d.Seconds);
                }
                return isEn ?
                    string.Format("{0}s", (int)d.TotalSeconds) :
                    string.Format("{0} 秒", (int)d.TotalSeconds);
            }
        }

        public string FormattedTimeRange
        {
            get
            {
                bool isEn = WhoEatWifi.Core.LanguageManager.CurrentLanguage == WhoEatWifi.Core.AppLanguage.English;
                if (EndTime.HasValue)
                {
                    return string.Format("{0:HH:mm:ss} - {1:HH:mm:ss}", StartTime, EndTime.Value);
                }
                return isEn ?
                    string.Format("{0:HH:mm:ss} - Active...", StartTime) :
                    string.Format("{0:HH:mm:ss} - 进行中...", StartTime);
            }
        }

        public string FormattedTotal
        {
            get { return AppTrafficRecord.FormatBytes(TotalBytes); }
        }

        public string FormattedDownloaded
        {
            get { return AppTrafficRecord.FormatBytes(TotalDownloadedBytes); }
        }

        public string FormattedUploaded
        {
            get { return AppTrafficRecord.FormatBytes(TotalUploadedBytes); }
        }
    }
}
