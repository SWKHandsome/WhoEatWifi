using System;

namespace WhoEatWifi.Models
{
    public class AppTrafficRecord
    {
        public int ProcessId { get; set; }
        public string ProcessName { get; set; }
        public string DisplayName { get; set; }
        public string ExecutablePath { get; set; }
        public long TotalDownloadedBytes { get; set; }
        public long TotalUploadedBytes { get; set; }
        public double UsagePercentage { get; set; }

        public string FormattedUsagePercentage
        {
            get
            {
                if (UsagePercentage <= 0.0) return "0.0%";
                return UsagePercentage.ToString("F1") + "%";
            }
        }

        public long TotalBytes
        {
            get { return TotalDownloadedBytes + TotalUploadedBytes; }
        }

        public string FormattedTotal
        {
            get { return FormatBytes(TotalBytes); }
        }

        public string FormattedDownloaded
        {
            get { return FormatBytes(TotalDownloadedBytes); }
        }

        public string FormattedUploaded
        {
            get { return FormatBytes(TotalUploadedBytes); }
        }

        public AppTrafficRecord()
        {
            ProcessName = "Unknown";
            DisplayName = "Unknown";
            ExecutablePath = string.Empty;
        }

        public static string FormatBytes(long bytes)
        {
            if (bytes <= 0) return "0 B";
            if (bytes < 1024) return bytes + " B";
            double kb = bytes / 1024.0;
            if (kb < 1024) return kb.ToString("F1") + " KB";
            double mb = kb / 1024.0;
            if (mb < 1024) return mb.ToString("F1") + " MB";
            double gb = mb / 1024.0;
            return gb.ToString("F2") + " GB";
        }
    }
}
