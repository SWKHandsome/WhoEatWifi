using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace WhoEatWifi.UI
{
    public class SpeedToBrushConverter : IValueConverter
    {
        private static readonly SolidColorBrush NormalBrush = new SolidColorBrush(Color.FromRgb(30, 41, 59));      // Dark slate
        private static readonly SolidColorBrush ActiveBrush = new SolidColorBrush(Color.FromRgb(14, 116, 144));    // Cyan
        private static readonly SolidColorBrush WarningBrush = new SolidColorBrush(Color.FromRgb(217, 119, 6));    // Amber
        private static readonly SolidColorBrush HeavyBrush = new SolidColorBrush(Color.FromRgb(220, 38, 38));      // Red

        static SpeedToBrushConverter()
        {
            NormalBrush.Freeze();
            ActiveBrush.Freeze();
            WarningBrush.Freeze();
            HeavyBrush.Freeze();
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is long)
            {
                long speed = (long)value;
                if (speed >= 2097152) return HeavyBrush;   // >= 2 MB/s
                if (speed >= 307200) return WarningBrush;  // >= 300 KB/s
                if (speed > 0) return ActiveBrush;         // > 0
            }
            return NormalBrush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class NullToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value == null ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool val = value is bool && (bool)value;
            if (parameter != null && parameter.ToString().Equals("Inverse", StringComparison.OrdinalIgnoreCase))
            {
                val = !val;
            }
            return val ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class PercentageToBrushConverter : IValueConverter
    {
        private static readonly SolidColorBrush HeavyBrush = new SolidColorBrush(Color.FromRgb(220, 38, 38));      // Red > 50%
        private static readonly SolidColorBrush MediumBrush = new SolidColorBrush(Color.FromRgb(37, 99, 235));     // Blue > 20%
        private static readonly SolidColorBrush ModerateBrush = new SolidColorBrush(Color.FromRgb(13, 148, 136));  // Teal > 5%
        private static readonly SolidColorBrush LightBrush = new SolidColorBrush(Color.FromRgb(100, 116, 139));    // Slate <= 5%

        static PercentageToBrushConverter()
        {
            HeavyBrush.Freeze();
            MediumBrush.Freeze();
            ModerateBrush.Freeze();
            LightBrush.Freeze();
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double)
            {
                double pct = (double)value;
                if (pct >= 50.0) return HeavyBrush;
                if (pct >= 20.0) return MediumBrush;
                if (pct >= 5.0) return ModerateBrush;
            }
            return LightBrush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
