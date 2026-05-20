using System;
using System.Windows;
using System.Windows.Controls;
using StudentAppWPF.Data;
using StudentAppWPF.Models;
using System.Configuration;
using System.Data.SqlClient;

namespace StudentAppWPF
{
    public partial class MainWindow : Window
    {
        private StudentRepository _repo;
        private Student _editingStudent;
        private readonly string _currentLogin;

        public MainWindow(string currentLogin = "")
        {
            InitializeComponent();
            _currentLogin = currentLogin;
            _repo = new StudentRepository();
            LoadData();
        }

        private void LoadData()
        {
            string search = string.IsNullOrWhiteSpace(SearchBox.Text) ? null : SearchBox.Text;
            int? course = null;
            if (CourseFilterBox.SelectedIndex > 0)
            {
                course = CourseFilterBox.SelectedIndex; // 1,2,3,4
            }
            var students = _repo.GetStudentsFiltered(search, course, null);
            StudentsGrid.ItemsSource = students;
            StatusLabel.Content = $"Найдено: {students.Count} студентов";
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e) => LoadData();

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            SearchBox.Text = "";
            CourseFilterBox.SelectedIndex = 0;
            LoadData();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(FullNameBox.Text) || string.IsNullOrWhiteSpace(GroupNameBox.Text))
            {
                MessageBox.Show("Заполните ФИО и группу!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var student = new Student
            {
                FullName = FullNameBox.Text.Trim(),
                GroupName = GroupNameBox.Text.Trim(),
                StudentCardNumber = CardNumberBox.Text.Trim(),
                Phone = PhoneBox.Text.Trim(),
                Email = EmailBox.Text.Trim(),
                IsStudying = true
            };

            if (_editingStudent == null)
            {
                _repo.AddStudent(student);
                StatusLabel.Content = "✅ Студент добавлен";
            }
            else
            {
                student.StudentId = _editingStudent.StudentId;
                _repo.UpdateStudent(student);
                StatusLabel.Content = "✏️ Студент обновлён";
                _editingStudent = null;
                CancelButton.IsEnabled = false;
                FormTitle.Text = "➕ Добавить студента";
            }

            ClearForm();
            LoadData();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            ClearForm();
            _editingStudent = null;
            CancelButton.IsEnabled = false;
            FormTitle.Text = "➕ Добавить студента";
            StatusLabel.Content = "Редактирование отменено";
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            var student = (sender as FrameworkElement)?.DataContext as Student;
            if (student != null)
            {
                _editingStudent = student;
                FullNameBox.Text = student.FullName;
                GroupNameBox.Text = student.GroupName;
                CardNumberBox.Text = student.StudentCardNumber;
                PhoneBox.Text = student.Phone;
                EmailBox.Text = student.Email;
                FormTitle.Text = "✏️ Редактировать студента";
                CancelButton.IsEnabled = true;
                StatusLabel.Content = $"Редактирование: {student.FullName}";
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var student = (sender as FrameworkElement)?.DataContext as Student;
            if (student != null)
            {
                var result = MessageBox.Show($"Удалить студента {student.FullName}?", "Подтверждение",
                    MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    _repo.DeleteStudent(student.StudentId, true);
                    if (_editingStudent?.StudentId == student.StudentId) ClearForm();
                    LoadData();
                    StatusLabel.Content = $"🗑️ Удалён: {student.FullName}";
                }
            }
        }

        private void AdminButton_Click(object sender, RoutedEventArgs e)
        {
            var adminWindow = new AdminWindow(_currentLogin);
            adminWindow.ShowDialog();
        }

        private void StudentsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Можно оставить пустым
        }

        private void ClearForm()
        {
            FullNameBox.Text = "";
            GroupNameBox.Text = "";
            CardNumberBox.Text = "";
            PhoneBox.Text = "";
            EmailBox.Text = "";
            _editingStudent = null;
            CancelButton.IsEnabled = false;
            FormTitle.Text = "➕ Добавить студента";
        }
    }
}