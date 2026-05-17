using System.Globalization;
using System.Windows;
using System.Windows.Media;
using GymAdminPanel.Converters;
using GymAdminPanel.Models;

namespace GymAdminPanel.Tests.Converters;

public class ConverterTests
{
    private static readonly CultureInfo Culture = CultureInfo.InvariantCulture;

    [Theory]
    [InlineData(true, Visibility.Visible)]
    [InlineData(false, Visibility.Collapsed)]
    public void BoolToVisibilityConverter_ConvertsBooleanToVisibility(bool value, Visibility expected)
    {
        var converter = new BoolToVisibilityConverter();

        var result = converter.Convert(value, typeof(Visibility), string.Empty, Culture);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void InverseBoolConverter_InvertsBoolean(bool value, bool expected)
    {
        var converter = new InverseBoolConverter();

        var result = converter.Convert(value, typeof(bool), string.Empty, Culture);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(0, Visibility.Collapsed)]
    [InlineData(3, Visibility.Visible)]
    public void IntToVisibilityConverter_ShowsOnlyPositiveCounts(int value, Visibility expected)
    {
        var converter = new IntToVisibilityConverter();

        var result = converter.Convert(value, typeof(Visibility), string.Empty, Culture);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("ROLE_ADMIN", 39, 174, 96)]
    [InlineData("ROLE_TRAINER", 142, 68, 173)]
    [InlineData("ROLE_USER", 41, 128, 185)]
    [InlineData("UNKNOWN", 127, 140, 141)]
    public void RoleToColorConverter_ReturnsExpectedBrush(string role, byte red, byte green, byte blue)
    {
        var converter = new RoleToColorConverter();

        var result = Assert.IsType<SolidColorBrush>(
            converter.Convert(role, typeof(SolidColorBrush), string.Empty, Culture));

        Assert.Equal(Color.FromRgb(red, green, blue), result.Color);
    }

    [Theory]
    [InlineData(false, "Zajęcia grupowe", 41, 128, 185)]
    [InlineData(true, "Trening osobisty", 142, 68, 173)]
    public void BoolToTypeColorConverter_ReturnsExpectedBrush(bool value, string _, byte red, byte green, byte blue)
    {
        var converter = new BoolToTypeColorConverter();

        var result = Assert.IsType<SolidColorBrush>(
            converter.Convert(value, typeof(SolidColorBrush), string.Empty, Culture));

        Assert.Equal(Color.FromRgb(red, green, blue), result.Color);
    }

    [Theory]
    [InlineData(AppStatusKind.Success, "Border", 39, 174, 96)]
    [InlineData(AppStatusKind.Warning, "Border", 243, 156, 18)]
    [InlineData(AppStatusKind.Error, "Border", 231, 76, 60)]
    [InlineData(AppStatusKind.Info, "Border", 41, 128, 185)]
    public void StatusKindToBrushConverter_ReturnsExpectedBorderBrush(
        AppStatusKind kind,
        string mode,
        byte red,
        byte green,
        byte blue)
    {
        var converter = new StatusKindToBrushConverter();

        var result = Assert.IsType<SolidColorBrush>(
            converter.Convert(kind, typeof(SolidColorBrush), mode, Culture));

        Assert.Equal(Color.FromRgb(red, green, blue), result.Color);
    }

    [Theory]
    [InlineData("DELETE_USER", 231, 76, 60)]
    [InlineData("CREATE_CLASS", 39, 174, 96)]
    [InlineData("UPDATE_ROLE", 243, 156, 18)]
    [InlineData("LOGIN", 41, 128, 185)]
    [InlineData("OTHER", 127, 140, 141)]
    public void ActionToColorConverter_ReturnsExpectedBrush(string action, byte red, byte green, byte blue)
    {
        var converter = new ActionToColorConverter();

        var result = Assert.IsType<SolidColorBrush>(
            converter.Convert(action, typeof(SolidColorBrush), string.Empty, Culture));

        Assert.Equal(Color.FromRgb(red, green, blue), result.Color);
    }
}
