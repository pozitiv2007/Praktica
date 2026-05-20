using System.Data;
using System.Configuration;
using System.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;
using System.Collections.ObjectModel;
using StudentAppWPF.Models;

namespace StudentAppWPF.Data
{
    public class UserRepository
    {
        private readonly string _connectionString;

        public UserRepository()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["StudentDB"].ConnectionString;
            EnsureDefaultUser();
        }

        private void EnsureDefaultUser()
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                // Проверяем, есть ли таблица Users
                var checkTable = connection.GetSchema("Tables").Select("TABLE_NAME = 'Users'");
                if (checkTable.Length == 0)
                {
                    using (var cmd = new SqlCommand(
                        "CREATE TABLE Users (UserId INT IDENTITY(1,1) PRIMARY KEY, Login NVARCHAR(50) NOT NULL UNIQUE, PasswordHash NVARCHAR(256) NOT NULL)", connection))
                    {
                        cmd.ExecuteNonQuery();
                    }
                }

                // Добавляем или обновляем root
                using (var cmd = new SqlCommand(
                    "IF NOT EXISTS (SELECT 1 FROM Users WHERE Login = 'root') " +
                    "INSERT INTO Users (Login, PasswordHash) VALUES ('root', '96cae35ce8a9b0244178bf28e4966c2ce1b8385723a96a6b838858cdd6ca0a1e') " +
                    "ELSE UPDATE Users SET PasswordHash = '96cae35ce8a9b0244178bf28e4966c2ce1b8385723a96a6b838858cdd6ca0a1e' WHERE Login = 'root'", connection))
                {
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public bool Validate(string login, string password)
        {
            string hash = HashPassword(password);

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                string query = "SELECT COUNT(1) FROM Users WHERE Login = @Login AND PasswordHash = @Hash";
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Login", login);
                    command.Parameters.AddWithValue("@Hash", hash);
                    return (int)command.ExecuteScalar() > 0;
                }
            }
        }

        public ObservableCollection<UserModel> GetAllUsers()
        {
            var users = new ObservableCollection<UserModel>();
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                string query = "SELECT UserId, Login FROM Users ORDER BY Login";
                using (var command = new SqlCommand(query, connection))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        users.Add(new UserModel
                        {
                            UserId = reader.GetInt32(0),
                            Login = reader.GetString(1)
                        });
                    }
                }
            }
            return users;
        }

        public void AddUser(string login, string password)
        {
            string hash = HashPassword(password);
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var cmd = new SqlCommand(
                    "INSERT INTO Users (Login, PasswordHash) VALUES (@Login, @Hash)", connection))
                {
                    cmd.Parameters.AddWithValue("@Login", login);
                    cmd.Parameters.AddWithValue("@Hash", hash);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void DeleteUser(int userId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var cmd = new SqlCommand("DELETE FROM Users WHERE UserId = @Id", connection))
                {
                    cmd.Parameters.AddWithValue("@Id", userId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private static string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                var sb = new StringBuilder();
                foreach (byte b in bytes)
                    sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }
    }
}
