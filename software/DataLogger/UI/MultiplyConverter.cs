using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;

namespace DataLogger
{
    /// <summary>
    /// Value converter that multiplies the argument by the converter parameter
    /// </summary>
    class MultiplyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            double dParam = (double)parameter;

            //Different calculations for different argument types, not sure why a simple case to (double) doesn't work
            if (value.GetType() == typeof(int))
                return (int)value * dParam;
            if (value.GetType() == typeof(float))
                return (float)value * dParam;
            else
                return (double)value * dParam;


        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
