using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using GymAdminPanel.Models;

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

public class BoolToRedGreenConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true
            ? new SolidColorBrush(Color.FromRgb(231, 76, 60))
            : new SolidColorBrush(Color.FromRgb(39, 174, 96));
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => DependencyProperty.UnsetValue;
}

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

public class IntToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is int count && count > 0 ? Visibility.Visible : Visibility.Collapsed;
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => DependencyProperty.UnsetValue;
}

public class StatusKindToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var kind = value is AppStatusKind statusKind ? statusKind : AppStatusKind.Info;
        var mode = parameter?.ToString();

        return (kind, mode) switch
        {
            (AppStatusKind.Success, "Border") => new SolidColorBrush(Color.FromRgb(39, 174, 96)),
            (AppStatusKind.Warning, "Border") => new SolidColorBrush(Color.FromRgb(243, 156, 18)),
            (AppStatusKind.Error, "Border") => new SolidColorBrush(Color.FromRgb(231, 76, 60)),
            (_, "Border") => new SolidColorBrush(Color.FromRgb(41, 128, 185)),

            (AppStatusKind.Success, "Text") => new SolidColorBrush(Color.FromRgb(21, 87, 36)),
            (AppStatusKind.Warning, "Text") => new SolidColorBrush(Color.FromRgb(133, 100, 4)),
            (AppStatusKind.Error, "Text") => new SolidColorBrush(Color.FromRgb(114, 28, 36)),
            (_, "Text") => new SolidColorBrush(Color.FromRgb(12, 84, 96)),

            (AppStatusKind.Success, _) => new SolidColorBrush(Color.FromRgb(212, 237, 218)),
            (AppStatusKind.Warning, _) => new SolidColorBrush(Color.FromRgb(255, 243, 205)),
            (AppStatusKind.Error, _) => new SolidColorBrush(Color.FromRgb(248, 215, 218)),
            _ => new SolidColorBrush(Color.FromRgb(209, 236, 241)),
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => DependencyProperty.UnsetValue;
}
