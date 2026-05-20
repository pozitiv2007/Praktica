using StudentAppWPF.Data;
using System.Windows;

namespace StudentAppWPF
{
    public partial class LoginWindow : Window
    {
        private readonly UserRepository _userRepo;

        public LoginWindow()
        {
            InitializeComponent();
            _userRepo = new UserRepository();
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string login = LoginBox.Text.Trim();
            string password = PasswordBox.Password;

            if (_userRepo.Validate(login, password))
            {
                var mainWindow = new MainWindow(login);
                mainWindow.Show();
                Close();
            }
            else
            {
                MessageBox.Show("Неверный логин или пароль!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
