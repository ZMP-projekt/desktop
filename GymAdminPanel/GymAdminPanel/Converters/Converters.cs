using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace GymAdminPanel.Converters;

public class RoleToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value?.ToString() switch
        {
            "ROLE_ADMIN" => new SolidColorBrush(Color.FromRgb(39, 174, 96)),   // zielony
            "ROLE_TRAINER" => new SolidColorBrush(Color.FromRgb(142, 68, 173)),  // fioletowy
            "ROLE_USER" => new SolidColorBrush(Color.FromRgb(41, 128, 185)),  // niebieski
            _ => new SolidColorBrush(Color.FromRgb(127, 140, 141)), // szary
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => DependencyProperty.UnsetValue;
}
public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value is Visibility.Visible;
}
