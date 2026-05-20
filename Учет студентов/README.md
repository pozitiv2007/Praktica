# Учет студентов

**WPF-приложение для ведения базы данных студентов**  
Разработано на платформе .NET Framework 4.8 с использованием языка C# и Windows Presentation Foundation (WPF).  
Система управления базами данных — Microsoft SQL Server (LocalDB / SQL Express).

---

## 1. Назначение приложения

Приложение предназначено для автоматизации учёта студентов учебного заведения. Позволяет:

- хранить информацию о студентах (ФИО, группа, курс, специальность, факультет, контактные данные);
- добавлять, редактировать и удалять записи;
- осуществлять поиск и фильтрацию по ФИО и курсу;
- разграничивать доступ через систему авторизации (администраторы);
- управлять учётными записями администраторов.

---

## 2. Технологии и архитектура

### 2.1. Стек технологий

| Компонент | Технология |
|-----------|-----------|
| Язык программирования | C# 7.3 |
| Платформа | .NET Framework 4.8 |
| Графический интерфейс | WPF (XAML) |
| СУБД | Microsoft SQL Server (LocalDB или SQL Express) |
| Доступ к данным | ADO.NET (SqlConnection, SqlCommand) |
| Хэширование паролей | SHA-256 |
| Среда разработки | Visual Studio 2022 |

### 2.2. Архитектура приложения

Приложение построено по трёхслойной архитектуре:

```
┌─────────────────────────────────────────────────────────────┐
│                    Presentation Layer (WPF)                  │
│  LoginWindow  →  MainWindow  →  AdminWindow                 │
├─────────────────────────────────────────────────────────────┤
│                    Data Access Layer (Repository)            │
│  StudentRepository  →  CRUD студента через хранимые процедуры│
│  UserRepository     →  управление пользователями + авториз.  │
├─────────────────────────────────────────────────────────────┤
│                    Database Layer (SQL Server)               │
│  StudentDB: Faculties, Specialties, Groups, Students, Users  │
└─────────────────────────────────────────────────────────────┘
```

---

## 3. Структура базы данных

### 3.1. Схема данных (ER-диаграмма)

```
Faculties ──1:N── Specialties ──1:N── Groups ──1:N── Students
                                                  Users (отдельная таблица)
```

### 3.2. Таблицы базы данных

#### Faculties (Факультеты)

| Колонка | Тип | Описание |
|---------|-----|----------|
| FacultyId | INT (PK, IDENTITY) | Идентификатор факультета |
| FacultyName | NVARCHAR(100) | Название факультета |

Заполняется тремя факультетами: «Факультет информационных технологий», «Факультет экономики и управления», «Факультет гуманитарных наук».

#### Specialties (Специальности)

| Колонка | Тип | Описание |
|---------|-----|----------|
| SpecialtyId | INT (PK, IDENTITY) | Идентификатор специальности |
| SpecialtyName | NVARCHAR(100) | Название специальности |
| FacultyId | INT (FK → Faculties) | Ссылка на факультет |

Содержит 6 специальностей: «Программная инженерия», «Информационная безопасность», «Прикладная информатика», «Экономика», «Менеджмент», «Психология».

#### Groups (Группы)

| Колонка | Тип | Описание |
|---------|-----|----------|
| GroupId | INT (PK, IDENTITY) | Идентификатор группы |
| GroupName | NVARCHAR(20) | Название группы (например, ПИ-101) |
| SpecialtyId | INT (FK → Specialties) | Ссылка на специальность |
| Course | INT (CHECK 1–4) | Курс (1, 2, 3 или 4) |

#### Students (Студенты)

| Колонка | Тип | Описание |
|---------|-----|----------|
| StudentId | INT (PK, IDENTITY) | Идентификатор студента |
| LastName | NVARCHAR(50) | Фамилия |
| FirstName | NVARCHAR(50) | Имя |
| MiddleName | NVARCHAR(50) NULL | Отчество |
| GroupId | INT (FK → Groups) | Ссылка на группу |
| StudentCardNumber | NVARCHAR(20) | Номер студенческого билета |
| BirthDate | DATE NULL | Дата рождения |
| Phone | NVARCHAR(20) NULL | Телефон |
| Email | NVARCHAR(100) NULL | Электронная почта |
| Address | NVARCHAR(200) NULL | Адрес |
| EnrollmentDate | DATE (DEFAULT GETDATE()) | Дата зачисления |
| IsStudying | BIT (DEFAULT 1) | Статус (обучается / отчислен) |

