using System;
using System.Windows;
using System.Windows.Threading;
using static ArendaPro.OtherOborot;

namespace ArendaPro
{

    // Логика класса: MainWindow содержит сценарии этого модуля, управляет данными и координирует взаимодействие UI с сервисами.
    public partial class MainWindow : Window
    {
        private readonly LoginWindow loginWindow;
        private readonly string userRole;
        private readonly string fullName;

        public MainWindow(
    string role,
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

        // Метод ConfigureAccessByRole: реализует отдельный этап внутренней логики модуля: трансформирует вход, применяет правила и формирует следующий шаг исполнения (комментарий #1).
        private void ConfigureAccessByRole(string role)
        {
            switch (role.ToLower())
            {
                case "admin":
                    break;
                case "manager":
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

        // Метод Button_Table_Click: обрабатывает нажатие в интерфейсе: считывает ввод, проверяет ограничения и запускает следующий пользовательский шаг (комментарий #2).
        private void Button_Table_Click(object sender, RoutedEventArgs e)
        {
            Table_BD table_BD = new(userRole, this);
            table_BD.Show();
            this.Hide();
        }

        // Метод Button_Dogovor_Click: обрабатывает нажатие в интерфейсе: считывает ввод, проверяет ограничения и запускает следующий пользовательский шаг (комментарий #3).
        private void Button_Dogovor_Click(object sender, RoutedEventArgs e)
        {
            ContractWindow contractWindow = new();
            contractWindow.ShowDialog();
        }

        // Метод Button_tarifi_Click: обрабатывает нажатие в интерфейсе: считывает ввод, проверяет ограничения и запускает следующий пользовательский шаг (комментарий #4).
        private void Button_tarifi_Click(object sender, RoutedEventArgs e)
        {
            bool isAdmin = string.Equals(CurrentSession.Role,
                              "admin",
                              StringComparison.OrdinalIgnoreCase);

            var win = new Tarifi(isAdmin);
            win.ShowDialog();
        }

        // Метод Button_spisok_dogovorov_Click: обрабатывает нажатие в интерфейсе: считывает ввод, проверяет ограничения и запускает следующий пользовательский шаг (комментарий #5).
        private void Button_spisok_dogovorov_Click(object sender, RoutedEventArgs e)
        {
            spisok_dogovorov spisok_Dogovorov = new(userRole, fullName);
            spisok_Dogovorov.Show();
        }

        // Метод Button_OtherOborot_Click: обрабатывает нажатие в интерфейсе: считывает ввод, проверяет ограничения и запускает следующий пользовательский шаг (комментарий #6).
        private void Button_OtherOborot_Click(object sender, RoutedEventArgs e)
        {
            OtherOborot otherOborot = new(fullName);
            otherOborot.Show();
        }

        // Метод Button_Exit_Click: обрабатывает нажатие в интерфейсе: считывает ввод, проверяет ограничения и запускает следующий пользовательский шаг (комментарий #7).
        private void Button_Exit_Click(object sender, RoutedEventArgs e)
        {
            loginWindow?.ResetForRelogin();
            loginWindow?.Show();
            this.Close();       
        }
    }
}
