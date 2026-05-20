IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'StudentDB')
BEGIN
    CREATE DATABASE StudentDB;
END
GO

USE StudentDB;
GO

IF OBJECT_ID('sp_DeleteStudent', 'P') IS NOT NULL DROP PROCEDURE sp_DeleteStudent;
IF OBJECT_ID('sp_UpdateStudent', 'P') IS NOT NULL DROP PROCEDURE sp_UpdateStudent;
IF OBJECT_ID('sp_AddStudent', 'P') IS NOT NULL DROP PROCEDURE sp_AddStudent;
IF OBJECT_ID('sp_GetStudentsFiltered', 'P') IS NOT NULL DROP PROCEDURE sp_GetStudentsFiltered;
IF OBJECT_ID('vw_StudentDetails', 'V') IS NOT NULL DROP VIEW vw_StudentDetails;
IF OBJECT_ID('Users', 'U') IS NOT NULL DROP TABLE Users;
IF OBJECT_ID('Students', 'U') IS NOT NULL DROP TABLE Students;
IF OBJECT_ID('Groups', 'U') IS NOT NULL DROP TABLE [Groups];
IF OBJECT_ID('Specialties', 'U') IS NOT NULL DROP TABLE Specialties;
IF OBJECT_ID('Faculties', 'U') IS NOT NULL DROP TABLE Faculties;
GO

CREATE TABLE Faculties (
    FacultyId INT IDENTITY(1,1) PRIMARY KEY,
    FacultyName NVARCHAR(100) NOT NULL
);

CREATE TABLE Specialties (
    SpecialtyId INT IDENTITY(1,1) PRIMARY KEY,
    SpecialtyName NVARCHAR(100) NOT NULL,
    FacultyId INT NOT NULL CONSTRAINT FK_Specialties_Faculties 
        FOREIGN KEY REFERENCES Faculties(FacultyId)
);

CREATE TABLE [Groups] (
    GroupId INT IDENTITY(1,1) PRIMARY KEY,
    GroupName NVARCHAR(20) NOT NULL,
    SpecialtyId INT NOT NULL CONSTRAINT FK_Groups_Specialties 
        FOREIGN KEY REFERENCES Specialties(SpecialtyId),
    Course INT NOT NULL DEFAULT 1 CHECK (Course BETWEEN 1 AND 4)
);

CREATE TABLE Students (
    StudentId INT IDENTITY(1,1) PRIMARY KEY,
    LastName NVARCHAR(50) NOT NULL,
    FirstName NVARCHAR(50) NOT NULL,
    MiddleName NVARCHAR(50) NULL,
    GroupId INT NOT NULL CONSTRAINT FK_Students_Groups 
        FOREIGN KEY REFERENCES [Groups](GroupId),
    StudentCardNumber NVARCHAR(20) NOT NULL,
    BirthDate DATE NULL,
    Phone NVARCHAR(20) NULL,
    Email NVARCHAR(100) NULL,
    Address NVARCHAR(200) NULL,
    EnrollmentDate DATE NOT NULL DEFAULT GETDATE(),
    IsStudying BIT NOT NULL DEFAULT 1
);

CREATE TABLE Users (
    UserId INT IDENTITY(1,1) PRIMARY KEY,
    Login NVARCHAR(50) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(256) NOT NULL
);

-- Факультеты
INSERT INTO Faculties (FacultyName) VALUES 
(N'Факультет информационных технологий'),
(N'Факультет экономики и управления'),
(N'Факультет гуманитарных наук');

-- Специальности
INSERT INTO Specialties (SpecialtyName, FacultyId) VALUES 
(N'Программная инженерия', 1),
(N'Информационная безопасность', 1),
(N'Прикладная информатика', 1),
(N'Экономика', 2),
(N'Менеджмент', 2),
(N'Психология', 3);

-- Группы
INSERT INTO [Groups] (GroupName, SpecialtyId, Course) VALUES 
(N'ПИ-101', 1, 1),
(N'ПИ-102', 1, 1),
(N'ПИ-201', 1, 2),
(N'ПИ-301', 1, 3),
(N'ИБ-101', 2, 1),
(N'ИБ-201', 2, 2),
(N'ПИ-401', 1, 4),
(N'ЭК-101', 4, 1),
(N'МН-101', 5, 1),
(N'ПС-101', 6, 1);
GO