#### Users (Администраторы)

| Колонка | Тип | Описание |
|---------|-----|----------|
| UserId | INT (PK, IDENTITY) | Идентификатор пользователя |
| Login | NVARCHAR(50) (UNIQUE) | Логин для входа |
| PasswordHash | NVARCHAR(256) | Хэш пароля (SHA-256) |

### 3.3. Хранимые процедуры

#### sp_GetStudentsFiltered
Поиск и фильтрация студентов. Принимает параметры:
- `@SearchText` — поиск по ФИО (частичное совпадение через LIKE);
- `@Course` — фильтр по курсу;
- `@GroupId` — фильтр по группе.

#### sp_AddStudent
Добавление нового студента:
1. Разделяет ФИО на фамилию, имя, отчество;
2. Если указанной группы не существует — создаёт её (с привязкой к специальности по умолчанию);
3. Вставляет запись в таблицу Students;
4. Возвращает идентификатор нового студента.

#### sp_UpdateStudent
Обновление данных студента. Аналогично sp_AddStudent, но обновляет существующую запись по StudentId.

#### sp_DeleteStudent
Удаление студента. Режимы:
- `@HardDelete = 1` — физическое удаление из таблицы;
- `@HardDelete = 0` — мягкое удаление (IsStudying = 0).

### 3.4. Представление vw_StudentDetails

Объединяет данные таблиц Students, Groups, Specialties, Faculties через JOIN, формируя полную информацию о студенте (ФИО, группа, курс, специальность, факультет, контактные данные, статус обучения).

---

## 4. Система авторизации

### 4.1. Принцип работы

1. При первом запуске приложения класс `UserRepository` проверяет существование таблицы `Users`. Если её нет — создаёт.
2. Автоматически добавляется (или обновляется) учётная запись администратора по умолчанию:
   - **Логин:** `root`
   - **Пароль:** `123123`
3. Пароль хранится в виде SHA-256 хэша. В базе данных хранится строка: `96cae35ce8a9b0244178bf28e4966c2ce1b8385723a96a6b838858cdd6ca0a1e`.

### 4.2. Окно входа (LoginWindow)

- Поля: «Логин» (TextBox) и «Пароль» (PasswordBox).
- При нажатии «Войти» выполняется проверка: метод `UserRepository.Validate()` сравнивает SHA-256 хэш введённого пароля с хэшем в базе.
- При успешной авторизации открывается главное окно.
- При неверном логине или пароле выводится сообщение об ошибке.

---

## 5. Описание окон приложения

### 5.1. LoginWindow (Окно авторизации)

**Файлы:** `LoginWindow.xaml`, `LoginWindow.xaml.cs`

Первое окно, которое видит пользователь при запуске. Содержит форму входа с двумя полями. После успешной авторизации передаёт логин текущего пользователя в MainWindow для дальнейшего использования (защита от удаления самого себя в панели администратора).

**Класс:** `StudentAppWPF.LoginWindow`

### 5.2. MainWindow (Главное окно)

**Файлы:** `MainWindow.xaml`, `MainWindow.xaml.cs`

Основное окно приложения. Состоит из следующих зон:

#### Верхняя панель
- Заголовок «Учёт студентов».
- Кнопка «Управление админами» — открывает окно AdminWindow (доступно из главного окна).

#### Панель поиска и фильтрации
- Текстовое поле «Поиск по ФИО» — ввод фамилии или её части.
- Выпадающий список «Курс» — выбор курса (1–4 или «Все курсы»).
- Кнопка «Применить» — запускает хранимую процедуру `sp_GetStudentsFiltered`.
- Кнопка «Сброс» — очищает поля поиска и показывает всех студентов.

#### Форма добавления / редактирования
- Поля: ФИО, Группа, Номер студенческого, Телефон, Email.
- Поля ФИО и Группа — обязательные (проверка перед сохранением).
- Кнопка «Сохранить» — вызывает `StudentRepository.AddStudent()` (для новой записи) или `StudentRepository.UpdateStudent()` (для редактирования).
- Кнопка «Отмена» — очищает форму и отменяет редактирование.

#### Таблица студентов (DataGrid)
Отображает данные через представление `vw_StudentDetails`. Колонки:
| Колонка | Привязка |
|---------|----------|
| ID | StudentId |
| ФИО | FullName |
| Группа | GroupName |
| Курс | Course |
| Специальность | SpecialtyName |
| Факультет | FacultyName |
| Студ. билет | StudentCardNumber |
| Телефон | Phone |
| Email | Email |
| Действия | ✏️ (редактировать), 🗑️ (удалить) |

