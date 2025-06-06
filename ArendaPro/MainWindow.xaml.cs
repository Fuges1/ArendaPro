using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;

namespace ArendaPro
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private LoginWindow loginWindow;
        private string userRole;
        private string userName;
        private string fullName;
        private string email;
        private string passportNumber;

        public MainWindow(string role, string username, string fullName, string email, string passportNumber, Window loginWinn)
        {
            InitializeComponent();
            loginWindow = loginWinn as LoginWindow;

            userRole = role;
            userName = username;
            this.fullName = fullName;
            this.email = email;
            this.passportNumber = passportNumber;

            txtRoleInfo.Text = $"Вы зашли как: {userRole}";
            txtWorkerName.Text = $"ФИО: {fullName}";
            txtEmail.Text = $"Email: {email}";
            txtPassport.Text = $"Паспорт: {passportNumber}";

            switch (userRole.ToLower())
            {
                case "администратор":
                    // Действия для администраторов
                    break;
                case "менеджер":
                    // Действия для менеджеров
                    break;
                case "director":
                    // Действия для директора
                    break;
                case "guest":
                    // Действия для гостей
                    break;
                default:
                    break;
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            // На случай, если пользователь закрыл окно не через кнопку, а [X]
            loginWindow?.ResetForRelogin();
            if (loginWindow != null)
                loginWindow.Show();
        }

        private void Button_Table_Click(object sender, RoutedEventArgs e)
        {
            Table_BD table_BD = new Table_BD(userRole, this);
            table_BD.Show();
            this.Hide();
        }

        private void Button_Dogovor_Click(object sender, RoutedEventArgs e)
        {
            ContractWindow contractWindow = new ContractWindow();
            contractWindow.ShowDialog();
        }

        private void Button_tarifi_Click(object sender, RoutedEventArgs e)
        {
            Tarifi tarifi = new Tarifi();
            tarifi.Show();
        }

        private void Button_spisok_dogovorov_Click(object sender, RoutedEventArgs e)
        {
            spisok_dogovorov spisok_Dogovorov = new spisok_dogovorov();
            spisok_Dogovorov.Show();
        }

        private void Button_OtherOborot_Click(object sender, RoutedEventArgs e)
        {
            OtherOborot otherOborot = new OtherOborot();
            otherOborot.Show();
        }

        private void Button_Exit_Click(object sender, RoutedEventArgs e)
        {
            loginWindow?.ResetForRelogin();
            loginWindow?.Show();
            this.Close();      // Закрыть текущее
        }
    }
}
