using EDPA.Models;
using EDPA.Services;
using Microsoft.Extensions.Configuration;
using System;
using System.Configuration;
using System.Globalization;
using System.Net.Http;
using System.Windows.Data;

namespace EDPA.WPF.Config
{
    public class WeightedScoreToPercentageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double score && parameter is string scoreType)
            {
                // Get the weight for this score type
                double weight = scoreType switch
                {
                    "NoRings" => 0.10,
                    "Security" => 0.25,
                    "FactionState" => 0.05,
                    "MarketDemand" => 0.10,
                    _ => 1.0
                };

                // Calculate the percentage of the maximum possible score for this category
                // The score is already weighted, so we need to calculate what percentage it is of the maximum weight
                double percentage = score / weight * 100;

                // Ensure the percentage is between 0 and 100
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