#### Строка состояния
Отображает количество найденных студентов и статус операций.

**Класс:** `StudentAppWPF.MainWindow`

### 5.3. AdminWindow (Окно управления администраторами)

**Файлы:** `AdminWindow.xaml`, `AdminWindow.xaml.cs`

Окно для управления учётными записями. Содержит:

- Таблицу всех администраторов (ID, логин, кнопка удаления).
- Форму добавления нового администратора (логин, пароль, кнопка «Добавить»).

**Ограничения:**
- Нельзя удалить самого себя (проверка по логину текущего пользователя).
- Нельзя добавить пользователя с уже существующим логином (ограничение UNIQUE в БД).

**Класс:** `StudentAppWPF.AdminWindow`

---

## 6. Описание классов C#

### 6.1. Модели

#### Student (`Student.cs`)
Модель данных студента для отображения в DataGrid и передачи между слоями.

| Свойство | Тип | Соответствие в БД |
|----------|-----|-------------------|
| StudentId | int | Students.StudentId |
| FullName | string | CONCAT(LastName, FirstName, MiddleName) |
| GroupName | string | Groups.GroupName |
| Course | int | Groups.Course |
| SpecialtyName | string | Specialties.SpecialtyName |
| FacultyName | string | Faculties.FacultyName |
| StudentCardNumber | string | Students.StudentCardNumber |
| Phone | string | Students.Phone |
| Email | string | Students.Email |
| EnrollmentDate | DateTime | Students.EnrollmentDate |
| IsStudying | bool | Students.IsStudying |

**Пространство имён:** `StudentAppWPF.Models`

#### UserModel (`UserModel.cs`)
Модель данных администратора.

| Свойство | Тип | Описание |
|----------|-----|----------|
| UserId | int | Идентификатор |
| Login | string | Логин |

**Пространство имён:** `StudentAppWPF.Models`

### 6.2. Репозитории

#### StudentRepository (`StudentRepository.cs`)
Класс для работы с данными студентов через ADO.NET.

**Методы:**

| Метод | Описание |
|-------|----------|
| GetAllStudents() | Получает всех студентов через представление vw_StudentDetails |
| GetStudentsFiltered(search, course, groupId) | Поиск и фильтрация через sp_GetStudentsFiltered |
| AddStudent(student) | Добавление через sp_AddStudent. ФИО разбивается на фамилию, имя, отчество |
| UpdateStudent(student) | Обновление через sp_UpdateStudent |
| DeleteStudent(id, hardDelete) | Удаление через sp_DeleteStudent |
| MapToStudent(reader) | Преобразует строку из IDataReader в объект Student |

**Пространство имён:** `StudentAppWPF.Data`

#### UserRepository (`UserRepository.cs`)
Класс для работы с пользователями и авторизацией.

**Методы:**

| Метод | Описание |
|-------|----------|
| EnsureDefaultUser() | Создаёт таблицу Users, если её нет; добавляет или обновляет учётную запись root |
| Validate(login, password) | Проверяет логин и пароль (сравнение SHA-256) |
| GetAllUsers() | Получает список всех администраторов |
| AddUser(login, password) | Добавляет нового администратора |
| DeleteUser(id) | Удаляет администратора по ID |
| HashPassword(password) | SHA-256 хэширование пароля |

**Пространство имён:** `StudentAppWPF.Data`

---

## 7. Структура проекта

```
Учет студентов/
├── .gitignore
├── .git/
├── README.md                          # Документация (текущий файл)
├── CreateDatabase.sql                 # SQL-скрипт создания базы данных
├── token.txt                          # Токен для GitHub
├── log.txt                            # Лог выполнения SQL-скрипта
├── Учет Студентов.slnx                # Файл решения Visual Studio
└── Учет Студентов/
    ├── App.xaml / App.xaml.cs         # Точка входа (StartupUri = LoginWindow.xaml)
    ├── App.config                     # Строка подключения к БД
    ├── Учет Студентов.csproj          # Файл проекта
    ├── LoginWindow.xaml / .cs         # Окно авторизации
    ├── MainWindow.xaml / .cs          # Главное окно
    ├── AdminWindow.xaml / .cs         # Окно управления админами
    ├── Student.cs                     # Модель студента
    ├── UserModel.cs                   # Модель пользователя
    ├── StudentRepository.cs           # Репозиторий студентов
    ├── UserRepository.cs              # Репозиторий пользователей
    └── Properties/                    # Настройки и ресурсы сборки
```

