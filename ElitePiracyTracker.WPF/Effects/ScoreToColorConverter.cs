using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace ElitePiracyTracker.WPF.Effects
{
    public class ScoreToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double score)
            {
                // Use Fluent UI colors for consistency
                if (score >= 90) return new SolidColorBrush(Color.FromRgb(16, 124, 16));    // Green
                if (score >= 80) return new SolidColorBrush(Color.FromRgb(0, 99, 177));     // Blue
                if (score >= 70) return new SolidColorBrush(Color.FromRgb(202, 133, 0));    // Yellow/Orange
                return new SolidColorBrush(Color.FromRgb(196, 43, 28));                     // Red
            }
            return new SolidColorBrush(Colors.Gray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
