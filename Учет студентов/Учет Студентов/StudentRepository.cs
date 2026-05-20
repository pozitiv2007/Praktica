using System;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using StudentAppWPF.Models;

namespace StudentAppWPF.Data
{
    public class StudentRepository
    {
        private readonly string _connectionString;

        public StudentRepository()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["StudentDB"].ConnectionString;
        }

        public ObservableCollection<Student> GetAllStudents()
        {
            var students = new ObservableCollection<Student>();
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                string query = "SELECT * FROM vw_StudentDetails ORDER BY FullName";
                using (var command = new SqlCommand(query, connection))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        students.Add(MapToStudent(reader));
                    }
                }
            }
            return students;
        }

        public ObservableCollection<Student> GetStudentsFiltered(string searchText, int? course, int? groupId)
        {
            var students = new ObservableCollection<Student>();
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var command = new SqlCommand("sp_GetStudentsFiltered", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    command.Parameters.AddWithValue("@SearchText", (object)searchText ?? DBNull.Value);
                    command.Parameters.AddWithValue("@Course", (object)course ?? DBNull.Value);
                    command.Parameters.AddWithValue("@GroupId", (object)groupId ?? DBNull.Value);

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            students.Add(MapToStudent(reader));
                        }
                    }
                }
            }
            return students;
        }

        public int AddStudent(Student student)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var command = new SqlCommand("sp_AddStudent", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    var parts = student.FullName.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    string lastName = parts.Length > 0 ? parts[0] : "";
                    string firstName = parts.Length > 1 ? parts[1] : "";
                    string middleName = parts.Length > 2 ? parts[2] : null;

                    command.Parameters.AddWithValue("@LastName", lastName);
                    command.Parameters.AddWithValue("@FirstName", firstName);
                    command.Parameters.AddWithValue("@MiddleName", (object)middleName ?? DBNull.Value);
                    command.Parameters.AddWithValue("@GroupName", student.GroupName);
                    command.Parameters.AddWithValue("@StudentCardNumber", student.StudentCardNumber);
                    command.Parameters.AddWithValue("@BirthDate", DBNull.Value);
                    command.Parameters.AddWithValue("@Phone", (object)student.Phone ?? DBNull.Value);
                    command.Parameters.AddWithValue("@Email", (object)student.Email ?? DBNull.Value);
                    command.Parameters.AddWithValue("@Address", DBNull.Value);

                    var result = command.ExecuteScalar();
                    return Convert.ToInt32(result);
                }
            }
        }

        public void UpdateStudent(Student student)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var command = new SqlCommand("sp_UpdateStudent", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    var parts = student.FullName.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    string lastName = parts.Length > 0 ? parts[0] : "";
                    string firstName = parts.Length > 1 ? parts[1] : "";
                    string middleName = parts.Length > 2 ? parts[2] : null;

                    command.Parameters.AddWithValue("@StudentId", student.StudentId);
                    command.Parameters.AddWithValue("@LastName", lastName);
                    command.Parameters.AddWithValue("@FirstName", firstName);
                    command.Parameters.AddWithValue("@MiddleName", (object)middleName ?? DBNull.Value);
                    command.Parameters.AddWithValue("@GroupName", student.GroupName);
                    command.Parameters.AddWithValue("@StudentCardNumber", student.StudentCardNumber);
                    command.Parameters.AddWithValue("@BirthDate", DBNull.Value);
                    command.Parameters.AddWithValue("@Phone", (object)student.Phone ?? DBNull.Value);
                    command.Parameters.AddWithValue("@Email", (object)student.Email ?? DBNull.Value);
                    command.Parameters.AddWithValue("@Address", DBNull.Value);
                    command.Parameters.AddWithValue("@IsStudying", student.IsStudying);

                    command.ExecuteNonQuery();
                }
            }
        }

        public void DeleteStudent(int studentId, bool hardDelete = false)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var command = new SqlCommand("sp_DeleteStudent", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@StudentId", studentId);
                    command.Parameters.AddWithValue("@HardDelete", hardDelete);
                    command.ExecuteNonQuery();
                }
            }
        }

        private Student MapToStudent(IDataRecord reader)
        {
            return new Student
            {
                StudentId = reader.GetInt32(reader.GetOrdinal("StudentId")),
                FullName = reader.GetString(reader.GetOrdinal("FullName")),
                GroupName = reader.GetString(reader.GetOrdinal("GroupName")),
                Course = reader.GetInt32(reader.GetOrdinal("Course")),
                SpecialtyName = reader.GetString(reader.GetOrdinal("SpecialtyName")),
                FacultyName = reader.GetString(reader.GetOrdinal("FacultyName")),
                StudentCardNumber = reader.GetString(reader.GetOrdinal("StudentCardNumber")),
                Phone = reader.IsDBNull(reader.GetOrdinal("Phone")) ? "" : reader.GetString(reader.GetOrdinal("Phone")),
                Email = reader.IsDBNull(reader.GetOrdinal("Email")) ? "" : reader.GetString(reader.GetOrdinal("Email")),
                EnrollmentDate = reader.GetDateTime(reader.GetOrdinal("EnrollmentDate")),
                IsStudying = reader.GetBoolean(reader.GetOrdinal("IsStudying"))
            };
        }
    }
}