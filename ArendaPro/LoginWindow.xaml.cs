using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ArendaPro
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
        }

        private void Login_Click(object sender, RoutedEventArgs e)
        {
            string username = txtUsername.Text;
            string password = txtPassword.Password;
            string connStr = ConfigurationManager.ConnectionStrings["DbConnection"]?.ConnectionString;
            BD db = new BD(connStr);

            string query = $"SELECT role FROM users WHERE username = @username AND password = @password";

            using (var conn = db.GetConnection())
            {
                conn.Open();
                using (var cmd = new Npgsql.NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("username", username);
                    cmd.Parameters.AddWithValue("password", password);

                    var result = cmd.ExecuteScalar();

                    if (result != null)
                    {
                        string role = result.ToString();

                        var main = new MainWindow(role, this); // ← передаём ссылку на окно входа
                        main.Show();
                        this.Hide();
                    }
                    else
                    {
                        MessageBox.Show("Неверный логин или пароль.");
                    }
                }
            }
        }
        private void txtUsername_TextChanged(object sender, TextChangedEventArgs e)
        {
            txtUsernamePlaceholder.Visibility = string.IsNullOrWhiteSpace(txtUsername.Text)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }
        public void ResetForRelogin()
        {
            txtPassword.Password = "";
            txtPasswordPlaceholder.Visibility = Visibility.Visible;

            txtUsername.Focus();
            txtUsername.SelectAll();
        }
        private void txtPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            txtPasswordPlaceholder.Visibility = string.IsNullOrWhiteSpace(txtPassword.Password)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

    }
}