---

## 8. Подготовка и запуск приложения

### 8.1. Требования

- Операционная система: Windows 7 и выше;
- .NET Framework 4.8 (устанавливается вместе с Visual Studio);
- Microsoft SQL Server (LocalDB, SQL Express или полная версия);
- Visual Studio 2022 (Community или другая редакция).

### 8.2. Настройка базы данных

1. Откройте **SQL Server Management Studio (SSMS)**.
2. Подключитесь к серверу. По умолчанию используется `localhost\SQLEXPRESS`.
3. Откройте файл **`CreateDatabase.sql`** (File → Open → File).
4. Нажмите **F5** (Execute).

**Скрипт CreateDatabase.sql выполняет следующие действия:**
1. Создаёт базу данных `StudentDB` (если не существует).
2. Создаёт таблицы: Faculties, Specialties, Groups, Students, Users.
3. Заполняет справочные данные:
   - 3 факультета;
   - 6 специальностей (с привязкой к факультетам);
   - 10 учебных групп (с привязкой к специальностям и курсам).
4. Создаёт представление `vw_StudentDetails`.
5. Создаёт 4 хранимые процедуры: sp_GetStudentsFiltered, sp_AddStudent, sp_UpdateStudent, sp_DeleteStudent.
6. Добавляет учётную запись администратора: root / 123123.

### 8.3. Настройка строки подключения

Строка подключения находится в файле `App.config`:

```xml
<connectionStrings>
    <add name="StudentDB"
         connectionString="Server=localhost\SQLEXPRESS;Database=StudentDB;Trusted_Connection=True;TrustServerCertificate=True;"
         providerName="System.Data.SqlClient" />
</connectionStrings>
```

При необходимости измените `Server=localhost\SQLEXPRESS` на имя вашего SQL Server (например, `(LocalDB)\MSSQLLocalDB` или `.\SQLEXPRESS`).

### 8.4. Запуск приложения

1. Откройте файл решения `Учет Студентов.slnx` в Visual Studio.
2. Убедитесь, что конфигурация сборки — **Debug**, платформа — **Any CPU**.
3. Нажмите клавишу **F5** (или меню Debug → Start Debugging).
4. В появившемся окне авторизации введите:
   - **Логин:** `root`
   - **Пароль:** `123123`
5. Нажмите «Войти».

---

## 9. Руководство пользователя

### 9.1. Авторизация

При запуске открывается окно входа. Введите логин и пароль администратора. При неверных данных появится сообщение об ошибке.

### 9.2. Просмотр студентов

После входа в систему все студенты отображаются в таблице. Для обновления данных используется кнопка «Применить» или «Сброс».

### 9.3. Поиск и фильтрация

Поиск осуществляется по ФИО (частичное совпадение). Дополнительно можно выбрать курс. Для применения фильтра нажмите «Применить». Для сброса — «Сброс».

### 9.4. Добавление студента

1. Заполните поле **ФИО** (формат: Фамилия Имя Отчество, через пробел).
2. Заполните поле **Группа** (например, ПИ-101). Если группы нет в базе — она будет создана автоматически.
3. При необходимости заполните: студенческий билет, телефон, email.
4. Нажмите «Сохранить».

### 9.5. Редактирование студента

1. В таблице нажмите кнопку **✏️** в строке нужного студента.
2. Поля формы заполнятся текущими данными, заголовок изменится на «Редактировать студента».
3. Внесите изменения.
4. Нажмите «Сохранить». Для отмены нажмите «Отмена».

### 9.6. Удаление студента

1. В таблице нажмите кнопку **🗑️** в строке нужного студента.
2. Подтвердите удаление в диалоговом окне.
3. Студент будет помечен как отчисленный (IsStudying = 0) или полностью удалён из базы.

### 9.7. Управление администраторами

1. В главном окне нажмите кнопку **«Управление админами»**.
2. Откроется окно со списком администраторов.
3. Для добавления нового: введите логин и пароль, нажмите «Добавить».
4. Для удаления: нажмите **🗑️** в строке нужного пользователя.
5. Нельзя удалить самого себя.

---

## 10. Возможные ошибки и их решение

