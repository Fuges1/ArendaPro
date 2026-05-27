using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Windows;

namespace ArendaPro
{
    // Логика класса: AddConnectionWindow содержит сценарии этого модуля, управляет данными и координирует взаимодействие UI с сервисами.
    public partial class AddConnectionWindow : Window
    {
        public AddConnectionWindow()
        {
            InitializeComponent();
        }

        // Метод Add_Click: обрабатывает нажатие в интерфейсе: считывает ввод, проверяет ограничения и запускает следующий пользовательский шаг (комментарий #1).
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

            // Шаг 1: читаем текущий список подключений, если файл уже создан ранее.
            if (File.Exists(file))
            {
                string json = File.ReadAllText(file);
                list = JsonSerializer.Deserialize<List<MainWindow.DbConnectionConfig>>(json)
                    ?? new List<MainWindow.DbConnectionConfig>();
            }

            // Шаг 2: не допускаем дубликатов по имени, чтобы пользователю было проще выбирать подключение.
            if (list.Exists(c => c.Name == name))
            {
                MessageBox.Show("Подключение с таким именем уже существует.");
                return;
            }

            // Шаг 3: добавляем новое подключение и сохраняем конфиг в читаемом формате JSON.
            list.Add(newConn);
            File.WriteAllText(file, JsonSerializer.Serialize(list, new JsonSerializerOptions { WriteIndented = true }));

            MessageBox.Show("Подключение добавлено!");
            Close();
        }
    }
}
