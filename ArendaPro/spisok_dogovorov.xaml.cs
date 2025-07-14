using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using Word = Microsoft.Office.Interop.Word;
namespace ArendaPro
{
    public class StringToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => string.IsNullOrWhiteSpace(value as string)
                ? Visibility.Collapsed
                : Visibility.Visible;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
    public class StatusToDeleteVisibilityConverter : IValueConverter
    {
        public string NameFull;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value as string) == "not_confirmed"
        ? Visibility.Visible
        : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class StatusToCompleteVisibilityConverter : IValueConverter
    {
   
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value as string) == "active"
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    public class BoolToBrushConverter : IValueConverter
    {
        public static BoolToBrushConverter Instance { get; } = new BoolToBrushConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)value ? Brushes.LightCoral : Brushes.LightGreen;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class BoolToConfirmCancelButtonConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isActive = (bool)value;
            return isActive ? "Возврат" : "Подтвердить";

        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value.ToString() == "Отменить";
        }
    }
    public class StatusToToggleVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string status = value as string;
            return status == "not_confirmed"
               ? Visibility.Visible
               : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class AdminAndStatusToVisibilityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type t, object p, CultureInfo c)
        {
            bool isAdmin = values.Length > 0 && values[0] is bool b && b;
            string status = values.Length > 1 ? values[1]?.ToString() : "";

            if (!isAdmin) return Visibility.Collapsed;

            return (status == "not_confirmed" || status == "cancelled")
                   ? Visibility.Visible
                   : Visibility.Collapsed;
        }
        public object[] ConvertBack(object v, Type[] tt, object p, CultureInfo c)
            => throw new NotImplementedException();
    }
    public class FileNameConverter : IValueConverter
    {
     
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var path = value as string;
            return string.IsNullOrEmpty(path)
                ? ""
                : System.IO.Path.GetFileName(path);
        }

      
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
    public partial class spisok_dogovorov : Window
    {
        private readonly BD database;
        private readonly string connStr;
        private ObservableCollection<ContractInfo> allContracts = new();
        private string role;
        public class ContractInfo
        {

            public int UserId { get; set; }

            public decimal Price { get; set; }
            public int CarId { get; set; }            
            public TimeSpan TimeEnd { get; set; }            
            public string ReturnReportPath { get; set; }

            public decimal ExtraAmount { get; set; }
            public decimal PaidAmount { get; set; }
            public int RentalDays
     => (EndDate.Date - StartDate.Date).Days + 1;

            public decimal TotalAmount
                => Price * RentalDays;
            public bool IsPaid => DebtAmount <= 0;

            public bool CanReturn => Status == "active";           
            public bool CanProlong => Status == "active";           
            public string Status { get; set; }
            public int ContractId { get; set; }
            public string FilePath { get; set; }
            public string Familia { get; set; }
            public string Imia { get; set; }
            public string Otchestvo { get; set; }
            public string StatusDescription { get; set; }
            public bool IsActive { get; set; }
            public string CarInfo { get; set; }
            public DateTime CreationDate { get; set; }
            public string PlaceStart { get; set; }
            public string PlaceEnd { get; set; }
            public DateTime StartDate { get; set; }
            public DateTime EndDate { get; set; }
            public DateTime? CancelDate { get; set; }
            public string RentalPeriod => $"{StartDate:dd.MM.yyyy} - {EndDate:dd.MM.yyyy}";
            public string StatusDisplay => Status switch
            {
                "active" => "Активен",
                "cancelled" => "Отменён",
                "not_confirmed" => "Не подтверждён",
                "completed" => "Завершён",
                _ => Status
            };
      
            public decimal BaseAmount => Price;

        
            public decimal TotalAmountFull => Price * RentalDays + ExtraAmount;

    
            public decimal PaidAmountFull => PaidAmount;

      
            public decimal DebtAmount => TotalAmountFull - PaidAmountFull;
            public string FullName => $"{Familia} {Imia} {Otchestvo}";
        }

        public bool IsAdmin { get; private set; }

        private readonly string currentUserName;
        public spisok_dogovorov(string userRole, string userName)
        {
            InitializeComponent();
            currentUserName = userName;
            FilterColumnBox.Items.Add(new ComboBoxItem { Content = "№ договора", Tag = nameof(ContractInfo.ContractId) });
            FilterColumnBox.Items.Add(new ComboBoxItem { Content = "ФИО клиента", Tag = nameof(ContractInfo.FullName) });
            FilterColumnBox.Items.Add(new ComboBoxItem { Content = "Автомобиль", Tag = nameof(ContractInfo.CarInfo) });
            FilterColumnBox.Items.Add(new ComboBoxItem { Content = "Статус", Tag = nameof(ContractInfo.StatusDescription) });
            FilterColumnBox.Items.Add(new ComboBoxItem { Content = "К оплате", Tag = nameof(ContractInfo.DebtAmount) });

            FilterColumnBox.SelectedIndex = 0;
            FilterOpBox.SelectedIndex = 0;

            IsAdmin = userRole == "администратор";
            FileColumn.Visibility = IsAdmin
    ? Visibility.Visible
    : Visibility.Collapsed;
            connStr = ConfigurationManager
                         .ConnectionStrings["DbConnection"]?.ConnectionString
                      ?? throw new Exception("Строка подключения не найдена.");
            role = userRole;
            database = new BD(connStr);
            MarkExpiredContractsAsCompleted();
            LoadContracts();
        }
        private static bool Compare(ContractInfo c, string col, string op, string val)
        {
            object v = typeof(ContractInfo).GetProperty(col)?.GetValue(c);
            if (v == null) return false;

            switch (op)
            {
                case "=": return v.ToString().Equals(val, StringComparison.OrdinalIgnoreCase);
                case "≠": return !v.ToString().Equals(val, StringComparison.OrdinalIgnoreCase);
                case "≥": return decimal.TryParse(v.ToString(), out var d1) && d1 >= decimal.Parse(val);
                case "≤": return decimal.TryParse(v.ToString(), out var d2) && d2 <= decimal.Parse(val);
                default: return v.ToString().IndexOf(val, StringComparison.OrdinalIgnoreCase) >= 0;
            }
        }
        public object ConvertBack(object value, Type t, object p, CultureInfo c)
        {
            return value?.ToString() == "Возврат";
        }

        private void LoadContracts()
        {
            try
            {

                database.ExecuteNonQuery("SELECT prolong_overdue();", new Dictionary<string, object>());

                var dataTable = database.ExecuteQuery(@"
           SELECT 
    c.id               AS contract_id,
    c.contract_doc_path AS file_path,  -- <- берём из contracts
    c.contract_number,
    c.creation_date,
    cl.familia,
    cl.imia,
    cl.otchestvo,
    c.return_report_path,
    c.car_id,
    c.time_end,
    c.extra_amount,
    c.paid_amount,
    cr.marka || ' ' || cr.gos_nomer AS car_info,
    c.price,
    c.start_date,
    c.end_date,
    c.cancel_date,
    CASE c.status
        WHEN 'active'        THEN 'Активен'
        WHEN 'cancelled'     THEN 'Отменён'
        WHEN 'not_confirmed' THEN 'Не подтверждён'
        WHEN 'completed'     THEN 'Завершён'
        ELSE c.status
    END AS status_description,
    c.status
FROM contracts c
JOIN clients cl ON c.client_id = cl.id
JOIN cars cr    ON c.car_id    = cr.id
ORDER BY c.id ASC;
        ");

                allContracts.Clear();
                foreach (System.Data.DataRow row in dataTable.Rows)
                {
                    var status = row["status"].ToString();
                    allContracts.Add(new ContractInfo
                    {
                        CarId = Convert.ToInt32(row["car_id"]),
                        TimeEnd = row["time_end"] switch
                        {
                            TimeSpan ts => ts,
                            DateTime dt => dt.TimeOfDay,
                            string s => TimeSpan.Parse(s),
                            _ => TimeSpan.Zero
                        },
                        ExtraAmount = Convert.ToDecimal(row["extra_amount"]),
                        PaidAmount = Convert.ToDecimal(row["paid_amount"]),
                        ContractId = Convert.ToInt32(row["contract_id"]),
                        FilePath = row["file_path"]?.ToString() ?? "",
                        Familia = row["familia"]?.ToString() ?? "",
                        Imia = row["imia"]?.ToString() ?? "",
                        Otchestvo = row["otchestvo"]?.ToString() ?? "",
                        StatusDescription = row["status_description"]?.ToString() ?? "Неизвестно",
                        IsActive = status == "active",
                        CarInfo = row["car_info"]?.ToString() ?? "",
                        Price = Convert.ToDecimal(row["price"]),
                        CreationDate = Convert.ToDateTime(row["creation_date"]),
                        StartDate = Convert.ToDateTime(row["start_date"]),
                        EndDate = Convert.ToDateTime(row["end_date"]),
                        CancelDate = status == "cancelled" && row["cancel_date"] != DBNull.Value
                            ? Convert.ToDateTime(row["cancel_date"])
                            : null,
                        ReturnReportPath = row["return_report_path"]?.ToString() ?? "",     
                        Status = status,

                    });
                }


                ContractsGrid.ItemsSource = allContracts;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при загрузке договоров:\n" + ex.Message,
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void ClearFilter_Click(object sender, RoutedEventArgs e)
        {

            FilterValueBox.Text = "";
            FilterOpBox.SelectedIndex = 0;
            FilterColumnBox.SelectedIndex = 0;
            ContractsGrid.ItemsSource = allContracts;
        }


        private void OpenReport_Click_ochet(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is ContractInfo ci && !string.IsNullOrEmpty(ci.ReturnReportPath))
            {
                var fullPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ci.ReturnReportPath);
                if (File.Exists(fullPath))
                {
                    Process.Start(new ProcessStartInfo(fullPath) { UseShellExecute = true });
                }
                else
                {
                    MessageBox.Show($"Отчёт не найден:\n{fullPath}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        private void ApplyFilter_Click(object s, RoutedEventArgs e)
        {
            var col = FilterColumnBox.SelectedItem as ComboBoxItem;
            if (col == null) return;

            string column = (col.Tag ?? "").ToString();
            string op = (FilterOpBox.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "=";
            string val = FilterValueBox.Text;

            if (string.IsNullOrWhiteSpace(val))
            {
                ContractsGrid.ItemsSource = allContracts;
                return;
            }

            var filtered = allContracts
                .Where(c => Compare(c, column, op, val))
                .ToList();

            ContractsGrid.ItemsSource = filtered;
        }
        private void MarkExpiredContractsAsCompleted()
        {
            var now = DateTime.Now;

            var expiredContracts = database.ExecuteQuery(@"
        SELECT id, car_id FROM contracts 
        WHERE status = 'active' AND end_date < @now",
                new Dictionary<string, object> { { "@now", now } });

            foreach (System.Data.DataRow row in expiredContracts.Rows)
            {
                int contractId = Convert.ToInt32(row["id"]);
                int carId = Convert.ToInt32(row["car_id"]);

                database.ExecuteNonQuery("UPDATE contracts SET status = 'completed' WHERE id = @id",
                    new Dictionary<string, object> { { "@id", contractId } });

                bool hasOtherActive = database.ExecuteScalar<bool>(
                    @"SELECT EXISTS (
                SELECT 1 FROM contracts 
                WHERE car_id = @carId AND status = 'active'
            )",
                    new Dictionary<string, object> { { "@carId", carId } });

                if (!hasOtherActive)
                {
                    database.ExecuteNonQuery(
                        "UPDATE cars SET status = 'available' WHERE id = @carId",
                        new Dictionary<string, object> { { "@carId", carId } });
                }
            }
        }
        private void ToggleContractStatus_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is ContractInfo contract)
            {
                if (contract.IsActive)
                {
                    CancelConfirm_Click(sender, e);
                }
                else
                {
                    ConfirmContract_Click(sender, e);
                }
            }
        }


        private void Prolong_Click(object s, RoutedEventArgs e)
        {
            if (s is Button btn && btn.DataContext is ContractInfo ci)
            {
                var dlg = new DatePickerDialog(ci.EndDate);
                if (dlg.ShowDialog() == true)
                {
                    DateTime newEnd = dlg.SelectedDate;
                    if (newEnd <= ci.EndDate) { MessageBox.Show("Новая дата должна быть позже."); return; }

                    int extraDays = (newEnd - ci.EndDate).Days;
                    decimal extraCost = extraDays * ci.Price;       
                    database.ExecuteNonQuery(
                        "UPDATE contracts SET end_date = @newEnd, extra_amount = extra_amount + @extra WHERE id = @id",
                        new Dictionary<string, object>
                        {
        { "@newEnd", newEnd      },    
        { "@extra",  extraCost   },
        { "@id",     ci.ContractId }
                        });
                    var payForExtension = MessageBox.Show(
    $"Клиент оплатил продление ({extraDays} дн. × {ci.Price:N2} = {extraCost:N2})?",
    "Оплата продления",
    MessageBoxButton.YesNo,
    MessageBoxImage.Question
) == MessageBoxResult.Yes;

                    if (payForExtension)
                    {
                        database.ExecuteNonQuery(
                            @"INSERT INTO payments(contract_id, pay_type, amount)
          VALUES (@cid, 'other', @amt);",
                            new Dictionary<string, object>
                            {
                                ["@cid"] = ci.ContractId,
                                ["@amt"] = extraCost
                            });

                        database.ExecuteNonQuery(
                            @"UPDATE contracts
            SET paid_amount = paid_amount + @amt
          WHERE id = @cid;",
                            new Dictionary<string, object>
                            {
                                ["@cid"] = ci.ContractId,
                                ["@amt"] = extraCost
                            });
                    }
                    LoadContracts();
                }
            }
        }
        private void Return_Click(object s, RoutedEventArgs e)
        {
            if (s is Button btn && btn.DataContext is ContractInfo ci)
            {
                if (!ci.IsPaid) { MessageBox.Show("Не погашена задолженность!"); return; }
                DateTime due = ci.EndDate + ci.TimeEnd;    
                bool early = DateTime.Now < due;

                if (early && MessageBox.Show("Вернуть раньше срока?", "Подтвердите",
                                MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;

                using var tr = database.BeginTransaction();
                database.ExecuteNonQuery("UPDATE contracts SET status='completed', returned_at=now() WHERE id=@id",
                                         new Dictionary<string, object> { { "@id", ci.ContractId } });
                database.ExecuteNonQuery("UPDATE cars SET status='available' WHERE id=@car",
                                         new Dictionary<string, object> { { "@car", ci.CarId } });
                tr.Commit();
                LoadContracts();
            }
        }
        private void OpenReport_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is ContractInfo ci && !string.IsNullOrEmpty(ci.ReturnReportPath))
            {
                Process.Start(new ProcessStartInfo(ci.ReturnReportPath) { UseShellExecute = true });
            }
        }
        private void CompleteRental_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is Button btn && btn.DataContext is ContractInfo ci)) return;

            bool isEarlyReturn = DateTime.Now < ci.EndDate + ci.TimeEnd;

            var dlg = new ReturnReportWindow(
                contractId: ci.ContractId,
                isEarly: isEarlyReturn,
                employeeName: currentUserName) 
            {
                Owner = this
            };

            if (dlg.ShowDialog() != true)
                return;

            string reportPath = dlg.ReportFilePath;

            database.ExecuteNonQuery(
                @"UPDATE contracts
        SET status = 'completed',
            returned_at = now(),
            return_report_path = @path
        WHERE id = @id",
                new Dictionary<string, object>
                {
                    ["@id"] = ci.ContractId,
                    ["@path"] = reportPath
                });

            database.ExecuteNonQuery(
                "UPDATE cars SET status = 'available' WHERE id = @car",
                new Dictionary<string, object> { ["@car"] = ci.CarId });

            MessageBox.Show("Аренда завершена, отчёт сохранён.", "Готово",
                           MessageBoxButton.OK, MessageBoxImage.Information);
            LoadContracts();
        }
        private void Pay_Click(object s, RoutedEventArgs e)
        {
            if (s is Button btn && btn.DataContext is ContractInfo ci)
            {
                database.ExecuteNonQuery("UPDATE contracts SET paid_amount = total_amount WHERE id=@id",
                                         new Dictionary<string, object> { { "@id", ci.ContractId } });
                LoadContracts();
            }
        }

        private void OpenFile_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is ContractInfo contract)
            {
                string encryptedPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, contract.FilePath);

                if (!File.Exists(encryptedPath))
                {
                    MessageBox.Show("Файл не найден:\n" + encryptedPath);
                    return;
                }

                try
                {
                    string tempPath = CryptoHelper.DecryptToTempFile(encryptedPath);
                    Process.Start(new ProcessStartInfo(tempPath) { UseShellExecute = true });
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка при открытии:\n" + ex.Message);
                }
            }
        }

        private void PrintFile_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is ContractInfo contract)
            {
                string encryptedPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, contract.FilePath);

                if (!File.Exists(encryptedPath))
                {
                    MessageBox.Show("Файл не найден:\n" + encryptedPath);
                    return;
                }

                try
                {
                    string tempDocx = CryptoHelper.DecryptToTempFile(encryptedPath);
                    if (tempDocx.EndsWith(".docx", StringComparison.OrdinalIgnoreCase))
                        PrintWordDocument(tempDocx);
                    else
                        PrintUsingWindowsDefault(tempDocx);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка при печати:\n" + ex.Message);
                }
            }
        }
        private void PrintWordDocument(string filePath)
        {
            Word.Application wordApp = null;
            Word.Document wordDoc = null;

            try
            {
                System.Threading.Thread.Sleep(500);
                wordApp = new Word.Application();
                wordApp.Visible = false;
                wordDoc = wordApp.Documents.Open(filePath, ReadOnly: true);
                wordDoc.PrintOut(Background: false, Copies: 1, Range: Word.WdPrintOutRange.wdPrintAllDocument);
                System.Threading.Thread.Sleep(1000);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка печати через Word:\n{ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (wordDoc != null)
                {
                    wordDoc.Close(false);
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(wordDoc);
                }
                if (wordApp != null)
                {
                    wordApp.Quit(false);
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(wordApp);
                }
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }

        private void PrintUsingWindowsDefault(string filePath)
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = filePath,
                        Verb = "Print",
                        UseShellExecute = true,
                        CreateNoWindow = true
                    }
                };
                process.Start();
                MessageBox.Show("Документ отправлен на печать!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при печати файла:\n{ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteContract_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is ContractInfo contract)
            {
                if (contract.StatusDescription != "Не подтверждён" && contract.StatusDescription != "Отменён")
                {
                    MessageBox.Show("Можно удалять только не подтверждённые или отменённые договоры",
                                  "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var result = MessageBox.Show(
                    "Вы уверены, что хотите удалить этот договор?",
                    "Подтверждение удаления",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        database.ExecuteNonQuery(
                            "DELETE FROM contracts WHERE id = @id",
                            new Dictionary<string, object> { { "@id", contract.ContractId } });

                        MessageBox.Show("Договор успешно удалён", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                        LoadContracts();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при удалении договора:\n{ex.Message}",
                                      "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private async void ConfirmContract_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is Button btn && btn.DataContext is ContractInfo contract)) return;

       
            var paidNow = MessageBox.Show(
                "Клиент оплатил всю сумму аренды сейчас?",
                "Подтверждение оплаты",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question
            ) == MessageBoxResult.Yes;

            using var tx = database.BeginTransaction();
            try
            {
             
                if (paidNow)
                {
                    var amountToPay = contract.TotalAmount;

                    database.ExecuteNonQuery(
    @"INSERT INTO payments (contract_id, pay_type, amount)
      VALUES (@cid, 'other', @amt);",
    new Dictionary<string, object>
    {
        ["@cid"] = contract.ContractId,
        ["@amt"] = amountToPay
    });

                    database.ExecuteNonQuery(
                        "UPDATE contracts SET paid_amount = @amt WHERE id = @cid;",
                        new Dictionary<string, object>
                        {
                            ["@cid"] = contract.ContractId,
                            ["@amt"] = amountToPay
                        });

                }

              
                database.ExecuteNonQuery(
                    "UPDATE contracts SET status = 'active' WHERE id = @id",
                    new Dictionary<string, object> { ["@id"] = contract.ContractId });

                database.ExecuteNonQuery(
                    "INSERT INTO car_occupations(car_id, status, start_date, end_date) VALUES(@car,'occupied',@sd,@ed)",
                    new Dictionary<string, object>
                    {
                        ["@car"] = contract.CarId,
                        ["@sd"] = contract.StartDate,
                        ["@ed"] = contract.EndDate
                    });

                database.ExecuteNonQuery(
                    "UPDATE cars SET status = 'rented' WHERE id = @car",
                    new Dictionary<string, object> { ["@car"] = contract.CarId });

                tx.Commit();
                MessageBox.Show("Договор подтверждён и, при необходимости, оплата учтена.", "Готово", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadContracts();
            }
            catch (Exception ex)
            {
                tx.Rollback();
                MessageBox.Show("Ошибка при подтверждении: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void CancelConfirm_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is ContractInfo contract)
            {
                try
                {
                    var contractData = database.ExecuteQuery(
                        "SELECT car_id, start_date, end_date FROM contracts WHERE id = @contractId",
                        new Dictionary<string, object> { { "@contractId", contract.ContractId } });

                    if (contractData.Rows.Count == 0)
                    {
                        MessageBox.Show("Договор не найден", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    int carId = Convert.ToInt32(contractData.Rows[0]["car_id"]);
                    DateTime startDate = Convert.ToDateTime(contractData.Rows[0]["start_date"]);
                    DateTime endDate = Convert.ToDateTime(contractData.Rows[0]["end_date"]);

                    using (var transaction = database.BeginTransaction())
                    {
                        try
                        {
                            database.ExecuteNonQuery(
     "UPDATE contracts SET status = 'cancelled', cancel_date = @now WHERE id = @id",
     new Dictionary<string, object> {
        { "@id", contract.ContractId },
        { "@now", DateTime.Now }
     });
                            database.ExecuteNonQuery(
                                "DELETE FROM car_occupations " +
                                "WHERE car_id = @carId AND start_date = @startDate AND end_date = @endDate",
                                new Dictionary<string, object> { { "@carId", carId }, { "@startDate", startDate }, { "@endDate", endDate } });

                            database.ExecuteNonQuery(
                                "UPDATE cars SET status = 'available' WHERE id = @carId",
                                new Dictionary<string, object> { { "@carId", carId } });

                            transaction.Commit();

                            MessageBox.Show("Договор отменён. Автомобиль снова доступен.",
                                          "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                            LoadContracts();
                        }
                        catch
                        {
                            transaction.Rollback();
                            throw;
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
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
                    (c.FilePath?.ToLower().Contains(searchText) ?? false) ||
                    c.CarInfo.ToLower().Contains(searchText) ||
                    c.StatusDescription.ToLower().Contains(searchText))
                .ToList();

            ContractsGrid.ItemsSource = filtered;
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        private string DecryptToTempFile(string encryptedPath)
        {
            string tempPath = Path.Combine(Path.GetTempPath(), Path.GetFileNameWithoutExtension(encryptedPath) + ".docx");

            byte[] key = new byte[32];
            byte[] iv = new byte[16];
            for (int i = 0; i < key.Length; i++) key[i] = (byte)(i + 1);
            for (int i = 0; i < iv.Length; i++) iv[i] = (byte)(i + 2);

            using (FileStream inputFileStream = new(encryptedPath, FileMode.Open, FileAccess.Read))
            using (FileStream outputFileStream = new(tempPath, FileMode.Create, FileAccess.Write))
            using (var aes = System.Security.Cryptography.Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv;

                using var decryptor = aes.CreateDecryptor();
                using var cryptoStream = new System.Security.Cryptography.CryptoStream(inputFileStream, decryptor, System.Security.Cryptography.CryptoStreamMode.Read);
                cryptoStream.CopyTo(outputFileStream);
            }

            return tempPath;
        }


    }
}