CREATE VIEW vw_StudentDetails AS
SELECT 
    s.StudentId,
    TRIM(CONCAT(s.LastName, ' ', s.FirstName, ' ', COALESCE(s.MiddleName, ''))) AS FullName,
    g.GroupId,
    g.GroupName,
    g.Course,
    sp.SpecialtyName,
    f.FacultyName,
    s.StudentCardNumber,
    s.Phone,
    s.Email,
    s.EnrollmentDate,
    s.IsStudying
FROM Students s
INNER JOIN [Groups] g ON s.GroupId = g.GroupId
INNER JOIN Specialties sp ON g.SpecialtyId = sp.SpecialtyId
INNER JOIN Faculties f ON sp.FacultyId = f.FacultyId;
GO

CREATE PROCEDURE sp_GetStudentsFiltered
    @SearchText NVARCHAR(100) = NULL,
    @Course INT = NULL,
    @GroupId INT = NULL
AS
BEGIN
    SELECT * FROM vw_StudentDetails
    WHERE 
        (@SearchText IS NULL OR FullName LIKE '%' + @SearchText + '%')
        AND (@Course IS NULL OR Course = @Course)
        AND (@GroupId IS NULL OR GroupId = @GroupId)
    ORDER BY FullName;
END
GO

CREATE PROCEDURE sp_AddStudent
    @LastName NVARCHAR(50),
    @FirstName NVARCHAR(50),
    @MiddleName NVARCHAR(50) = NULL,
    @GroupName NVARCHAR(20),
    @StudentCardNumber NVARCHAR(20),
    @BirthDate DATE = NULL,
    @Phone NVARCHAR(20) = NULL,
    @Email NVARCHAR(100) = NULL,
    @Address NVARCHAR(200) = NULL
AS
BEGIN
    DECLARE @GroupId INT;

    SELECT @GroupId = GroupId FROM [Groups] WHERE GroupName = @GroupName;

    IF @GroupId IS NULL
    BEGIN
        INSERT INTO [Groups] (GroupName, SpecialtyId, Course) 
        VALUES (@GroupName, 1, 1);
        SET @GroupId = SCOPE_IDENTITY();
    END

    INSERT INTO Students (LastName, FirstName, MiddleName, GroupId, StudentCardNumber, BirthDate, Phone, Email, Address, EnrollmentDate, IsStudying)
    VALUES (@LastName, @FirstName, @MiddleName, @GroupId, @StudentCardNumber, @BirthDate, @Phone, @Email, @Address, GETDATE(), 1);

    SELECT SCOPE_IDENTITY();
END
GO

CREATE PROCEDURE sp_UpdateStudent
    @StudentId INT,
    @LastName NVARCHAR(50),
    @FirstName NVARCHAR(50),
    @MiddleName NVARCHAR(50) = NULL,
    @GroupName NVARCHAR(20),
    @StudentCardNumber NVARCHAR(20),
    @BirthDate DATE = NULL,
    @Phone NVARCHAR(20) = NULL,
    @Email NVARCHAR(100) = NULL,
    @Address NVARCHAR(200) = NULL,
    @IsStudying BIT = 1
AS
BEGIN
    DECLARE @GroupId INT;

    SELECT @GroupId = GroupId FROM [Groups] WHERE GroupName = @GroupName;
    IF @GroupId IS NULL
    BEGIN
        INSERT INTO [Groups] (GroupName, SpecialtyId, Course) 
        VALUES (@GroupName, 1, 1);
        SET @GroupId = SCOPE_IDENTITY();
    END

    UPDATE Students
    SET LastName = @LastName,
        FirstName = @FirstName,
        MiddleName = @MiddleName,
        GroupId = @GroupId,
        StudentCardNumber = @StudentCardNumber,
        BirthDate = @BirthDate,
        Phone = @Phone,
        Email = @Email,
        Address = @Address,
        IsStudying = @IsStudying
    WHERE StudentId = @StudentId;
END
GO

CREATE PROCEDURE sp_DeleteStudent
    @StudentId INT,
    @HardDelete BIT = 0
AS
BEGIN
    IF @HardDelete = 1
    BEGIN
        DELETE FROM Students WHERE StudentId = @StudentId;
        IF NOT EXISTS (SELECT 1 FROM Students)
            DBCC CHECKIDENT('Students', RESEED, 0);
    END
    ELSE
        UPDATE Students SET IsStudying = 0 WHERE StudentId = @StudentId;
END
GO

-- root / 123123 (SHA256 hash)
IF NOT EXISTS (SELECT 1 FROM Users WHERE Login = 'root')
    INSERT INTO Users (Login, PasswordHash)
    VALUES ('root', '96cae35ce8a9b0244178bf28e4966c2ce1b8385723a96a6b838858cdd6ca0a1e');
GO