using Npgsql;
using System;
using System.Configuration;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using static ArendaPro.OtherOborot;

namespace ArendaPro
{
    public partial class LoginWindow : Window
    {
        public class UserData
        {
            public int UserId { get; set; }
            public string Username { get; set; }
            public string Role { get; set; }
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public string MiddleName { get; set; }
            public string Email { get; set; }
            public string PassportNumber { get; set; }
            public string PassportIssuedBy { get; set; }
            public DateTime PassportIssueDate { get; set; }
        }

        private readonly string connectionString =
            ConfigurationManager.ConnectionStrings["DbConnection"]?.ConnectionString;

        private MainWindow mainWindow;
        private bool _isLoggingIn = false;

        public LoginWindow() => InitializeComponent();

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isLoggingIn) return;
            _isLoggingIn = true;
            SetUiEnabled(false);

            try
            {
                string username = txtUsername.Text.Trim();
                string password = txtPassword.Password.Trim();

                if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                {
                    ShowError("Заполните все поля.");
                    return;
                }

                await using var conn = new NpgsqlConnection(connectionString);
                await conn.OpenAsync();

                if (!await CheckUserExists(conn, username))
                {
                    ShowError("Пользователь не найден.");
                    return;
                }

                var (storedPwd, isBcrypt) = await GetPasswordWithDiagnostics(conn, username);
                bool isValid = isBcrypt
                    ? BCrypt.Net.BCrypt.Verify(password, storedPwd)
                    : password == storedPwd;

                if (!isBcrypt && isValid)
                    await UpgradePasswordToBcrypt(conn, username, password);

                if (isValid)
                {
                    var user = await GetFullUserData(conn, username);
                    OpenMainWindow(user);
                }
                else
                {
                    ShowError("Неверный пароль. Проверьте правильность ввода.");
                }
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка системы: {ex.Message}");
                Debug.WriteLine(ex);
            }
            finally
            {
                _isLoggingIn = false;
                SetUiEnabled(true);
            }
        }

        private void txtUsername_TextChanged(object sender, TextChangedEventArgs e) =>
            txtUsernamePlaceholder.Visibility =
                string.IsNullOrWhiteSpace(txtUsername.Text)
                    ? Visibility.Visible
                    : Visibility.Collapsed;

        private void txtPassword_PasswordChanged(object sender, RoutedEventArgs e) =>
            txtPasswordPlaceholder.Visibility =
                string.IsNullOrWhiteSpace(txtPassword.Password)
                    ? Visibility.Visible
                    : Visibility.Collapsed;

        private void SetUiEnabled(bool enabled)
        {
            LoginButton.IsEnabled = enabled;
            txtUsername.IsEnabled = enabled;
            txtPassword.IsEnabled = enabled;
        }

        private void ShowError(string msg)
        {
            ErrorTextBlock.Text = msg;
            ErrorTextBlock.Visibility = Visibility.Visible;
        }

        private bool IsBcryptHash(string s) =>
          !string.IsNullOrEmpty(s) &&
          (s.StartsWith("$2a$") || s.StartsWith("$2b$") || s.StartsWith("$2y$"));
        private async Task<bool> CheckUserExists(NpgsqlConnection conn, string username)
        {
            using var cmd = new NpgsqlCommand(
                "SELECT 1 FROM users WHERE username = @username", conn);
            cmd.Parameters.AddWithValue("username", username);
            return (await cmd.ExecuteScalarAsync()) != null;
        }

        private async Task<(string password, bool isBcrypt)>
            GetPasswordWithDiagnostics(NpgsqlConnection conn, string username)
        {
            using var cmd = new NpgsqlCommand(
                "SELECT password FROM users WHERE username = @username", conn);
            cmd.Parameters.AddWithValue("username", username);

            string pwd = (await cmd.ExecuteScalarAsync())?.ToString() ?? string.Empty;
            bool isBC = IsBcryptHash(pwd);

            Debug.WriteLine($"Пароль из БД: {pwd}  |  BCrypt: {isBC}");
            return (pwd, isBC);
        }

        private async Task UpgradePasswordToBcrypt(
            NpgsqlConnection conn, string username, string plainPwd)
        {
            string hash = BCrypt.Net.BCrypt.HashPassword(plainPwd);
            using var cmd = new NpgsqlCommand(
                "UPDATE users SET password = @p WHERE username = @u", conn);
            cmd.Parameters.AddWithValue("p", hash);
            cmd.Parameters.AddWithValue("u", username);
            await cmd.ExecuteNonQueryAsync();
        }

        private async Task<UserData> GetFullUserData(NpgsqlConnection conn, string username)
        {
            const string q = @"
        SELECT id, role, first_name, last_name, middle_name,
               email, passport_number, passport_issued_by, passport_issue_date
        FROM users WHERE username = @u";

            using var cmd = new NpgsqlCommand(q, conn);
            cmd.Parameters.AddWithValue("u", username);

            using var r = await cmd.ExecuteReaderAsync();
            if (await r.ReadAsync())
            {
                return new UserData
                {
                    UserId = r.GetInt32(0),
                    Role = r.GetString(1),
                    FirstName = r.IsDBNull(2) ? "" : r.GetString(2),
                    LastName = r.IsDBNull(3) ? "" : r.GetString(3),
                    MiddleName = r.IsDBNull(4) ? "" : r.GetString(4),
                    Email = r.IsDBNull(5) ? "" : r.GetString(5),
                    PassportNumber = r.IsDBNull(6) ? "" : r.GetString(6),
                    PassportIssuedBy = r.IsDBNull(7) ? "" : r.GetString(7),
                    PassportIssueDate = r.IsDBNull(8) ? DateTime.MinValue : r.GetDateTime(8),
                    Username = username
                };
            }

            throw new InvalidOperationException("Данные пользователя не найдены");
        }
        private void OpenMainWindow(UserData u)
        {
            CurrentSession.UserId = u.UserId;
            CurrentSession.Role = u.Role;
            CurrentSession.FullName = $"{u.LastName} {u.FirstName} {u.MiddleName}".Trim();
            CurrentSession.Email = u.Email;

            mainWindow = new MainWindow(
                role: u.Role,
                username: u.Username,
                firstName: u.FirstName,
                middleName: u.MiddleName,
                lastName: u.LastName,
                email: u.Email,
                passportNumber: u.PassportNumber,
                passportIssuedBy: u.PassportIssuedBy,
                passportIssueDate: u.PassportIssueDate,
                loginWinn: this);

            mainWindow.Show();
            Hide();
        }

        public void ResetForRelogin()
        {
            txtPassword.Password = "";
            txtPasswordPlaceholder.Visibility = Visibility.Visible;
            txtUsername.Text = "";
            txtUsernamePlaceholder.Visibility = Visibility.Visible;
            txtUsername.Focus();
            ErrorTextBlock.Visibility = Visibility.Hidden;
            SetUiEnabled(true);
        }
    }
}
