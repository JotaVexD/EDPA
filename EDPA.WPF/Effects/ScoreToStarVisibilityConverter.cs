using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace EDPA.WPF.Effects
{
    public class ScoreToStarVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double score && parameter is string paramStr && int.TryParse(paramStr, out int starLevel))
            {
                double threshold = (starLevel - 1) * 20;
                return score >= threshold ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}