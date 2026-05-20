using StudentAppWPF.Data;
using StudentAppWPF.Models;
using System.Windows;

namespace StudentAppWPF
{
    public partial class AdminWindow : Window
    {
        private readonly UserRepository _repo;
        private readonly string _currentLogin;

        public AdminWindow(string currentLogin)
        {
            InitializeComponent();
            _repo = new UserRepository();
            _currentLogin = currentLogin;
            LoadUsers();
        }

        private void LoadUsers()
        {
            UsersGrid.ItemsSource = _repo.GetAllUsers();
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            string login = NewLoginBox.Text.Trim();
            string password = NewPasswordBox.Password;

            if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Заполните логин и пароль!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                _repo.AddUser(login, password);
                NewLoginBox.Text = "";
                NewPasswordBox.Password = "";
                LoadUsers();
            }
            catch
            {
                MessageBox.Show("Пользователь с таким логином уже существует!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteUser_Click(object sender, RoutedEventArgs e)
        {
            var user = (sender as FrameworkElement)?.DataContext as UserModel;
            if (user == null) return;

            if (user.Login == _currentLogin)
            {
                MessageBox.Show("Нельзя удалить самого себя!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show($"Удалить администратора {user.Login}?", "Подтверждение",
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                _repo.DeleteUser(user.UserId);
                LoadUsers();
            }
        }
    }
}
