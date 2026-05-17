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
                viewModel.Password = passwordBox.Password;
            }
        }

        private void PasswordInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter || DataContext is not LoginViewModel viewModel)
                return;

            if (viewModel.LoginCommand.CanExecute(this))
            {
                viewModel.LoginCommand.Execute(this);
                e.Handled = true;
            }
        }
    }
}
