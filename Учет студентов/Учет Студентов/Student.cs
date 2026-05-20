using System;

namespace StudentAppWPF.Models
{
    public class Student
    {
        public int RowNumber { get; set; }           
        public int StudentId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string GroupName { get; set; } = string.Empty;
        public int Course { get; set; }
        public string SpecialtyName { get; set; } = string.Empty;
        public string FacultyName { get; set; } = string.Empty;
        public string StudentCardNumber { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public DateTime EnrollmentDate { get; set; }
        public bool IsStudying { get; set; }
    }
}
