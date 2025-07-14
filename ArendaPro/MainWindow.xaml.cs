using System;
using System.Windows;
using System.Windows.Threading;
using static ArendaPro.OtherOborot;

namespace ArendaPro
{

    public partial class MainWindow : Window
    {
        private LoginWindow loginWindow;
        private string userRole;
        private string userName;
        private string fullName;
        private string email;
        private string passportNumber;

        public MainWindow(
    string role,
    string username,
    string firstName,
    string middleName,
    string lastName,
    string email,
    string passportNumber,
    string passportIssuedBy,
    DateTime passportIssueDate,
    Window loginWinn)
        {
            InitializeComponent();

            this.userRole = role;
            DispatcherTimer timer = new();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += (s, e) => txtCurrentTime.Text = DateTime.Now.ToString("HH:mm:ss dd.MM.yyyy");
            timer.Start();

            loginWindow = (LoginWindow)loginWinn;
            fullName = $"{lastName} {firstName} {middleName}";
            txtRoleInfo.Text = $"Роль: {role}";
            txtWorkerName.Text = $"{lastName} {firstName} {middleName}";
            txtEmail.Text = email;
            txtPassport.Text = $"Серия и номер: {passportNumber}";
            txtPassportIssued.Text = $"Выдан: {passportIssuedBy}";
            txtPassportDate.Text = $"Дата выдачи: {passportIssueDate:dd.MM.yyyy}";
            ConfigureAccessByRole(role);
        }

        private void ConfigureAccessByRole(string role)
        {
            switch (role.ToLower())
            {
                case "администратор":
                    break;
                case "менеджер":
                    break;
                default:
                    break;
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            loginWindow?.ResetForRelogin();
            if (loginWindow != null)
                loginWindow.Show();
        }

        private void Button_Table_Click(object sender, RoutedEventArgs e)
        {
            Table_BD table_BD = new(userRole, this);
            table_BD.Show();
            this.Hide();
        }

        private void Button_Dogovor_Click(object sender, RoutedEventArgs e)
        {
            ContractWindow contractWindow = new();
            contractWindow.ShowDialog();
        }

        private void Button_tarifi_Click(object sender, RoutedEventArgs e)
        {
            bool isAdmin = string.Equals(CurrentSession.Role,
                              "администратор",
                              StringComparison.OrdinalIgnoreCase);

            var win = new Tarifi(isAdmin);
            win.ShowDialog();
            ;
        }

        private void Button_spisok_dogovorov_Click(object sender, RoutedEventArgs e)
        {
            spisok_dogovorov spisok_Dogovorov = new(userRole, fullName);
            spisok_Dogovorov.Show();
        }

        private void Button_OtherOborot_Click(object sender, RoutedEventArgs e)
        {
            OtherOborot otherOborot = new(fullName);
            otherOborot.Show();
        }

        private void Button_Exit_Click(object sender, RoutedEventArgs e)
        {
            loginWindow?.ResetForRelogin();
            loginWindow?.Show();
            this.Close();       
        }
    }
}
