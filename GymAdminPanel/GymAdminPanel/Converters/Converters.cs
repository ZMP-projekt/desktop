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
            "ROLE_ADMIN" => new SolidColorBrush(Color.FromRgb(39, 174, 96)),
            "ROLE_TRAINER" => new SolidColorBrush(Color.FromRgb(142, 68, 173)),
            "ROLE_USER" => new SolidColorBrush(Color.FromRgb(41, 128, 185)),
            _ => new SolidColorBrush(Color.FromRgb(127, 140, 141)),
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

// IsFull: true = czerwony, false = zielony
public class BoolToRedGreenConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true
            ? new SolidColorBrush(Color.FromRgb(231, 76, 60))
            : new SolidColorBrush(Color.FromRgb(39, 174, 96));
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => DependencyProperty.UnsetValue;
}

// PersonalTraining: true = fioletowy, false = niebieski
public class BoolToTypeColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true
            ? new SolidColorBrush(Color.FromRgb(142, 68, 173))
            : new SolidColorBrush(Color.FromRgb(41, 128, 185));
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => DependencyProperty.UnsetValue;
}
public class ActionToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var action = value?.ToString()?.ToUpperInvariant() ?? "";

        if (action.Contains("DELETE") || action.Contains("REMOVE") || action.Contains("BAN"))
            return new SolidColorBrush(Color.FromRgb(231, 76, 60));

        if (action.Contains("CREATE") || action.Contains("ADD") || action.Contains("REGISTER"))
            return new SolidColorBrush(Color.FromRgb(39, 174, 96));

        if (action.Contains("UPDATE") || action.Contains("EDIT") || action.Contains("CHANGE") || action.Contains("ROLE"))
            return new SolidColorBrush(Color.FromRgb(243, 156, 18));

        if (action.Contains("LOGIN") || action.Contains("LOGOUT"))
            return new SolidColorBrush(Color.FromRgb(41, 128, 185));

        return new SolidColorBrush(Color.FromRgb(127, 140, 141));
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => DependencyProperty.UnsetValue;
}
