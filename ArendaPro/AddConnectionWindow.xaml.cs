using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Windows;

namespace ArendaPro
{
    public partial class AddConnectionWindow : Window
    {
        public AddConnectionWindow()
        {
            InitializeComponent();
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            string name = NameBox.Text.Trim();
            string conn = ConnectionBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(conn))
            {
                MessageBox.Show("Введите название и строку подключения.");
                return;
            }

            var newConn = new MainWindow.DbConnectionConfig
            {
                Name = name,
                ConnectionString = conn
            };

            var file = "connections.json";
            var list = new List<MainWindow.DbConnectionConfig>();

            if (File.Exists(file))
            {
                string json = File.ReadAllText(file);
                list = JsonSerializer.Deserialize<List<MainWindow.DbConnectionConfig>>(json);
            }

            list.Add(newConn);
            File.WriteAllText(file, JsonSerializer.Serialize(list, new JsonSerializerOptions { WriteIndented = true }));

            MessageBox.Show("Подключение добавлено!");
            this.Close();
        }
    }
}
