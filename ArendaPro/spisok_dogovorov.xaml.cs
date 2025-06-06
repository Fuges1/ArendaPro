using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.IO;
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
using Word = Microsoft.Office.Interop.Word;
namespace ArendaPro
{
    public class BoolToConfirmCancelButtonConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isActive = (bool)value;
            return isActive ? "Отменить" : "Подтвердить";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value.ToString() == "Отменить";
        }
    }
    /// <summary>
    /// Логика взаимодействия для spisok_dogovorov.xaml
    /// </summary>
    public partial class spisok_dogovorov : Window
    {
        private readonly BD database;
        private readonly string connStr;
        public spisok_dogovorov()
        {
            InitializeComponent();
            connStr = ConfigurationManager.ConnectionStrings["DbConnection"]?.ConnectionString
               ?? throw new Exception("Строка подключения не найдена.");

            database = new BD(connStr);
            LoadContracts();

        }
        private ObservableCollection<ContractInfo> allContracts = new();
       

        public class ContractInfo
        {
            public int ContractId { get; set; }
            public string FilePath { get; set; }
            public string Familia { get; set; }
            public string Imia { get; set; }
            public string Otchestvo { get; set; }
            public string StatusDescription { get; set; }
            public bool IsActive { get; set; }

            public string FullName
            {
                get
                {
                    return $"{Familia} {Imia} {Otchestvo}";
                }
            }
        }

        private void LoadContracts()
        {
            var dataTable = database.ExecuteQuery(@"
    SELECT 
        contracts.id AS contract_id,
        contracts.contract_number,
        contracts.creation_date,
        contracts.file_path,
        clients.familia,
        clients.imia,
        clients.otchestvo,
        cars.marka || ' ' || cars.gos_nomer AS car_info,
        contracts.price,
        contract_statuses.description AS status_description,
        contracts.status
    FROM contracts
    JOIN clients ON contracts.client_id = clients.id
    JOIN cars ON contracts.car_id = cars.id
    LEFT JOIN contract_statuses ON contracts.status = contract_statuses.code
    ORDER BY contracts.contract_number::int ASC;
    ");

            allContracts.Clear();
            foreach (System.Data.DataRow row in dataTable.Rows)
            {
                var status = row["status"].ToString();
                allContracts.Add(new ContractInfo
                {
                    ContractId = Convert.ToInt32(row["contract_id"]),
                    FilePath = row["file_path"]?.ToString() ?? "",
                    Familia = row["familia"]?.ToString() ?? "",
                    Imia = row["imia"]?.ToString() ?? "",
                    Otchestvo = row["otchestvo"]?.ToString() ?? "",
                    StatusDescription = row["status_description"]?.ToString() ?? "Неизвестно",
                    IsActive = status == "active"
                });
            }

            ContractsGrid.ItemsSource = allContracts;
        }
        private void ToggleContractStatus_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is ContractInfo contract)
            {
                string newStatus = contract.IsActive ? "inactive" : "active";
                string actionText = contract.IsActive ? "Отменить" : "Подтвердить";

                var result = MessageBox.Show(
                    $"Вы хотите {actionText.ToLower()} аренду?",
                    "Подтверждение действия",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        // Обновляем статус договора
                        database.ExecuteNonQuery(
                            "UPDATE contracts SET status = @status WHERE id = @id",
                            new Dictionary<string, object> { { "@status", newStatus }, { "@id", contract.ContractId } }
                        );

                        // Обновляем данные в таблице
                        LoadContracts();
                        MessageBox.Show("Статус изменён.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Ошибка при изменении статуса:\n" + ex.Message);
                    }
                }
            }
        }

        private void OpenFile_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is ContractInfo contract)
            {
                // Получаем абсолютный путь
                string fullPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, contract.FilePath);
                if (File.Exists(fullPath))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = fullPath,
                        UseShellExecute = true
                    });
                }
                else
                {
                    MessageBox.Show("Файл не найден:\n" + fullPath);
                }
            }
        }
        private void SearchBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (SearchBox.Text == "Введите запрос")
            {
                SearchBox.Text = "";
                SearchBox.Foreground = Brushes.Black;
            }
        }
        private void SearchBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(SearchBox.Text))
            {
                SearchBox.Text = "Введите запрос";
                SearchBox.Foreground = Brushes.Gray;
            }
        }
        private void PrintFile_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is ContractInfo contract)
            {
                string fullPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, contract.FilePath);

                if (File.Exists(fullPath))
                {
                    try
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = fullPath,
                            UseShellExecute = true,
                            Verb = "Print" // 👈 запускаем печать через ассоциированную программу (Word)
                        });
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Ошибка при печати:\n" + ex.Message);
                    }
                }
                else
                {
                    MessageBox.Show("Файл не найден:\n" + fullPath);
                }
            }
        }
        private void CancelConfirm_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is ContractInfo contract)
            {
                var result = MessageBox.Show(
                    "Вы уверены, что хотите отменить подтверждение договора?",
                    "Отмена подтверждения",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        // Обновляем статус договора на 'pending' (или другой статус по умолчанию)
                        database.ExecuteNonQuery(
                            "UPDATE contracts SET status = 'pending' WHERE id = @id",
                            new Dictionary<string, object> { { "@id", contract.ContractId } }
                        );

                        MessageBox.Show("Подтверждение договора отменено.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);

                        // Перезагружаем данные, чтобы обновить UI
                        LoadContracts();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Ошибка при отмене подтверждения:\n" + ex.Message);
                    }
                }
            }
        }

        private void ConfirmContract_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is ContractInfo contract)
            {
                var result = MessageBox.Show(
                    "Вы хотите активировать аренду?",
                    "Подтверждение договора",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        // Обновляем статус договора на "active"
                        database.ExecuteNonQuery(
                            "UPDATE contracts SET status = 'active' WHERE id = @id",
                            new Dictionary<string, object> { { "@id", contract.ContractId } }
                        );

                        // Получаем данные для аренды — машину и даты
                        var rentalData = database.ExecuteQuery(
                            @"SELECT car_id, start_date, end_date FROM contracts WHERE id = @id",
                            new Dictionary<string, object> { { "@id", contract.ContractId } }
                        );

                        if (rentalData.Rows.Count == 1)
                        {
                            int carId = Convert.ToInt32(rentalData.Rows[0]["car_id"]);
                            DateTime startDate = Convert.ToDateTime(rentalData.Rows[0]["start_date"]);
                            DateTime endDate = Convert.ToDateTime(rentalData.Rows[0]["end_date"]);

                            // Вставляем занятость машины
                            database.ExecuteNonQuery(
                                @"INSERT INTO car_occupations (car_id, status, start_date, end_date)
                                VALUES (@car_id, 'occupied', @start_date, @end_date);",
                                new Dictionary<string, object>
                                {
                            { "@car_id", carId },
                            { "@start_date", startDate },
                            { "@end_date", endDate }
                                }
                            );
                        }

                        MessageBox.Show("Договор подтверждён и аренда активирована.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                        // Обновляем данные в таблице
                        LoadContracts();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Ошибка при подтверждении договора:\n" + ex.Message);
                    }
                }
            }
        }



        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (ContractsGrid == null || allContracts == null)
                return;

            if (SearchBox.Text == "Введите запрос" || string.IsNullOrWhiteSpace(SearchBox.Text))
            {
                ContractsGrid.ItemsSource = allContracts;
                return;
            }

            string searchText = SearchBox.Text.ToLower();

            var filtered = allContracts
                .Where(c =>
                    c.FullName.ToLower().Contains(searchText) ||
                    c.ContractId.ToString().Contains(searchText) ||
                    (c.FilePath?.ToLower().Contains(searchText) ?? false)
                ).ToList();

            ContractsGrid.ItemsSource = filtered;
        }

    }
}