| Ошибка | Причина | Решение |
|--------|---------|---------|
| «Неверный логин или пароль» | Неправильно введены учётные данные | Проверьте раскладку клавиатуры, Caps Lock |
| «Заполните ФИО и группу!» | Не заполнены обязательные поля | Заполните ФИО и группу перед сохранением |
| «Не удалось подключиться к БД» | SQL Server не запущен или неверная строка подключения | Запустите SQL Server, проверьте App.config |
| «Пользователь с таким логином уже существует» | Попытка создать дубликат логина | Используйте другой логин |
| «Нельзя удалить самого себя» | Попытка удалить свою учётную запись | Удаление может выполнить другой администратор |
| Пустая таблица после запуска | В базе нет данных | Добавьте студентов через форму |
| Ошибка SQL при выполнении CreateDatabase.sql | Неверный порядок выполнения | Запускайте скрипт целиком, а не по частям |

---

## 11. Безопасность

- Пароли администраторов хранятся в виде SHA-256 хэшей (не в открытом виде).
- Строка подключения к базе данных использует встроенную аутентификацию Windows (Trusted_Connection=True).
- Разграничение доступа: работа со студентами доступна после авторизации; управление администраторами — отдельное окно, доступное из главного.

---

## 12. Заключение

Приложение представляет собой полнофункциональную систему для учёта студентов с графическим интерфейсом, авторизацией, CRUD-операциями и фильтрацией. База данных нормализована (3-я нормальная форма), используются хранимые процедуры и представления. Приложение может быть расширено добавлением новых функций (отчётность, импорт/экспорт данных, история изменений и т.д.).

---

*Приложение разработано в рамках учебной практики.*

---

<div align="center">

# Student Records

**WPF application for managing a student database**  
Built on .NET Framework 4.8 using C# and Windows Presentation Foundation (WPF).  
Database management system — Microsoft SQL Server (LocalDB / SQL Express).

---

## 1. Application Purpose

The application is designed to automate student record-keeping at an educational institution. It allows:

- storing student information (full name, group, course, specialty, faculty, contact details);
- adding, editing, and deleting records;
- searching and filtering by name and course;
- access control via an authentication system (administrators);
- managing administrator accounts.

---

## 2. Technologies and Architecture

### 2.1. Technology Stack

| Component | Technology |
|-----------|-----------|
| Programming Language | C# 7.3 |
| Platform | .NET Framework 4.8 |
| GUI Framework | WPF (XAML) |
| Database | Microsoft SQL Server (LocalDB or SQL Express) |
| Data Access | ADO.NET (SqlConnection, SqlCommand) |
| Password Hashing | SHA-256 |
| IDE | Visual Studio 2022 |

### 2.2. Application Architecture

The application follows a three-layer architecture:

```
┌─────────────────────────────────────────────────────────────┐
│                    Presentation Layer (WPF)                  │
│  LoginWindow  →  MainWindow  →  AdminWindow                 │
├─────────────────────────────────────────────────────────────┤
│                    Data Access Layer (Repository)            │
│  StudentRepository  →  CRUD via stored procedures           │
│  UserRepository     →  user management + authentication     │
├─────────────────────────────────────────────────────────────┤
│                    Database Layer (SQL Server)               │
│  StudentDB: Faculties, Specialties, Groups, Students, Users  │
└─────────────────────────────────────────────────────────────┘
```

---

## 3. Database Structure

### 3.1. Data Schema (ER Diagram)

```
Faculties ──1:N── Specialties ──1:N── Groups ──1:N── Students
                                                  Users (separate table)
```

### 3.2. Database Tables

#### Faculties

| Column | Type | Description |
|--------|------|-------------|
| FacultyId | INT (PK, IDENTITY) | Faculty identifier |
| FacultyName | NVARCHAR(100) | Faculty name |

Populated with three faculties: "Faculty of Information Technologies", "Faculty of Economics and Management", "Faculty of Humanities".

#### Specialties

| Column | Type | Description |
|--------|------|-------------|
| SpecialtyId | INT (PK, IDENTITY) | Specialty identifier |
| SpecialtyName | NVARCHAR(100) | Specialty name |
| FacultyId | INT (FK → Faculties) | Reference to faculty |

Contains 6 specialties: "Software Engineering", "Information Security", "Applied Informatics", "Economics", "Management", "Psychology".

#### Groups

| Column | Type | Description |
|--------|------|-------------|
| GroupId | INT (PK, IDENTITY) | Group identifier |
| GroupName | NVARCHAR(20) | Group name (e.g., CS-101) |
| SpecialtyId | INT (FK → Specialties) | Reference to specialty |
| Course | INT (CHECK 1–4) | Course year (1, 2, 3, or 4) |

