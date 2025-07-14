using Npgsql;
using System;
using System.Configuration;
using System.Windows;
using System.Windows.Controls;

namespace ArendaPro
{
    public partial class RegisterUserWindow : Window
    {
        private readonly string connectionString;

        public RegisterUserWindow()
        {
            InitializeComponent();
            connectionString = ConfigurationManager.ConnectionStrings["DbConnection"]?.ConnectionString;
        }

        private async void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string username = UsernameBox.Text.Trim();
                string password = PasswordBox.Password;
                string role = ((ComboBoxItem)RoleBox.SelectedItem)?.Content.ToString();
                string lastName = LastNameBox.Text.Trim();
                string firstName = FirstNameBox.Text.Trim();
                string middleName = MiddleNameBox.Text.Trim();
                string email = EmailBox.Text.Trim();
                string passport = PassportBox.Text.Trim();
                if (string.IsNullOrEmpty(passport))
                {
                    MessageBox.Show("Введите серию и номер паспорта.");
                    return;
                }
                string issuedBy = PassportIssuedByBox.Text.Trim();
                DateTime? issueDate = PassportIssueDatePicker.SelectedDate;

                if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(role))
                {
                    MessageBox.Show("Заполните логин, пароль и роль.");
                    return;
                }


                using var conn = new NpgsqlConnection(connectionString);
                await conn.OpenAsync();

                using (var checkCmd = new NpgsqlCommand(
                    @"SELECT COUNT(1)
          FROM public.users
          WHERE passport_number = @passport", conn))
                {
                    checkCmd.Parameters.AddWithValue("passport", passport);
                    bool exists = Convert.ToInt32(await checkCmd.ExecuteScalarAsync()) > 0;
                    if (exists)
                    {
                        MessageBox.Show("Сотрудник с таким паспортом уже зарегистрирован.");
                        return;
                    }
                }
                using (var checkUser = new NpgsqlCommand(
    @"SELECT COUNT(1) FROM public.users WHERE username = @username", conn))
                {
                    checkUser.Parameters.AddWithValue("username", username);
                    bool userExists = Convert.ToInt32(await checkUser.ExecuteScalarAsync()) > 0;
                    if (userExists)
                    {
                        MessageBox.Show($"Пользователь с логином «{username}» уже существует.");
                        return;
                    }
                }
                string hash = BCrypt.Net.BCrypt.HashPassword(password);
                using (var insertCmd = new NpgsqlCommand(@"
        INSERT INTO public.users
          (username, password, role, first_name, last_name, middle_name,
           email, passport_number, passport_issued_by, passport_issue_date)
        VALUES
          (@username, @password, @role, @first, @last, @middle,
           @mail, @passport, @issuedby, @issuedate)", conn))
                {
                    insertCmd.Parameters.AddWithValue("username", username);
                    insertCmd.Parameters.AddWithValue("password", hash);
                    insertCmd.Parameters.AddWithValue("role", role);
                    insertCmd.Parameters.AddWithValue("first", firstName);
                    insertCmd.Parameters.AddWithValue("last", lastName);
                    insertCmd.Parameters.AddWithValue("middle", middleName);
                    insertCmd.Parameters.AddWithValue("mail", email);
                    insertCmd.Parameters.AddWithValue("passport", passport);
                    insertCmd.Parameters.AddWithValue("issuedby", issuedBy);
                    insertCmd.Parameters.AddWithValue("issuedate", issueDate ?? DateTime.Now);

                    await insertCmd.ExecuteNonQueryAsync();
                }

                MessageBox.Show("Сотрудник успешно добавлен.");
                this.Close();
            }
            catch (Exception ex)
            {

                MessageBox.Show("Сотрудник не добавлен по причине: " + ex);
            }
        }


        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
