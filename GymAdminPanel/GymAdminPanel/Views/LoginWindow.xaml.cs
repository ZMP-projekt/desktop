using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using GymAdminPanel.ViewModels;

namespace GymAdminPanel.Views
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();

            DataContext = new LoginViewModel();
        }

        private void PasswordInput_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is LoginViewModel viewModel && sender is PasswordBox passwordBox)
            {
                viewModel.PasswordLength = passwordBox.SecurePassword.Length;
            }
        }

        private void PasswordInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter || DataContext is not LoginViewModel viewModel)
                return;

            if (viewModel.LoginCommand.CanExecute(PasswordInput))
            {
                viewModel.LoginCommand.Execute(PasswordInput);
                e.Handled = true;
            }
        }
    }
}