#### Students

| Column | Type | Description |
|--------|------|-------------|
| StudentId | INT (PK, IDENTITY) | Student identifier |
| LastName | NVARCHAR(50) | Last name |
| FirstName | NVARCHAR(50) | First name |
| MiddleName | NVARCHAR(50) NULL | Middle name (patronymic) |
| GroupId | INT (FK → Groups) | Reference to group |
| StudentCardNumber | NVARCHAR(20) | Student ID card number |
| BirthDate | DATE NULL | Date of birth |
| Phone | NVARCHAR(20) NULL | Phone number |
| Email | NVARCHAR(100) NULL | Email address |
| Address | NVARCHAR(200) NULL | Residential address |
| EnrollmentDate | DATE (DEFAULT GETDATE()) | Enrollment date |
| IsStudying | BIT (DEFAULT 1) | Status (enrolled / expelled) |

#### Users (Administrators)

| Column | Type | Description |
|--------|------|-------------|
| UserId | INT (PK, IDENTITY) | User identifier |
| Login | NVARCHAR(50) (UNIQUE) | Login name |
| PasswordHash | NVARCHAR(256) | SHA-256 password hash |

### 3.3. Stored Procedures

#### sp_GetStudentsFiltered
Search and filter students. Parameters:
- `@SearchText` — search by full name (partial match via LIKE);
- `@Course` — filter by course;
- `@GroupId` — filter by group.

#### sp_AddStudent
Add a new student:
1. Splits the full name into last name, first name, and middle name;
2. If the specified group does not exist — creates it (with default specialty binding);
3. Inserts a record into the Students table;
4. Returns the new student's identifier.

#### sp_UpdateStudent
Update student data. Similar to sp_AddStudent, but updates an existing record by StudentId.

#### sp_DeleteStudent
Delete a student. Modes:
- `@HardDelete = 1` — physical deletion from the table;
- `@HardDelete = 0` — soft delete (IsStudying = 0).

### 3.4. View vw_StudentDetails

Joins data from Students, Groups, Specialties, and Faculties tables via JOIN, forming complete student information (full name, group, course, specialty, faculty, contact details, enrollment status).

---

## 4. Authentication System

### 4.1. How It Works

1. On the first application launch, the `UserRepository` class checks whether the `Users` table exists. If not — it creates it.
2. A default administrator account is automatically added (or updated):
   - **Login:** `root`
   - **Password:** `123123`
3. The password is stored as a SHA-256 hash. The database stores: `96cae35ce8a9b0244178bf28e4966c2ce1b8385723a96a6b838858cdd6ca0a1e`.

### 4.2. Login Window

- Fields: "Login" (TextBox) and "Password" (PasswordBox).
- Pressing "Login" triggers validation: `UserRepository.Validate()` compares the SHA-256 hash of the entered password against the hash stored in the database.
- On successful authentication, the main window opens.
- On invalid credentials, an error message is displayed.

---

## 5. Application Windows

### 5.1. LoginWindow

**Files:** `LoginWindow.xaml`, `LoginWindow.xaml.cs`

The first window the user sees on startup. Contains a login form with two fields. After successful authentication, it passes the current user's login to MainWindow for further use (preventing self-deletion in the admin panel).

**Class:** `StudentAppWPF.LoginWindow`

### 5.2. MainWindow

**Files:** `MainWindow.xaml`, `MainWindow.xaml.cs`

The main application window. Consists of the following areas:

#### Top Panel
- Title "Student Records".
- "Admin Management" button — opens the AdminWindow.

#### Search and Filter Panel
- Text field "Search by name" — enter a surname or part of it.
- Dropdown "Course" — select a course (1–4 or "All courses").
- "Apply" button — executes the `sp_GetStudentsFiltered` stored procedure.
- "Reset" button — clears search fields and displays all students.

#### Add / Edit Form
- Fields: Full Name, Group, Student Card Number, Phone, Email.
- Full Name and Group are required (validated before saving).
- "Save" button — calls `StudentRepository.AddStudent()` (new record) or `StudentRepository.UpdateStudent()` (edit).
- "Cancel" button — clears the form and cancels editing.

#### Student DataGrid
Displays data via the `vw_StudentDetails` view. Columns:
| Column | Binding |
|--------|---------|
| ID | StudentId |
| Full Name | FullName |
| Group | GroupName |
| Course | Course |
| Specialty | SpecialtyName |
| Faculty | FacultyName |
| Student Card | StudentCardNumber |
| Phone | Phone |
| Email | Email |
| Actions | ✏️ (edit), 🗑️ (delete) |

