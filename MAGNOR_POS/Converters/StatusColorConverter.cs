using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace MAGNOR_POS.Converters;

public class StatusColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isActive)
        {
            return isActive
                ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981")) // Green
                : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444")); // Red
        }
        return new SolidColorBrush(Colors.Gray);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
