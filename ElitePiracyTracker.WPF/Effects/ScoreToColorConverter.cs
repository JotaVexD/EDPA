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
                if (score >= 85) return new SolidColorBrush(Color.FromRgb(76, 175, 80)); 
                if (score >= 75) return new SolidColorBrush(Color.FromRgb(175, 172, 76)); 
                if (score >= 60) return new SolidColorBrush(Color.FromRgb(255, 152, 0)); 
                if (score >= 45) return new SolidColorBrush(Color.FromRgb(255, 125, 90)); 

                return new SolidColorBrush(Color.FromRgb(244, 67, 54));                  
            }
            return new SolidColorBrush(Colors.Gray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
