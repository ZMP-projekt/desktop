using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using MahApps.Metro.IconPacks;

namespace GymAdminPanel.Views;

public partial class ConfirmDialog : Window
{
    public ConfirmDialog(
        string dialogTitle,
        string message,
        string details,
        ConfirmDialogKind kind)
    {
        InitializeComponent();

        DialogTitle = dialogTitle;
        Message = message;
        Details = details;

        (IconKind, AccentBrush, AccentBackground) = kind switch
        {
            ConfirmDialogKind.Warning => (
                PackIconMaterialKind.AlertOutline,
                new SolidColorBrush(Color.FromRgb(255, 174, 80)),
                new SolidColorBrush(Color.FromRgb(58, 42, 30))),
            _ => (
                PackIconMaterialKind.HelpCircleOutline,
                new SolidColorBrush(Color.FromRgb(191, 208, 255)),
                new SolidColorBrush(Color.FromRgb(38, 47, 68)))
        };

        DataContext = this;
    }

    public string DialogTitle { get; }
    public string Message { get; }
    public string Details { get; }
    public PackIconMaterialKind IconKind { get; }
    public Brush AccentBrush { get; }
    public Brush AccentBackground { get; }

    public static bool Show(
        string title,
        string message,
        string details,
        ConfirmDialogKind kind = ConfirmDialogKind.Question)
    {
        var dialog = new ConfirmDialog(title, message, details, kind)
        {
            Owner = Application.Current.Windows
                .OfType<Window>()
                .FirstOrDefault(window => window.IsActive)
        };

        return dialog.ShowDialog() == true;
    }

    private void Confirm_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

    private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
            DragMove();
    }
}

public enum ConfirmDialogKind
{
    Question,
    Warning
}
