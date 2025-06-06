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

        private List<DbConnectionConfig> connections;
        public static string ActiveConnectionString; // используется в BD

        private string userRole;
        public MainWindow(string role, Window loginWinn)
        {
            InitializeComponent();
            loginWindow = loginWinn as LoginWindow;

            userRole = role;
           
            txtRoleInfo.Text = $"Вы зашли как: {userRole}";

            // LoadConnections(); 

            switch (userRole.ToLower())
            {
                case "администратор":
                    break;
                case "менеджер":
                    break;
                case "director":
                    break;
                case "guest":
                default:
                    break;
            }
        }
        public class DbConnectionConfig
        {
            public string Name { get; set; }
            public string ConnectionString { get; set; }
        }
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            // На случай, если пользователь закрыл окно не через кнопку, а [X]
            loginWindow?.ResetForRelogin();
            if (loginWindow != null)
                loginWindow.Show();
        }
        private void LoadConnections()
        {
            connections = new List<DbConnectionConfig>();

            // Добавляем локальное подключение первым
            connections.Add(new DbConnectionConfig
            {
                Name = "Локальное (по умолчанию)",
                ConnectionString = "Host=localhost;Port=5432;Username=postgres;Password=123;Database=arendapro;"
            });

            try
            {
                if (File.Exists("connections.json"))
                {
                    string json = File.ReadAllText("connections.json");
                    var userConnections = JsonSerializer.Deserialize<List<DbConnectionConfig>>(json);

                    if (userConnections != null)
                        connections.AddRange(userConnections);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки подключений: " + ex.Message);
            }

            ConnectionSelector.ItemsSource = connections;
            ConnectionSelector.DisplayMemberPath = "Name";
            ConnectionSelector.SelectedIndex = 0;

            ActiveConnectionString = connections[0].ConnectionString;
        }
        private void ConnectionSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ConnectionSelector.SelectedItem is DbConnectionConfig selected)
            {
                ActiveConnectionString = selected.ConnectionString;

                // Проверка нужных таблиц
                if (!CheckDatabaseStructure())
                {
                    MessageBox.Show("Выбранная база не соответствует структуре ArendaPro!");
                    return;
                }

                MessageBox.Show($"Подключено: {selected.Name}");
            }
        }
        private bool CheckDatabaseStructure()
        {
            try
            {
                using var conn = new NpgsqlConnection(ActiveConnectionString);
                conn.Open();

                string[] requiredTables = { "clients", "cars", "contracts", "rentals", "users" };
                var existing = new List<string>();

                using var cmd = new NpgsqlCommand(
                    "SELECT table_name FROM information_schema.tables WHERE table_schema='public'", conn);
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                    existing.Add(reader.GetString(0));

                return requiredTables.All(t => existing.Contains(t));
            }
            catch
            {
                return false;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
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

        private void Button_Button_OtherOborot(object sender, RoutedEventArgs e)
        {
            OtherOborot otherOborot = new OtherOborot();
            otherOborot.Show();
        }
        private void AddConnection_Click(object sender, RoutedEventArgs e)
        {
            var addWindow = new AddConnectionWindow();
            addWindow.ShowDialog(); // ждём закрытия

            LoadConnections(); // обновить список после добавления
        }

        private void Button_Click_Exit(object sender, RoutedEventArgs e)
        {
            loginWindow?.ResetForRelogin();
            loginWindow?.Show();
            this.Close();      // Закрыть текущее
        }
    }
}
