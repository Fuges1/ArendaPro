using Npgsql;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace ArendaPro
{
    public partial class Table_BD : Window
    {
        private string _lastSearch = "";
        private Stack<string> ddlUndoStack = new();
        private Dictionary<string, Stack<DataTable>> undoStacks = new();
        private BD db;
        private string selectedTable;
        private DataTable currentTable;
        private string userRole;
        private Dictionary<string, DataTable> loadedTables = new();
        private Window parentWindow;
        private Dictionary<string, string> tableNameDisplayMap = new()
        {
            { "users", "Сотрудники" },
            { "cars", "Автомобили" },
            { "clients", "Клиенты" },
            { "contracts", "Договоры" },
            { "car_tariff_history", "История тарифов" },
            { "car_status_history", "История статусов авто" },
            { "car_owners", "Владельцы авто" },
            { "contract_statuses", "Статусы договоров" },
            { "places", "Места получения/возврата" },
            { "user_roles", "Роли пользователей" },
            { "notifications", "Уведомления" },
            { "car_statuses", "Статусы автомобилей" },
            { "car_occupations", "Занятость автомобилей" },
{ "contracts_docs",      "Файлы договоров" },
{ "contract_reports",      "Отчеты о контрактах" },
{ "payments",              "Оплаты" },
    { "maintenance_schedule",   "План ТО" },
    { "audit_log",              "Журнал действий" },
     { "tariffs",   "Тарифы" }
        };

        private readonly Dictionary<string, string> columnDisplayNames =
    new()
    {
    { "id",                     "ID" },
    { "full_name",              "ФИО клиента" },
    { "phone",                  "Телефон" },
    { "email",                  "Эл. почта" },
    { "username",               "Логин" },
    { "password",               "Пароль" },
    { "role",                   "Роль" },
    { "status",                 "Статус" },
    { "code",                   "Код" },
    { "name",                   "Название" },

    { "brand",                  "Марка" },
    { "model",                  "Модель" },
    { "license_plate",          "Гос. номер" },
    { "marka",                  "Марка автомобиля" },
    { "gos_nomer",              "Гос. номер" },
    { "vin",                    "VIN" },
    { "registr_svidetelstva",   "Рег. свидетельство" },
    { "cvet",                   "Цвет" },
    { "god_vipuska",            "Год выпуска" },
    { "pts",                    "ПТС" },
    { "owner_id",               "Владелец" },
    { "car_id",                 "Автомобиль" },
{ "car_model",   "Модель авто" },
    { "familia_vladelca",       "Фамилия владельца" },
    { "imia_vladelca",          "Имя владельца" },
    { "otchestvo_vladelca",     "Отчество владельца" },
    { "owner_name",     "Владельцы" },
    { "familia",                "Фамилия клиента" },
    { "imia",                   "Имя клиента" },
    { "otchestvo",              "Отчество клиента" },
    { "pasport",                "Паспорт" },
    { "kem_vydan_pasport",      "Кем выдан паспорт" },
    { "data_vydachi_pasporta",  "Дата выдачи паспорта" },
    { "adres_prozhivaniya",     "Адрес проживания" },
    { "telefon",                "Телефон" },
    { "voditelskoe_udostoverenie","Вод. удостоверение" },
    { "data_vydachi_voditelskogo","Дата выдачи в/у" },
    { "data_rozhdeniya",        "Дата рождения" },

    { "last_name",              "Фамилия" },
    { "first_name",             "Имя" },
    { "middle_name",            "Отчество" },
    { "passport_number",        "Паспорт" },
    { "passport_issued_by",     "Кем выдан паспорт" },
    { "passport_issue_date",    "Дата выдачи паспорта" },

    { "contract_number",        "Номер договора" },
    { "creation_date",          "Дата создания" },
    { "contract_id",            "Договор" },
    { "contractid",             "Договор" },
    { "client_id",              "Клиент" },
    { "place_start_id",         "Место получения" },
    { "place_end_id",           "Место возврата" },
    { "user_id",                "Сотрудник" },
    { "cancel_date",            "Дата отмены" },
    { "canceldate",             "Дата отмены" },
    { "contract_doc_path",       "Путь к файлу" },
    { "contract_doc_type",       "Тип файла" },
    { "return_report_path",      "Отчёт о возврате" },

    { "issue_date",             "Дата выдачи" },
    { "start_date",             "Дата начала" },
    { "end_date",               "Дата окончания" },
    { "returned_at",            "Возврат по адресу" },
    { "birth_date",             "Дата рождения" },
    { "time_start",             "Время начала" },
    { "time_end",               "Время окончания" },
    { "scheduled",              "Запланировано" },
    { "created_at",             "Создано" },
    { "report_date",            "Дата отчёта" },
    { "service_date",           "Дата ТО" },
    { "action_time",             "Время действия" },

    { "price",                  "Цена аренды" },
    { "daily_rate",             "Ставка/день (₽)" },
    { "discount_coef",          "Коэф. скидки" },
    { "extra_services",         "Доп. услуги" },
    { "total_amount",           "Общая сумма" },
    { "extra_amount",            "Доп. сумма" },
    { "paid_amount",             "Оплачено" },
    { "amount",                 "Сумма оплаты" },
    { "is_paid",                "Оплачена" },
    { "ispaid",                 "Оплачена" },
    { "paid_at",                "Оплачено (дата)" },
    { "kkt_receipt_number",      "Чек ККТ" },
    { "pay_type",               "Тип оплаты" },

    { "notify_type",            "Тип уведомления" },
    { "notifytype",             "Тип уведомления" },
    { "sent",                   "Отправлено" },
    { "doc_path",                "Путь к файлу" },
    { "doc_type",                "Тип файла" },
    { "tariff_name", "Тариф" },
    { "action",                 "Действие" },
    { "entity",                 "Сущность" },
    { "entity_id",               "ID записи" },
    { "details",                "Детали" },
    { "status_code",            "Код статуса" },
    { "condition_after",         "Состояние авто" },
    { "early_reason",            "Причина доср. возврата" },
     { "tariff_rate",  "Стоимость аренды" },
    { "description",            "Описание статуса" },
    { "returnedat",             "Возвращено по адресу" },  
    { "доступность",            "Доступность" },

{ "author_id",            "Автор"         }

};
        private readonly Dictionary<string, string[]> preferredOrder = new()
        {

            ["contracts"] = new[]
    {
        "id","contract_number","client_id","car_id",
        "start_date","end_date","returned_at",
        "status","price","extra_amount","totalamount",
        "paid_amount","ispaid","user_id",
        "place_start_id","place_end_id",
        "contract_doc_path","contract_doc_type","return_report_path","canceldate","created_at"
    },
            ["payments"] = new[]
    {
        "id","contract_id","amount","pay_type","kkt_receipt_number","paid_at"
    },
            ["notifications"] = new[]
    {
        "id","contract_id","notifytype","scheduled","sent","created_at"
    },
        };

        public Table_BD(string role, Window parent)
        {
            InitializeComponent();
            string connStr = ConfigurationManager.ConnectionStrings["DbConnection"]?.ConnectionString;
            userRole = role;
            parentWindow = parent;
            this.Closed += Table_BD_Closed;
            db = new BD(connStr);
            AdjustPermissions();

            if (userRole == "менеджер")
            {
                var managerTables = new[] { "cars", "clients", "maintenance_schedule", "contract_reports" };

                foreach (var name in managerTables
                    .OrderBy(n => tableNameDisplayMap.ContainsKey(n) ? tableNameDisplayMap[n] : n))
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

                    var allTableNames = new List<string>();
                    using (var conn = db.GetConnection())
                    {
                        conn.Open();
                        string query = @"
                    SELECT table_name 
                    FROM information_schema.tables 
                    WHERE table_schema = 'public' 
                      AND table_type = 'BASE TABLE';";

                        using (var cmd = new NpgsqlCommand(query, conn))
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                                allTableNames.Add(reader.GetString(0));
                        }
                    }

                    foreach (var name in allTableNames
     .OrderBy(n => tableNameDisplayMap.ContainsKey(n)
                    ? tableNameDisplayMap[n].Trim()
                    : n))
                    {
                        TablesList.Items.Add(new ListBoxItem
                        {
                            Content = tableNameDisplayMap.ContainsKey(name)
                                          ? tableNameDisplayMap[name].Trim()
                                          : name,
                            Tag = name
                        });
                    }

                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка при получении списка таблиц: " + ex.Message);
                }
            }
        }
        private void AdjustPermissions()
        {
            AddRowButton.Visibility =
            DeleteRowButton.Visibility =
            AddClientButton.Visibility = Visibility.Collapsed;
            EditToggleButton.Visibility = Visibility.Collapsed;
            DataTableGrid.IsReadOnly = true;
            foreach (var col in DataTableGrid.Columns)
                if (col is DataGridBoundColumn c) c.IsReadOnly = true;

            if (userRole == "администратор")
            {
                AddRowButton.Visibility =
                DeleteRowButton.Visibility =

                EditToggleButton.Visibility = Visibility.Visible;

                if (selectedTable == "clients")
                    AddClientButton.Visibility = Visibility.Visible;

                return;
            }

            if (selectedTable == "clients")
            {
                AddClientButton.Visibility = Visibility.Visible;
                EditToggleButton.Visibility = Visibility.Visible;

                DataTableGrid.IsReadOnly = false;
                foreach (var col in DataTableGrid.Columns)
                    if (col is DataGridBoundColumn c) c.IsReadOnly = false;
            }
            else if (selectedTable == "cars")
            {
                DataTableGrid.IsReadOnly = false;
                foreach (var col in DataTableGrid.Columns)
                {
                    if (col is DataGridBoundColumn c)
                    {
                        var name = (c.Binding as Binding)?.Path?.Path?.Trim('[', ']');
                        c.IsReadOnly = !string.Equals(name, "status",
                                                      StringComparison.OrdinalIgnoreCase);
                    }
                }
            }
        }
        private void GenerateReport_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Формирование отчёта пока не реализовано.",
                            "Отчёт",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
        }
        private void AddClientButton_Click(object sender, RoutedEventArgs e)
        {
            if (selectedTable != "clients") return;

            var addWindow = new ClientAdd(currentTable);
            bool? result = addWindow.ShowDialog();
            if (result != true) return;

            string newPassport = addWindow.NewClientRow["pasport"].ToString();
            string newLicense = addWindow.NewClientRow["voditelskoe_udostoverenie"].ToString();
            string newPhoneRaw = addWindow.NewClientRow["telefon"].ToString();

            string newPhone = NormalizePhone(newPhoneRaw);

            bool existsPassport = currentTable.Select($"pasport = '{newPassport}'").Any();
            bool existsLicense = currentTable.Select($"voditelskoe_udostoverenie = '{newLicense}'").Any();

            bool existsPhone = currentTable.AsEnumerable()
                .Select(r => NormalizePhone(r.Field<string>("telefon")))
                .Contains(newPhone);

            if (existsPassport)
            {
                MessageBox.Show("Клиент с таким паспортом уже существует.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (existsLicense)
            {
                MessageBox.Show("Клиент с таким ВУ уже существует.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (existsPhone)
            {
                MessageBox.Show("Клиент с таким номером телефона уже существует.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            SaveTableSnapshot();
            currentTable.Rows.Add(addWindow.NewClientRow);
        }
        private string NormalizePhone(string raw)
        {
            string digits = Regex.Replace(raw, @"\D", "");
            if (digits.Length == 11 && digits.StartsWith("8"))
                digits = "7" + digits.Substring(1);
            return digits;
        }

        private bool _isEditMode = false;

        private void EditToggleButton_Click(object sender, RoutedEventArgs e)
        {
            _isEditMode = !_isEditMode;

            DataTableGrid.IsReadOnly = !_isEditMode;
            foreach (var col in DataTableGrid.Columns)
                if (col is DataGridBoundColumn c) c.IsReadOnly = !_isEditMode;

            EditToggleButton.Content = _isEditMode ? "Просмотр" : "Изменить данные";
        }

        private void EditRow_Click(object sender, RoutedEventArgs e)
        {
            if (DataTableGrid.SelectedItem == null)
            {
                MessageBox.Show("Сначала выберите строку для редактирования.");
                return;
            }

            DataTableGrid.BeginEdit();
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
                PopulateFilterControls(currentTable);

            }
            else
            {
                MessageBox.Show("Нет изменений для отмены.");
            }
        }
        private void PopulateFilterControls(DataTable table)
        {
            FilterColumnComboBox.Items.Clear();
            FilterConditionComboBox.SelectedIndex = 0;
            FilterValueBox.Clear();

            foreach (DataColumn col in table.Columns)
            {
                FilterColumnComboBox.Items.Add(new ComboBoxItem
                {
                    Content = columnDisplayNames.ContainsKey(col.ColumnName)
                              ? columnDisplayNames[col.ColumnName]
                              : col.ColumnName,
                    Tag = col.ColumnName
                });
            }

            if (FilterColumnComboBox.Items.Count > 0)
                FilterColumnComboBox.SelectedIndex = 0;
        }
        private void ApplyFilterButton_Click(object sender, RoutedEventArgs e)
        {
            if (currentTable == null) return;

            if (!(FilterColumnComboBox.SelectedItem is ComboBoxItem cbItem))
                return;
            string column = cbItem.Tag.ToString();      
            string cond = (FilterConditionComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString();
            string val = FilterValueBox.Text.Trim();
            if (string.IsNullOrEmpty(val)) return;

            string expr = cond switch
            {
                "=" => $"[{column}] = '{val}'",
                "≠" => $"[{column}] <> '{val}'",
                "⩾" => $"[{column}] >= '{val}'",
                "⩽" => $"[{column}] <= '{val}'",
                "Содержит" => $"CONVERT([{column}], 'System.String') LIKE '%{val}%'",
                _ => ""
            };

            currentTable.DefaultView.RowFilter = expr;
        }
        private bool IsCarAvailable(int carId, DateTime startDate, DateTime endDate)
        {
            try
            {
                using (var conn = db.GetConnection())
                {
                    conn.Open();
                    string query = @"
                SELECT COUNT(*) FROM car_occupations 
                WHERE car_id = @carId 
                AND status = 'occupied'
                AND (
                    (start_date <= @endDate AND end_date >= @startDate) OR
                    (start_date BETWEEN @startDate AND @endDate) OR
                    (end_date BETWEEN @startDate AND @endDate)
                )";

                    using (var cmd = new NpgsqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@carId", carId);
                        cmd.Parameters.AddWithValue("@startDate", startDate);
                        cmd.Parameters.AddWithValue("@endDate", endDate);

                        int overlappingRentals = Convert.ToInt32(cmd.ExecuteScalar());
                        return overlappingRentals == 0;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при проверке доступности автомобиля: {ex.Message}");
                return false;
            }
        }


        private void ClearFilterButton_Click(object sender, RoutedEventArgs e)
        {
            if (currentTable != null) currentTable.DefaultView.RowFilter = string.Empty;
            FilterValueBox.Clear();
        }
        private void Table_BD_Closed(object sender, EventArgs e)
        {
            parentWindow.Show(); 
        }
        private void TablesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TablesList.SelectedItem is ListBoxItem item && item.Tag is string realTableName)
            {
                selectedTable = realTableName;
                if (userRole == "администратор" && realTableName == "users")
                {
                    RegisterEmployeeButton.Visibility = Visibility.Visible;
                }
                else
                {
                    RegisterEmployeeButton.Visibility = Visibility.Collapsed;
                }
                LoadTable(selectedTable);
                AdjustPermissions();
            }
        }
        private void RegisterEmployeeButton_Click(object sender, RoutedEventArgs e)
        {
            var registrationWindow = new RegisterUserWindow(); 
            registrationWindow.ShowDialog();

            if (selectedTable == "users")
            {
                LoadTable("users");
            }
        }
        private void LoadTable(string tableName)
        {
            try
            {
                DataTable table;

                if (tableName.Equals("contract_reports", StringComparison.OrdinalIgnoreCase))
                {
                    table = db.ExecuteQuery(@"
SELECT 
    r.id,
    c.contract_number AS contract,
    r.report_date,
    r.condition_after AS car_state,
    r.early_reason AS early_reason,
    u.last_name || ' ' || u.first_name || ' ' || u.middle_name AS author
FROM public.contract_reports r
JOIN public.contracts c ON c.id = r.contract_id
JOIN public.users u     ON u.id = r.author_id
ORDER BY r.report_date DESC;
");
                    DataTableGrid.Columns.Clear();

                    DataTableGrid.Columns.Add(new DataGridTextColumn
                    {
                        Header = "ID",
                        Binding = new Binding("[id]"),
                        Width = 40
                    });
                    DataTableGrid.Columns.Add(new DataGridTextColumn
                    {
                        Header = "Договор",
                        Binding = new Binding("[contract]"),
                        Width = 80
                    });
                    DataTableGrid.Columns.Add(new DataGridTextColumn
                    {
                        Header = "Дата отчёта",
                        Binding = new Binding("[report_date]") { StringFormat = "dd.MM.yyyy HH:mm" },
                        Width = 120
                    });
                    DataTableGrid.Columns.Add(new DataGridTextColumn
                    {
                        Header = "Состояние авто",
                        Binding = new Binding("[car_state]"),
                        Width = 200
                    });
                    DataTableGrid.Columns.Add(new DataGridTextColumn
                    {
                        Header = "Причина доср. возврата",
                        Binding = new Binding("[early_reason]"),
                        Width = 200
                    });
                    DataTableGrid.Columns.Add(new DataGridTextColumn
                    {
                        Header = "Автор",
                        Binding = new Binding("[author]"),
                        Width = 150
                    });

                    DataTableGrid.ItemsSource = table.DefaultView;
                    return;
                }

                DataTableGrid.AutoGenerateColumns = false;
                CheckAvailabilityButton.Visibility = Visibility.Collapsed;

                if (tableName == "cars")
                {
                    if (userRole == "администратор")
                    {
                        table = db.ExecuteQuery(@"
                    SELECT c.id, c.marka, c.gos_nomer, c.vin, c.status,
                           c.registr_svidetelstva, c.cvet, c.god_vipuska, c.pts,
                           c.owner_id, c.tariff_id
                    FROM public.cars c;");
                    }
                    else
                    {
                        table = db.ExecuteQuery(@"
                    SELECT 
                        c.id,
                        c.marka,
                        c.gos_nomer,
                        c.vin,
                        c.status,
                        c.registr_svidetelstva,
                        c.cvet,
                        c.god_vipuska,
                        c.pts,
                        co.last_name || ' ' || co.first_name || ' ' || co.middle_name AS owner_name,
                        t.daily_rate AS tariff_rate
                    FROM public.cars c
                    LEFT JOIN public.car_owners co ON c.owner_id = co.id
                    LEFT JOIN public.tariffs t     ON c.tariff_id = t.id;");
                    }
                }
                else if (tableName == "tariffs")
                {
                    table = db.ExecuteQuery(@"
                SELECT id, name, daily_rate
                FROM public.tariffs;");
                }
                else
                {
                    table = db.ExecuteQuery($"SELECT * FROM public.\"{tableName}\";");
                }

                if (tableName == "cars")
                {
                    CheckAvailabilityButton.Visibility = Visibility.Visible;
                    var availableColumn = new DataColumn("Доступность", typeof(string));
                    table.Columns.Add(availableColumn);

                    foreach (DataRow row in table.Rows)
                    {
                        var status = row["status"].ToString().ToLowerInvariant();
                        if (status == "rented" || status == "в аренде")
                            row["Доступность"] = "Недоступен (в аренде)";
                        else if (status == "available" || status == "доступен")
                            row["Доступность"] = "Доступен";
                        else
                            row["Доступность"] = row["status"];
                    }
                }

                loadedTables[tableName] = table;
                currentTable = table;
                PopulateFilterControls(table);
                if (!string.IsNullOrWhiteSpace(_lastSearch))
                    SearchBox_TextChanged(null, null);

                DataTableGrid.Columns.Clear();
                foreach (DataColumn column in table.Columns)
                {
                    if (column.ColumnName.Equals("id", StringComparison.OrdinalIgnoreCase))
                        continue;
                    var binding = new Binding($"[{column.ColumnName}]");

                    if (column.DataType == typeof(DateTime))
                    {
                        bool showTime;

                        if (string.Equals(tableName, "payments", StringComparison.OrdinalIgnoreCase))
                        {
                            showTime = true;
                        }
                        else
                        {
                            var name = column.ColumnName.ToLowerInvariant();
                            showTime = name.Contains("time")
                                    || name.Contains("_at")
                                    || name.Contains("start")
                                    || name.Contains("end");
                        }

                        if (!showTime)
                        {
                            binding.StringFormat = "dd.MM.yyyy";
                        }
                    }

                    var gridCol = new DataGridTextColumn
                    {
                        Header = columnDisplayNames.TryGetValue(column.ColumnName, out var h)
                 ? h
                 : column.ColumnName,
                        Binding = binding,
                        CanUserSort = false
                    };
                    DataTableGrid.Columns.Add(gridCol);
                }

                if (tableName == "cars" && preferredOrder.TryGetValue(tableName, out var order))
                {
                    foreach (var item in order.Select((col, idx) => new { col, idx }))
                    {
                        var dgCol = DataTableGrid.Columns
                            .OfType<DataGridTextColumn>()
                            .FirstOrDefault(c =>
                                (c.Binding as Binding)?.Path.Path.Trim('[', ']') == item.col);
                        if (dgCol != null)
                            dgCol.DisplayIndex = item.idx;
                    }
                }

                if (!string.IsNullOrWhiteSpace(_lastSearch))
                    SearchBox_TextChanged(null, null);

                DataTableGrid.ItemsSource = table.DefaultView;
                AdjustPermissions();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке таблицы: {ex.Message}",
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }



        private void CheckAvailability_Click(object sender, RoutedEventArgs e)
        {
            if (DataTableGrid.SelectedItem == null)
            {
                MessageBox.Show("Выберите автомобиль для проверки доступности");
                return;
            }

            var dialog = new DateRangeDialog { Owner = this };
            if (dialog.ShowDialog() == true)
            {
                DataRowView selectedRow = (DataRowView)DataTableGrid.SelectedItem;
                int carId = Convert.ToInt32(selectedRow["id"]);

                bool isAvailable = IsCarAvailable(carId, dialog.StartDate, dialog.EndDate);

                string carInfo = $"{selectedRow["marka"]} ({selectedRow["gos_nomer"]})";
                string periodInfo = $"{dialog.StartDate:d} - {dialog.EndDate:d}";

                if (isAvailable)
                {
                    MessageBox.Show($"Автомобиль {carInfo} доступен для аренды в период {periodInfo}",
                                  "Доступность",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show($"Автомобиль {carInfo} занят в период {periodInfo}",
                                  "Доступность",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Warning);
                }
            }
        }
        private void AddRow_Click(object sender, RoutedEventArgs e)
        {
            if (currentTable != null)
            {
                SaveTableSnapshot();
                DataRow newRow = currentTable.NewRow();

                if (selectedTable == "rentals" || selectedTable == "contracts")
                {
                    try
                    {
                        int carId = Convert.ToInt32(newRow["car_id"]);
                        DateTime startDate = Convert.ToDateTime(newRow["start_date"]);
                        DateTime endDate = Convert.ToDateTime(newRow["end_date"]);

                        if (!IsCarAvailable(carId, startDate, endDate))
                        {
                            MessageBox.Show("Этот автомобиль уже занят в выбранный период!");
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при проверке доступности: {ex.Message}");
                        return;
                    }
                }
                if (selectedTable == "cars")
                {
                    int defaultTariffId;
                    using (var conn = db.GetConnection())
                    {
                        conn.Open();
                        using (var cmd = new NpgsqlCommand("SELECT id FROM public.tariffs ORDER BY id LIMIT 1", conn))
                            defaultTariffId = Convert.ToInt32(cmd.ExecuteScalar());
                    }
                    newRow["tariff_id"] = defaultTariffId;
                }
                currentTable.Rows.Add(newRow);
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

                    using (var checkCmd = new NpgsqlCommand("SELECT table_name FROM information_schema.tables WHERE table_schema = 'public'", conn))
                    using (var reader = checkCmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            existingTables.Add(reader.GetString(0));
                        }
                    }

                    foreach (var tableName in loadedTables.Keys.ToList()) 
                    {
                        DataTable table = loadedTables[tableName];

                        if (!existingTables.Contains(tableName))
                            continue;

                        var adapter = new NpgsqlDataAdapter($"SELECT * FROM \"{tableName}\"", conn);
                        var builder = new NpgsqlCommandBuilder(adapter);

                        adapter.Update(table);

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


                        TablesList.Items.Remove(selectedItem); 
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
                    LoadTable(tableName); 
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при добавлении столбца: {ex.Message}");
                }
            }
        }


        private void DeleteColumn_Click(object sender, RoutedEventArgs e)
        {
            if (userRole == "администратор")
            {
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

                var columnNames = new List<string>();
                foreach (DataColumn col in currentTable.Columns)
                {
                    columnNames.Add(col.ColumnName);
                }

                var dialog = new SelectColumnDialog(columnNames) { Owner = this }; 
                if (dialog.ShowDialog() == true)
                {
                    string columnName = dialog.SelectedColumn;

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
                            LoadTable(selectedTable); 
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
                    LoadTable(selectedTable); 
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка при изменении столбца: " + ex.Message);
                }
            }
        }
        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _lastSearch = SearchBox.Text.Trim();
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

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }

}
