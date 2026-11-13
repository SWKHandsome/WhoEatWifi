using System;
using System.Windows.Media;

namespace WhoEatWifi.Models
{
    public class TrafficSegment
    {
        public string Name { get; set; }
        public double Percentage { get; set; }
        public long Bytes { get; set; }
        public Brush ColorBrush { get; set; }
        public string FormattedPercentage { get; set; }
        public string FormattedBytes { get; set; }

        public TrafficSegment()
        {
            Name = string.Empty;
            FormattedPercentage = "0.0%";
            FormattedBytes = "0 B";
        }
    }
}