#### Status Bar
Displays the number of found students and operation status.

**Class:** `StudentAppWPF.MainWindow`

### 5.3. AdminWindow

**Files:** `AdminWindow.xaml`, `AdminWindow.xaml.cs`

Window for managing user accounts. Contains:

- Table of all administrators (ID, login, delete button).
- Form for adding a new administrator (login, password, "Add" button).

**Restrictions:**
- Cannot delete yourself (validated against the current user's login).
- Cannot add a user with an existing login (UNIQUE constraint in the database).

**Class:** `StudentAppWPF.AdminWindow`

---

## 6. C# Class Descriptions

### 6.1. Models

#### Student (`Student.cs`)
Student data model for display in DataGrid and layer-to-layer transfer.

| Property | Type | Database Mapping |
|----------|------|------------------|
| StudentId | int | Students.StudentId |
| FullName | string | CONCAT(LastName, FirstName, MiddleName) |
| GroupName | string | Groups.GroupName |
| Course | int | Groups.Course |
| SpecialtyName | string | Specialties.SpecialtyName |
| FacultyName | string | Faculties.FacultyName |
| StudentCardNumber | string | Students.StudentCardNumber |
| Phone | string | Students.Phone |
| Email | string | Students.Email |
| EnrollmentDate | DateTime | Students.EnrollmentDate |
| IsStudying | bool | Students.IsStudying |

**Namespace:** `StudentAppWPF.Models`

#### UserModel (`UserModel.cs`)
Administrator data model.

| Property | Type | Description |
|----------|------|-------------|
| UserId | int | Identifier |
| Login | string | Login name |

**Namespace:** `StudentAppWPF.Models`

### 6.2. Repositories

#### StudentRepository (`StudentRepository.cs`)
Class for working with student data via ADO.NET.

**Methods:**

| Method | Description |
|--------|-------------|
| GetAllStudents() | Retrieves all students via the vw_StudentDetails view |
| GetStudentsFiltered(search, course, groupId) | Search and filter via sp_GetStudentsFiltered |
| AddStudent(student) | Add via sp_AddStudent. Splits full name into last, first, middle names |
| UpdateStudent(student) | Update via sp_UpdateStudent |
| DeleteStudent(id, hardDelete) | Delete via sp_DeleteStudent |
| MapToStudent(reader) | Converts an IDataReader row to a Student object |

**Namespace:** `StudentAppWPF.Data`

#### UserRepository (`UserRepository.cs`)
Class for user management and authentication.

**Methods:**

| Method | Description |
|--------|-------------|
| EnsureDefaultUser() | Creates the Users table if missing; adds or updates the root account |
| Validate(login, password) | Validates login and password (SHA-256 comparison) |
| GetAllUsers() | Retrieves the list of all administrators |
| AddUser(login, password) | Adds a new administrator |
| DeleteUser(id) | Deletes an administrator by ID |
| HashPassword(password) | SHA-256 password hashing |

**Namespace:** `StudentAppWPF.Data`

---

## 7. Project Structure

```
StudentRecords/
├── .gitignore
├── .git/
├── README.md                          # Documentation (this file)
├── CreateDatabase.sql                 # Database creation SQL script
├── token.txt                          # GitHub token
├── log.txt                            # SQL script execution log
├── StudentRecords.slnx                # Visual Studio solution file
└── StudentRecords/
    ├── App.xaml / App.xaml.cs         # Entry point (StartupUri = LoginWindow.xaml)
    ├── App.config                     # Database connection string
    ├── StudentRecords.csproj          # Project file
    ├── LoginWindow.xaml / .cs         # Login window
    ├── MainWindow.xaml / .cs          # Main window
    ├── AdminWindow.xaml / .cs         # Admin management window
    ├── Student.cs                     # Student model
    ├── UserModel.cs                   # User model
    ├── StudentRepository.cs           # Student repository
    ├── UserRepository.cs              # User repository
    └── Properties/                    # Assembly settings and resources
```

---

## 8. Setup and Launch

### 8.1. Requirements

- Operating System: Windows 7 and above;
- .NET Framework 4.8 (installed with Visual Studio);
- Microsoft SQL Server (LocalDB, SQL Express, or full version);
- Visual Studio 2022 (Community or other edition).

### 8.2. Database Setup

1. Open **SQL Server Management Studio (SSMS)**.
2. Connect to the server. Default is `localhost\SQLEXPRESS`.
3. Open the **`CreateDatabase.sql`** file (File → Open → File).
4. Press **F5** (Execute).

**The CreateDatabase.sql script performs the following:**
1. Creates the `StudentDB` database (if it does not exist).
2. Creates tables: Faculties, Specialties, Groups, Students, Users.
3. Populates reference data:
   - 3 faculties;
   - 6 specialties (bound to faculties);
   - 10 study groups (bound to specialties and courses).
4. Creates the `vw_StudentDetails` view.
5. Creates 4 stored procedures: sp_GetStudentsFiltered, sp_AddStudent, sp_UpdateStudent, sp_DeleteStudent.
6. Adds the default administrator account: root / 123123.

### 8.3. Connection String Configuration

The connection string is located in `App.config`:

```xml
<connectionStrings>
    <add name="StudentDB"
         connectionString="Server=localhost\SQLEXPRESS;Database=StudentDB;Trusted_Connection=True;TrustServerCertificate=True;"
         providerName="System.Data.SqlClient" />
</connectionStrings>
```

If needed, change `Server=localhost\SQLEXPRESS` to your SQL Server instance name (e.g., `(LocalDB)\MSSQLLocalDB` or `.\SQLEXPRESS`).

### 8.4. Launching the Application

1. Open the solution file `StudentRecords.slnx` in Visual Studio.
2. Ensure the build configuration is **Debug** and the platform is **Any CPU**.
3. Press **F5** (or Debug → Start Debugging).
4. In the login window that appears, enter:
   - **Login:** `root`
   - **Password:** `123123`
5. Click "Login".

---

## 9. User Guide

### 9.1. Authentication

On startup, the login window opens. Enter the administrator login and password. Invalid credentials will display an error message.

### 9.2. Viewing Students

After logging in, all students are displayed in the table. Use the "Apply" or "Reset" buttons to refresh the data.

### 9.3. Search and Filter

Search is performed by full name (partial match). You can additionally filter by course. Click "Apply" to apply the filter. Click "Reset" to clear it.

### 9.4. Adding a Student

1. Fill in the **Full Name** field (format: Last Name First Name Middle Name, space-separated).
2. Fill in the **Group** field (e.g., CS-101). If the group does not exist in the database, it will be created automatically.
3. Optionally fill in: student card number, phone, email.
4. Click "Save".

### 9.5. Editing a Student

1. In the table, click the **✏️** button on the desired student's row.
2. The form fields will populate with the current data, and the title will change to "Edit Student".
3. Make changes.
4. Click "Save". To cancel, click "Cancel".

### 9.6. Deleting a Student

1. In the table, click the **🗑️** button on the desired student's row.
2. Confirm deletion in the dialog box.
3. The student will be marked as expelled (IsStudying = 0) or completely removed from the database.

### 9.7. Managing Administrators

1. In the main window, click the **"Admin Management"** button.
2. A window with the list of administrators will open.
3. To add a new one: enter login and password, click "Add".
4. To delete: click **🗑️** on the desired user's row.
5. You cannot delete yourself.

---

## 10. Common Errors and Solutions

| Error | Cause | Solution |
|-------|-------|----------|
| "Invalid login or password" | Incorrect credentials | Check keyboard layout, Caps Lock |
| "Fill in the full name and group!" | Required fields are empty | Fill in full name and group before saving |
| "Could not connect to database" | SQL Server is not running or wrong connection string | Start SQL Server, check App.config |
| "A user with this login already exists" | Attempt to create a duplicate login | Use a different login |
| "Cannot delete yourself" | Attempt to delete your own account | Deletion must be performed by another administrator |
| Empty table on startup | No data in the database | Add students via the form |
| SQL error when running CreateDatabase.sql | Incorrect execution order | Run the entire script at once, not in parts |

---

## 11. Security

- Administrator passwords are stored as SHA-256 hashes (not in plain text).
- The database connection string uses Windows Integrated Authentication (Trusted_Connection=True).
- Access control: student operations are available after authentication; admin management is in a separate window accessible from the main one.

---

## 12. Conclusion

The application is a full-featured student records system with a graphical interface, authentication, CRUD operations, and filtering. The database is normalized (3rd normal form), using stored procedures and views. The application can be extended with additional features (reporting, data import/export, change history, etc.).

---

*Application developed as part of an educational practice.*
