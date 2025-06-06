using Npgsql;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using static ArendaPro.InputDialog;

namespace ArendaPro
{
    public partial class Table_BD : Window
    {
        private Stack<string> ddlUndoStack = new();
        private Dictionary<string, Stack<DataTable>> undoStacks = new();
        private BD db;
        private string selectedTable;
        private DataTable currentTable;
        private string userRole;
        private Dictionary<string, DataTable> loadedTables = new Dictionary<string, DataTable>();
        private Window parentWindow;
        private Dictionary<string, string> tableNameDisplayMap = new Dictionary<string, string>
        {
    { "users", "Пользователи" },
    { "cars", "Автомобили" },
    { "clients", "Клиенты" },
    { "rentals", "Аренды" },
    { "contracts", "Договоры" },
    { "vouchers", "Ваучеры" },
    { "tariffs", "Тарифы" },
    { "contract_statuses", "Статус договоров" },
        };
        private Dictionary<string, string> columnDisplayNames = new Dictionary<string, string>
        {
   { "id", "ID" },
    { "full_name", "ФИО клиента" },
    { "phone", "Телефон" },
    { "email", "Эл. почта" },
    { "passport", "Паспорт" },
    { "birth_date", "Дата рождения" },
    { "username", "Логин" },
    { "password", "Пароль" },
    { "role", "Роль" },
    { "brand", "Марка" },
    { "model", "Модель" },
    { "license_plate", "Гос. номер" },
    { "status", "Статус" },
    { "start_date", "Начало аренды" },
    { "end_date", "Конец аренды" },
    { "price", "Цена" },
    { "car_id", "Автомобиль" },
    { "client_id", "Клиент" },
    { "user_id", "Сотрудник" },
    { "rental_id", "Аренда" },
    { "file_path", "Путь к файлу" },
    { "created_at", "Создано" },
    { "code", "Код" },
    { "issue_date", "Дата выдачи" },
    { "pasport", "Паспорт" },
     

    // 👇 Добавленные из таблиц
    { "marka", "Марка автомобиля" },
    { "gos_nomer", "Гос. номер" },
    { "vin", "VIN" },
    { "registr_svidetelstva", "Рег. свидетельство" },
    { "cvet", "Цвет" },
    { "familia_vladelca", "Фамилия владельца" },
    { "imia_vladelca", "Имя владельца" },
    { "otchestvo_vladelca", "Отчество владельца" },
    { "god_vipuska", "Год выпуска" },
    { "pts", "ПТС" },

    { "familia", "Фамилия клиента" },
    { "imia", "Имя клиента" },
    { "otchestvo", "Отчество клиента" },
    { "data_rozhdeniya", "Дата рождения" },
    { "Kem_vydan_pasport", "Кем выдан паспорт" },
    { "data_vydachi_pasporta", "Дата выдачи паспорта" },
    { "adres_prozhivaniya", "Адрес проживания" },
    { "telefon", "Телефон" },
    { "voditelskoe_udostoverenie", "Вод. удостоверение" },
    { "data_vydachi_voditelskogo", "Дата выдачи в/у" },

    { "contract_number", "Номер договора" },
    { "creation_date", "Дата создания" },
    { "place_start", "Место получения" },
    { "place_end", "Место возврата" },
    { "time_start", "Время начала" },
    { "time_end", "Время окончания" },

    { "description", "Описание статуса" },

    // на случай нестандартных полей или позже добавленных
    { "status_code", "Код статуса" }
            
    // добавь другие поля по мере необходимости
        };
        public Table_BD(string role, Window parent)
        {
            InitializeComponent();
            string connStr = ConfigurationManager.ConnectionStrings["DbConnection"]?.ConnectionString;
            userRole = role;
            parentWindow = parent;
            this.Closed += Table_BD_Closed;
            db = new BD(connStr);

            if (userRole == "менеджер")
            {
                Button_admin_1.Visibility = Visibility.Collapsed;
                Button_admin_2.Visibility = Visibility.Collapsed;
                // Жёстко заданные таблицы для менеджера
                var managerTables = new[] { "users", "cars", "rentals", "clients" };

                foreach (var name in managerTables)
                {
                    TablesList.Items.Add(new ListBoxItem
                    {
                        Content = tableNameDisplayMap.ContainsKey(name) ? tableNameDisplayMap[name] : name,
                        Tag = name
                    });
                }
            }
            else if (userRole == "администратор")
            {
                try
                {
                    Button_admin_1.Visibility = Visibility.Visible;
                    Button_admin_2.Visibility = Visibility.Visible;
                    using (var conn = db.GetConnection())
                    {
                        conn.Open();
                        string query = "SELECT table_name FROM information_schema.tables WHERE table_schema = 'public' AND table_type = 'BASE TABLE';";

                        using (var cmd = new NpgsqlCommand(query, conn))
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string tableName = reader.GetString(0);

                                TablesList.Items.Add(new ListBoxItem
                                {
                                    Content = tableNameDisplayMap.ContainsKey(tableName) ? tableNameDisplayMap[tableName] : tableName,
                                    Tag = tableName
                                });
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка при получении списка таблиц: " + ex.Message);
                }
            }
        }
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.Z)
            {
                UndoLastAction();
            }
        }
        private void UndoLastAction()
        {
            if (selectedTable != null && undoStacks.ContainsKey(selectedTable) && undoStacks[selectedTable].Count > 0)
            {
                currentTable = undoStacks[selectedTable].Pop();
                loadedTables[selectedTable] = currentTable;
                DataTableGrid.ItemsSource = currentTable.DefaultView;
                
            }
            else
            {
                MessageBox.Show("Нет изменений для отмены.");
            }
        }

