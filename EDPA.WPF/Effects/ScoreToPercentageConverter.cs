using System;
using System.Globalization;
using System.Windows.Data;
using EDPA.WPF.Services;

namespace EDPA.WPF.Effects
{
    public class ScoreToPercentageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double score && parameter is string weightKey)
            {
                double weight = ScoringWeightsProvider.GetWeight(weightKey);
                double percentage = score / weight * 100;
                return Math.Min(Math.Max(percentage, 0), 100);
            }
            return 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}