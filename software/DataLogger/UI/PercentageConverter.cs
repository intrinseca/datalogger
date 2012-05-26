using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Windows;
using System.ComponentModel;

namespace DataLogger
{
    public class PercentageConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (Application.Current == null || Application.Current.GetType() != typeof(App))
            {
                return 50;
            }
            double max = System.Convert.ToDouble(values[0]);
            double frac = System.Convert.ToDouble(values[1]);

            if (frac == 0)
                return 1;

            return max * frac;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