        private void Table_BD_Closed(object sender, EventArgs e)
        {
            parentWindow.Show(); // показываем MainWindow обратно
        }
        private void TablesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TablesList.SelectedItem is ListBoxItem item && item.Tag is string realTableName)
            {
                selectedTable = realTableName;
                LoadTable(selectedTable);
            }
        }

        private void LoadTable(string tableName)
        {
            try
            {
                string query = $"SELECT * FROM \"{tableName}\";";
                var table = db.ExecuteQuery(query);

                loadedTables[tableName] = table;
                currentTable = table;

                DataTableGrid.Columns.Clear();

                foreach (DataColumn column in table.Columns)
                {
                    // Скрываем колонку "id" для НЕ-администратора
                    if (column.ColumnName == "id" && userRole != "администратор")
                        continue;

                    var gridCol = new DataGridTextColumn
                    {
                        Header = columnDisplayNames.ContainsKey(column.ColumnName) ? columnDisplayNames[column.ColumnName] : column.ColumnName,
                        Binding = new Binding($"[{column.ColumnName}]")
                    };

                    DataTableGrid.Columns.Add(gridCol);
                }
                foreach (var column in DataTableGrid.Columns)
                {
                    if (column is DataGridTextColumn textColumn)
                        textColumn.CanUserSort = false;
                }
                DataTableGrid.ItemsSource = currentTable.DefaultView;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке таблицы: {ex.Message}");
            }
        }

        private void AddRow_Click(object sender, RoutedEventArgs e)
        {
            if (currentTable != null)
            {
                SaveTableSnapshot();
                currentTable.Rows.Add(currentTable.NewRow());
            }

        }

        private void DeleteRow_Click(object sender, RoutedEventArgs e)
        {


            if (DataTableGrid.SelectedItem != null)
            {
                SaveTableSnapshot();
                (DataTableGrid.SelectedItem as DataRowView)?.Row.Delete();
            }
        }
        private void SaveTableSnapshot()
        {
            if (currentTable != null && selectedTable != null)
            {
                if (!undoStacks.ContainsKey(selectedTable))
                    undoStacks[selectedTable] = new Stack<DataTable>();

                undoStacks[selectedTable].Push(currentTable.Copy());
            }
        }
        private void SaveChanges_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (var conn = db.GetConnection())
                {
                    conn.Open();

                    var existingTables = new HashSet<string>();

                    // Получаем список существующих таблиц в текущей схеме (обычно public)
                    using (var checkCmd = new NpgsqlCommand("SELECT table_name FROM information_schema.tables WHERE table_schema = 'public'", conn))
                    using (var reader = checkCmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            existingTables.Add(reader.GetString(0));
                        }
                    }

                    foreach (var tableName in loadedTables.Keys.ToList()) // <-- .ToList() спасает
                    {
                        DataTable table = loadedTables[tableName];

                        if (!existingTables.Contains(tableName))
                            continue;

                        var adapter = new NpgsqlDataAdapter($"SELECT * FROM \"{tableName}\"", conn);
                        var builder = new NpgsqlCommandBuilder(adapter);

                        adapter.Update(table);

                        // Обновляем данные из базы
                        string refreshQuery = $"SELECT * FROM \"{tableName}\";";
                        var refreshedTable = db.ExecuteQuery(refreshQuery);
                        loadedTables[tableName] = refreshedTable;

                        if (selectedTable == tableName)
                            DataTableGrid.ItemsSource = refreshedTable.DefaultView;
                    }
                }

                MessageBox.Show("Все изменения во всех загруженных таблицах сохранены.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении: {ex.Message}");
            }
        }

        private void CreateTableButton_Click(object sender, RoutedEventArgs e)
        {

            var dialog = new InputDialog();
            bool? result = dialog.ShowDialog();

            if (result == true)
            {
                string realName = dialog.RealName;
                string displayName = dialog.DisplayName;

                if (string.IsNullOrWhiteSpace(realName) || string.IsNullOrWhiteSpace(displayName))
                {
                    MessageBox.Show("Заполните оба поля!");
                    return;
                }

                try
                {
                    var con = db.GetConnection();
                    con.Open();

                    string query = $"CREATE TABLE IF NOT EXISTS \"{realName}\" (id SERIAL PRIMARY KEY)";
                    using (var cmd = new NpgsqlCommand(query, con))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    // Добавление в отображаемую коллекцию
                    TablesList.Items.Add(new ListBoxItem
                    {
                        Content = displayName,
                        Tag = realName
                    });

                    tableNameDisplayMap[realName] = displayName;

                    MessageBox.Show($"Таблица '{displayName}' создана");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка при создании таблицы: " + ex.Message);
                }
            }
        }


        private void DeleteTable_Click(object sender, RoutedEventArgs e)
        {
            if (TablesList.SelectedItem is ListBoxItem selectedItem)
            {
                string tableName = selectedItem.Tag.ToString();

                var result = MessageBox.Show($"Удалить таблицу \"{tableName}\"?", "Подтверждение", MessageBoxButton.YesNo);
                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        string query = $"DROP TABLE IF EXISTS \"{tableName}\";";
                        db.ExecuteQuery(query);


                        TablesList.Items.Remove(selectedItem); // Удалить из списка
                        DataTableGrid.Columns.Clear();
                        DataTableGrid.ItemsSource = null;
                        selectedTable = null;

                        MessageBox.Show("Таблица удалена.");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Ошибка при удалении таблицы: " + ex.Message);
                    }
                }
            }
            else
            {
                MessageBox.Show("Сначала выберите таблицу для удаления.");
            }
        }

        private void AddColumn_Click(object sender, RoutedEventArgs e)
        {
            if (userRole != "администратор")
            {
                MessageBox.Show("Только администратор может добавлять столбцы.");
                return;
            }

            if (TablesList.SelectedItem == null)
            {
                MessageBox.Show("Сначала выберите таблицу.");
                return;
            }

  

            // 🔧 Исправлено здесь
            string tableName;
            if (TablesList.SelectedItem is ListBoxItem selectedItem && selectedItem.Tag is string tag)
            {
                tableName = tag;
            }
            else
            {
                MessageBox.Show("Не удалось получить имя таблицы.");
                return;
            }

            var dialog = new AddColumnDialog { Owner = this };
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    using (var conn = db.GetConnection())
                    {
                        conn.Open();
                        string query = $"ALTER TABLE \"{tableName}\" ADD COLUMN \"{dialog.ColumnName}\" {dialog.DataType};";
                        using (var cmd = new NpgsqlCommand(query, conn))
                        {
                            cmd.ExecuteNonQuery();
                        }
                    }

                    MessageBox.Show("Столбец успешно добавлен.");
                    LoadTable(tableName); // Обновить таблицу
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при добавлении столбца: {ex.Message}");
                }
            }
        }


        private void DeleteColumn_Click(object sender, RoutedEventArgs e)
        {
            if(userRole == "администратор") {
            if (selectedTable == null)
            {
                MessageBox.Show("Сначала выберите таблицу.");
                return;
            }

            if (currentTable == null)
            {
                MessageBox.Show("Нет загруженной таблицы.");
                return;
            }

            // Открываем диалог выбора столбца
            var columnNames = new List<string>();
            foreach (DataColumn col in currentTable.Columns)
            {
                columnNames.Add(col.ColumnName);
            }

            var dialog = new SelectColumnDialog(columnNames) { Owner = this }; // тебе нужно создать этот диалог
                if (dialog.ShowDialog() == true)
                {
                    string columnName = dialog.SelectedColumn;

                    // Подтверждение
                    var confirm = MessageBox.Show($"Удалить столбец '{columnName}' из таблицы '{selectedTable}'?",
                                                  "Подтверждение",
                                                  MessageBoxButton.YesNo,
                                                  MessageBoxImage.Warning);

                    if (confirm == MessageBoxResult.Yes)
                    {
                        try
                        {
                            using (var conn = db.GetConnection())
                            {
                                conn.Open();
                                string query = $"ALTER TABLE \"{selectedTable}\" DROP COLUMN \"{columnName}\";";
                                using (var cmd = new NpgsqlCommand(query, conn))
                                {
                                    cmd.ExecuteNonQuery();
                                }
                            }

                            MessageBox.Show($"Столбец '{columnName}' удалён.");
                            LoadTable(selectedTable); // Обновить таблицу
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Ошибка при удалении столбца: {ex.Message}");
                        }
                    }
                }
                else
                {
                    MessageBox.Show($"у {userRole} нет прав на добавление столбца");
                }
            }
        }

        private void CorectColumn_Click(object sender, RoutedEventArgs e)
        {
            if (userRole != "администратор")
            {
                MessageBox.Show("Редактирование столбцов доступно только админу.");
                return;
            }

            if (selectedTable == null || currentTable == null)
            {
                MessageBox.Show("Выберите таблицу.");
                return;
            }

            var columnNames = new List<string>();
            foreach (DataColumn col in currentTable.Columns)
                columnNames.Add(col.ColumnName);

            var dialog = new EditColumnDialog(columnNames) { Owner = this };
            if (dialog.ShowDialog() == true)
            {
                string oldName = dialog.SelectedColumn;
                string newName = dialog.NewName;
                string newType = dialog.NewType;

                try
                {
                    using (var conn = db.GetConnection())
                    {
                        conn.Open();

                        var cmd1 = new NpgsqlCommand($"ALTER TABLE \"{selectedTable}\" RENAME COLUMN \"{oldName}\" TO \"{newName}\";", conn);
                        cmd1.ExecuteNonQuery();

                        var cmd2 = new NpgsqlCommand($"ALTER TABLE \"{selectedTable}\" ALTER COLUMN \"{newName}\" TYPE {newType};", conn);
                        cmd2.ExecuteNonQuery();
                    }

                    MessageBox.Show("Столбец обновлён.");
                    LoadTable(selectedTable); // Обновить таблицу
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка при изменении столбца: " + ex.Message);
                }
            }
        }
        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (currentTable == null)
                return;

            string filterText = SearchBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(filterText))
            {
                DataTableGrid.ItemsSource = currentTable.DefaultView;
                return;
            }

            try
            {
                // Собираем условия фильтра для всех столбцов
                var filterConditions = new List<string>();
                foreach (DataColumn column in currentTable.Columns)
                {
                    if (column.DataType == typeof(string) || column.DataType == typeof(int))
                    {
                        filterConditions.Add($"CONVERT([{column.ColumnName}], 'System.String') LIKE '%{filterText}%'");
                    }
                }

                string filterExpression = string.Join(" OR ", filterConditions);
                currentTable.DefaultView.RowFilter = filterExpression;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при фильтрации: " + ex.Message);
            }
        }

    }

}
