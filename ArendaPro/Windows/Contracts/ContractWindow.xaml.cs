using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using static ArendaPro.OtherOborot;
using System.Data.SqlClient;
using System.Data;
namespace ArendaPro
{
    // Логика класса: ContractWindow содержит сценарии этого модуля, управляет данными и координирует взаимодействие UI с сервисами.
    public partial class ContractWindow : Window
    {
        private decimal _dailyRate = 0m;
        private readonly BD db;
        private DateTime? lastValidStartDate;
        private DateTime? lastValidEndDate;

        public ContractWindow()
        {
            InitializeComponent();
            db = new BD(ConfigurationManager.ConnectionStrings["DbConnection"]?.ConnectionString);
            Loaded += Window_Loaded;

            TimeStartPicker.Text = DateTime.Now.ToString("HH:mm");
            TimeEndPicker.Text = DateTime.Now.AddHours(1).ToString("HH:mm");
        }

        string NumberToWords(int number)
        {
            if (number == 0) return "ноль рублей";

            string[] unitsMale = { "", "один", "два", "три", "четыре", "пять", "шесть", "семь", "восемь", "девять" };
            string[] unitsFemale = { "", "одна", "две", "три", "четыре", "пять", "шесть", "семь", "восемь", "девять" };
            string[] teens = { "десять", "одиннадцать", "двенадцать", "тринадцать", "четырнадцать", "пятнадцать", "шестнадцать", "семнадцать", "восемнадцать", "девятнадцать" };
            string[] tens = { "", "", "двадцать", "тридцать", "сорок", "пятьдесят", "шестьдесят", "семьдесят", "восемьдесят", "девяносто" };
            string[] hundreds = { "", "сто", "двести", "триста", "четыреста", "пятьсот", "шестьсот", "семьсот", "восемьсот", "девятьсот" };

            List<string> parts = new();

            void AppendTriplet(int num, string[] units, string one, string few, string many)
            {
                if (num == 0) return;

                parts.Add(hundreds[num / 100]);

                int ten = (num % 100) / 10;
                int unit = num % 10;

                if (ten == 1)
                {
                    parts.Add(teens[unit]);
                }
                else
                {
                    parts.Add(tens[ten]);
                    parts.Add(units[unit]);
                }

                string wordForm = many;
                if (ten != 1)
                {
                    if (unit == 1) wordForm = one;
                    else if (unit >= 2 && unit <= 4) wordForm = few;
                }

                parts.Add(wordForm);
            }

            int millions = number / 1_000_000;
            int thousands = (number % 1_000_000) / 1_000;
            int rest = number % 1_000;

            if (millions > 0)
                AppendTriplet(millions, unitsMale, "миллион", "миллиона", "миллионов");

            if (thousands > 0)
                AppendTriplet(thousands, unitsFemale, "тысяча", "тысячи", "тысяч");

            if (rest > 0 || (millions == 0 && thousands == 0))
                AppendTriplet(rest, unitsMale, "рубль", "рубля", "рублей");

            return string.Join(" ", parts.Where(p => !string.IsNullOrWhiteSpace(p)));
        }

        // Метод Window_Loaded: инициализирует состояние модуля при старте: загружает данные и подготавливает элементы экрана к работе (комментарий #1).
        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadDataAsync();
        }

        // Логика класса: BoolToConfirmCancelButtonConverter содержит сценарии этого модуля, управляет данными и координирует взаимодействие UI с сервисами.
        public class BoolToConfirmCancelButtonConverter : IValueConverter
        {
            // Метод Convert: реализует отдельный этап внутренней логики модуля: трансформирует вход, применяет правила и формирует следующий шаг исполнения (комментарий #2).
            public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            {
                return (bool)value ? "Подтвердить" : "Отмена";
            }

            // Метод ConvertBack: реализует отдельный этап внутренней логики модуля: трансформирует вход, применяет правила и формирует следующий шаг исполнения (комментарий #3).
            public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }
        // Метод IsCarAvailable: реализует отдельный этап внутренней логики модуля: трансформирует вход, применяет правила и формирует следующий шаг исполнения (комментарий #4).
        private async Task<bool> IsCarAvailable(int carId, DateTime startDate, DateTime endDate)
        {
            try
            {
                return await db.ExecuteScalarAsync<bool>(@"
SELECT CASE 
WHEN EXISTS (
    SELECT 1
    FROM contracts
    WHERE car_id = @carId
      AND status = 'active'
      AND start_date <= @endDate
      AND end_date >= @startDate
)
THEN CAST(0 AS BIT)
ELSE CAST(1 AS BIT)
END",
        new
        {
            carId,
            startDate,
            endDate
        });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка проверки доступности: {ex.Message}");
                return false;
            }
        }
       
        // Метод ValidateDateTime: проводит целевую валидацию условий и сообщает, можно ли безопасно продолжать сценарий (комментарий #5).
        private bool ValidateDateTime()
        {

            if (!DateTime.TryParseExact(TimeStartPicker.Text, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var startTime))
            {
                MessageBox.Show("Неверный формат времени начала (используйте ЧЧ:MM)");
                return false;
            }

            if (!DateTime.TryParseExact(TimeEndPicker.Text, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var endTime))
            {
                MessageBox.Show("Неверный формат времени окончания (используйте ЧЧ:MM)");
                return false;
            }

            if (!StartDatePicker.SelectedDate.HasValue || !EndDatePicker.SelectedDate.HasValue)
            {
                MessageBox.Show("Выберите даты начала и окончания");
                return false;
            }

            var startDate = StartDatePicker.SelectedDate.Value.Date + startTime.TimeOfDay;
            var endDate = EndDatePicker.SelectedDate.Value.Date + endTime.TimeOfDay;

            if (startDate < DateTime.Now)
                if (startDate.Date < DateTime.Today)
                {
                    MessageBox.Show("Дата начала не может быть раньше сегодняшнего дня");
                    return false;
                }
            if (endDate.Date <= startDate.Date)
            {
                MessageBox.Show("Период аренды должен быть как минимум 1 день (конечная дата > начальной).",
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            if (endDate <= startDate)
            {
                MessageBox.Show("Дата окончания должна быть позже даты начала");
                return false;
            }

            lastValidStartDate = startDate;
            lastValidEndDate = endDate;
            return true;
        }

        // Метод LoadDataAsync: инициализирует состояние модуля при старте: загружает данные и подготавливает элементы экрана к работе (комментарий #6).
        private async Task LoadDataAsync()
        {
            try
            {
                var clientsTable = await db.ExecuteQueryAsync("SELECT id, familia, imia, otchestvo FROM clients");
                foreach (System.Data.DataRow row in clientsTable.Rows)
                {
                    ClientComboBox.Items.Add(new ComboBoxItem
                    {
                        Content = $"{row["familia"]} {row["imia"]} {row["otchestvo"]}",
                        Tag = row["id"]
                    });
                }

                var carsTable = await db.ExecuteQueryAsync(
                     "SELECT id, marka, gos_nomer FROM cars ORDER BY marka");
                foreach (System.Data.DataRow row in carsTable.Rows)
                {
                    CarComboBox.Items.Add(new ComboBoxItem
                    {
                        Content = $"{row["marka"]} ({row["gos_nomer"]})",
                        Tag = row["id"]
                    });
                }

                var placesTable = await db.ExecuteQueryAsync("SELECT name FROM places ORDER BY name");
                foreach (System.Data.DataRow row in placesTable.Rows)
                {
                    string place = row["name"].ToString();
                    PlaceStartBox.Items.Add(place);
                    PlaceEndBox.Items.Add(place);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}");
            }
        }
        // Метод OnDateChanged: реализует отдельный этап внутренней логики модуля: трансформирует вход, применяет правила и формирует следующий шаг исполнения (комментарий #7).
        private void OnDateChanged(object sender, SelectionChangedEventArgs e)
        {
            RecalculatePrice(sender, e);
        }


        // Метод RecalculatePrice: реализует отдельный этап внутренней логики модуля: трансформирует вход, применяет правила и формирует следующий шаг исполнения (комментарий #8).
        private async void RecalculatePrice(object sender, EventArgs e)
        {
            if (!TryGetDateTime(out DateTime startDateTime, out DateTime endDateTime)
                || CarComboBox.SelectedItem == null)
            {
                PriceBox.Text = "";
                return;
            }

            int carId = (int)((ComboBoxItem)CarComboBox.SelectedItem).Tag;

            bool useTempMode = Properties.Settings.Default.SaveTemporaryMode;

            using var conn = db.GetConnection();
            await conn.OpenAsync();

            decimal dailyRate;

            if (!useTempMode)
            {
                var cmd = new SqlCommand(@"
SELECT t.daily_rate
  FROM tariffs t
  JOIN cars c ON c.tariff_id = t.id
 WHERE c.id = @id;", conn);
                cmd.Parameters.AddWithValue("id", carId);
                var res = await cmd.ExecuteScalarAsync();
                if (res is DBNull or null)
                {
                    PriceBox.Text = "";
                    return;
                }
                dailyRate = Convert.ToDecimal(res);
            }
            else
            {
                var cmd = new SqlCommand(@"
SELECT TOP 1 price
FROM car_tariff_history
WHERE car_id = @id
  AND start_date <= @d
  AND (end_date IS NULL OR end_date >= @d)
ORDER BY start_date DESC;", conn);
                cmd.Parameters.AddWithValue("id", carId);
                cmd.Parameters.AddWithValue("d", startDateTime.Date);
                var r = await cmd.ExecuteScalarAsync();
                if (r is DBNull or null)
                {
                    PriceBox.Text = "";
                    return;
                }
                dailyRate = Convert.ToDecimal(r);
            }

            _dailyRate = decimal.Truncate(dailyRate);    
            PriceBox.Text = _dailyRate.ToString("F0", CultureInfo.InvariantCulture);
        }
        // Метод TryGetDateTime: реализует отдельный этап внутренней логики модуля: трансформирует вход, применяет правила и формирует следующий шаг исполнения (комментарий #9).
        private bool TryGetDateTime(out DateTime start, out DateTime end)
        {
            start = DateTime.MinValue;
            end = DateTime.MinValue;

            if (!DateTime.TryParseExact(TimeStartPicker.Text, "HH:mm", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var startTime)) return false;
            if (!DateTime.TryParseExact(TimeEndPicker.Text, "HH:mm", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var endTime)) return false;
            if (!StartDatePicker.SelectedDate.HasValue || !EndDatePicker.SelectedDate.HasValue)
                return false;

            start = StartDatePicker.SelectedDate.Value.Date + startTime.TimeOfDay;
            end = EndDatePicker.SelectedDate.Value.Date + endTime.TimeOfDay;

            if (start.Date < DateTime.Today) return false;

            if (end <= start) return false;

            lastValidStartDate = start;
            lastValidEndDate = end;
            return true;
        }
        // Метод AddPlaceIfNew: добавляет новую сущность в доменную коллекцию и синхронизирует зависимые визуальные и вычислительные представления (комментарий #10).
        private async Task AddPlaceIfNew(string place, ComboBox comboBox)
        {
            if (string.IsNullOrWhiteSpace(place)) return;

            using (var conn = db.GetConnection())
            {
                await conn.OpenAsync();
                var checkCmd = new SqlCommand("SELECT COUNT(*) FROM places WHERE name = @name", conn);
                checkCmd.Parameters.AddWithValue("name", place);
                int count = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());

                if (count == 0)
                {
                    var result = MessageBox.Show(
                        $"Обнаружен новый адрес: «{place}».\nДобавить его в список доступных мест аренды?",
                        "Новый адрес",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        var insertCmd = new SqlCommand("INSERT INTO places (name) VALUES (@name)", conn);
                        insertCmd.Parameters.AddWithValue("name", place);
                        await insertCmd.ExecuteNonQueryAsync();
                        comboBox.Items.Add(place);   
                    }
                }
            }
        }

        // Метод PlaceStartBox_LostFocus: реализует отдельный этап внутренней логики модуля: трансформирует вход, применяет правила и формирует следующий шаг исполнения (комментарий #11).
        private async void PlaceStartBox_LostFocus(object sender, RoutedEventArgs e)
        {
            await AddPlaceIfNew(PlaceStartBox.Text, PlaceStartBox);
        }

        // Метод PlaceEndBox_LostFocus: реализует отдельный этап внутренней логики модуля: трансформирует вход, применяет правила и формирует следующий шаг исполнения (комментарий #12).
        private async void PlaceEndBox_LostFocus(object sender, RoutedEventArgs e)
        {
            await AddPlaceIfNew(PlaceEndBox.Text, PlaceEndBox);
        }

        // Метод GenerateContract_Click: обрабатывает нажатие в интерфейсе: считывает ввод, проверяет ограничения и запускает следующий пользовательский шаг (комментарий #13).
        private async void GenerateContract_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!ValidateDateTime() ||
                    ClientComboBox.SelectedItem == null ||
                    CarComboBox.SelectedItem == null ||
                    string.IsNullOrWhiteSpace(PlaceStartBox.Text) ||
                    string.IsNullOrWhiteSpace(PlaceEndBox.Text))
                {
                    MessageBox.Show("Заполните все обязательные поля");
                    return;
                }

                var selectedClient = (ComboBoxItem)ClientComboBox.SelectedItem;
                var selectedCar = (ComboBoxItem)CarComboBox.SelectedItem;
                int clientId = (int)selectedClient.Tag;
                int carId = (int)selectedCar.Tag;
                string timeStart = TimeStartPicker.Text;
                string timeEnd = TimeEndPicker.Text;
                string placeStart = PlaceStartBox.Text;
                string placeEnd = PlaceEndBox.Text;
                DateTime startDateOnly = StartDatePicker.SelectedDate.Value.Date;
                DateTime endDateOnly = EndDatePicker.SelectedDate.Value.Date;
                if (!lastValidStartDate.HasValue || !lastValidEndDate.HasValue)
                {
                    MessageBox.Show("Неверные даты аренды");
                    return;
                }

                if (!await IsCarAvailable(carId, lastValidStartDate.Value, lastValidEndDate.Value))
                {
                    MessageBox.Show("Автомобиль уже занят на выбранный период.\nВыберите другие даты или автомобиль.");
                    return;
                }

                if (!decimal.TryParse(PriceBox.Text, out decimal price) || price <= 0)
                {
                    MessageBox.Show("Неверный формат стоимости или стоимость не рассчитана.");
                    return;
                }

                string templatePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Shablon", "dogovor.docx");
                string savePath = $"Contracts/Договор_{selectedClient.Content}_{DateTime.Now:yyyyMMdd_HHmmss}.docx";
                string folder = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
    "ArendaPro", "Contracts");

                Directory.CreateDirectory(folder); // если ещё не создана
                if (!File.Exists(templatePath))
                {
                    MessageBox.Show("Шаблон договора не найден.");
                    return;
                }

                Directory.CreateDirectory("Contracts");

                string clientFullName = "", passport = "", birth = "", kemVydan = "", address = "",
                       phone = "", drivingLicense = "", licenseDate = "";
                string carModel = "", plate = "", vin = "", color = "", year = "", regCert = "";

                using (var conn = db.GetConnection())
                {
                    await conn.OpenAsync();

                    var cmd = new SqlCommand("SELECT * FROM clients WHERE id = @id", conn);
                    cmd.Parameters.AddWithValue("id", clientId);
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            clientFullName = $"{reader["familia"]} {reader["imia"]} {reader["otchestvo"]}";
                            passport = reader["pasport"].ToString();
                            birth = ((DateTime)reader["data_rozhdeniya"]).ToString("dd.MM.yyyy");
                            kemVydan = reader["kem_vydan_pasport"].ToString();
                            address = reader["adres_prozhivaniya"].ToString();
                            phone = reader["telefon"].ToString();
                            drivingLicense = reader["voditelskoe_udostoverenie"].ToString();
                            licenseDate = ((DateTime)reader["data_vydachi_voditelskogo"]).ToString("dd.MM.yyyy");
                        }
                    }

                    cmd = new SqlCommand("SELECT * FROM cars WHERE id = @id", conn);
                    cmd.Parameters.AddWithValue("id", carId);
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            carModel = reader["marka"].ToString();
                            plate = reader["gos_nomer"].ToString();
                            vin = reader["vin"].ToString();
                            color = reader["cvet"].ToString();
                            year = reader["god_vipuska"].ToString();
                            regCert = reader["registr_svidetelstva"].ToString();
                        }
                    }
                }

                DateTime startDate = StartDatePicker.SelectedDate.Value.Date +
                       DateTime.ParseExact(timeStart, "HH:mm", CultureInfo.InvariantCulture).TimeOfDay;
                DateTime endDate = EndDatePicker.SelectedDate.Value.Date +
                       DateTime.ParseExact(timeEnd, "HH:mm", CultureInfo.InvariantCulture).TimeOfDay;

                string contractNumber;
                using (var conn = db.GetConnection())
                {
                    await conn.OpenAsync();
                    var cmd = new SqlCommand("SELECT ISNULL(MAX(contract_number), 0) + 1 \r\nFROM contracts", conn);
                    contractNumber = (await cmd.ExecuteScalarAsync() ?? 1).ToString();
                    if (!int.TryParse(contractNumber, out int contractNum) || contractNum <= 0)
                    {
                        MessageBox.Show("Неверный формат номера договора.");
                        return;
                    }
                }
                string tempPath = Path.Combine(Path.GetTempPath(), Path.GetFileName(savePath));
                File.Copy(templatePath, tempPath, true);

                using (WordprocessingDocument wordDoc = WordprocessingDocument.Open(tempPath, true))
                {
                    var replacements = new Dictionary<string, string>
                    {
                        ["{ClientFullName}"] = clientFullName,
                        ["{ClientPassport}"] = passport,
                        ["{ClientBirth}"] = birth,
                        ["{Kem_vydan_pasport}"] = kemVydan,
                        ["{adres_prozhivaniya}"] = address,
                        ["{telefon}"] = phone,
                        ["{voditelskoe_udostoverenie}"] = drivingLicense,
                        ["{data_vydachi_voditelskogo}"] = licenseDate,
                        ["{CarModel}"] = carModel,
                        ["{gos_nomer}"] = plate,
                        ["{vin}"] = vin,
                        ["{cvet}"] = color,
                        ["{god_vipuska}"] = year,
                        ["{registr_svidetelstva}"] = regCert,
                        ["{StartDate}"] = StartDatePicker.SelectedDate.Value.ToString("dd.MM.yyyy"),
                        ["{EndDate}"] = EndDatePicker.SelectedDate.Value.ToString("dd.MM.yyyy"),
                        ["{timeStart}"] = timeStart,
                        ["{timeEnd}"] = timeEnd,
                        ["{placestart}"] = placeStart,
                        ["{placeend}"] = placeEnd,
                        ["{creation_date}"] = DateTime.Now.ToString("dd.MM.yyyy"),
                        ["{nomer_dogovora}"] = contractNumber,
                        ["{data_zapolnenia}"] = DateTime.Now.ToString("dd.MM.yyyy"),
                        ["{stoimost}"] = $"{(int)price} ({NumberToWords((int)price)})"
                    };

                    ReplacePlaceholdersPreserveFormatting(wordDoc, replacements);
                    wordDoc.MainDocumentPart.Document.Save();
                }

                // === Шифрование ===
                string encryptedPath = Path.Combine(folder, $"enc_{Path.GetFileNameWithoutExtension(savePath)}.bin");
                CryptoHelper.EncryptFile(tempPath, encryptedPath);
                File.Delete(tempPath);
                savePath = encryptedPath; // сохранить в БД путь к .binh
                decimal dailyRate;
                if (!decimal.TryParse(PriceBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out dailyRate))
                {
                    MessageBox.Show("Неверный формат дневной ставки.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                int placeStartId, placeEndId;
                using (var conn = db.GetConnection())
                {
                    await conn.OpenAsync();

                    async Task<int> GetPlaceId(string placeName)
                    {
                        var cmd = new SqlCommand("SELECT id FROM places WHERE name = @name", conn);
                        cmd.Parameters.AddWithValue("name", placeName);
                        var result = await cmd.ExecuteScalarAsync();
                        if (result == null)
                            throw new Exception($"Место не найдено: {placeName}");
                        return Convert.ToInt32(result);
                    }

                    placeStartId = await GetPlaceId(placeStart);
                    placeEndId = await GetPlaceId(placeEnd);
                }
                int days = (int)(lastValidEndDate.Value.Date - lastValidStartDate.Value.Date).TotalDays + 1;
                if (days < 1) days = 1;
                decimal total = dailyRate * days;
                MessageBox.Show($"Договор успешно сохранён:\n{savePath}");

                using (var conn = db.GetConnection())
                {
                    await conn.OpenAsync();
                    var insertCmd = new SqlCommand(@"
INSERT INTO contracts (
    client_id, car_id, user_id,
    start_date, end_date, time_start, time_end,
    place_start_id, place_end_id,
    contract_number, creation_date,
    price, contract_doc_path, status
)
VALUES (
    @client_id, @car_id, @user_id,
    @start_date, @end_date, @time_start, @time_end,
    @place_start, @place_end,
    @contract_number, @creation_date,
    @price, @path, 'not_confirmed'
);

SELECT SCOPE_IDENTITY();", conn);

                    insertCmd.Parameters.AddWithValue("client_id", clientId);
                    insertCmd.Parameters.AddWithValue("car_id", carId);
                    insertCmd.Parameters.AddWithValue("user_id", CurrentSession.UserId);
                    insertCmd.Parameters.AddWithValue("start_date", startDateOnly);
                    insertCmd.Parameters.AddWithValue("end_date", endDateOnly);
                    insertCmd.Parameters.AddWithValue("time_start", timeStart);
                    insertCmd.Parameters.AddWithValue("time_end", timeEnd);
                    insertCmd.Parameters.AddWithValue("place_start", placeStartId);
                    insertCmd.Parameters.AddWithValue("place_end", placeEndId);
                    insertCmd.Parameters.AddWithValue("contract_number", int.Parse(contractNumber));
                    insertCmd.Parameters.AddWithValue("creation_date", DateTime.Today);
                    insertCmd.Parameters.AddWithValue("price", dailyRate);
                    insertCmd.Parameters.AddWithValue("path", savePath);

                    int contractId = Convert.ToInt32(await insertCmd.ExecuteScalarAsync());
                    using (var docCmd = new SqlCommand(@"
    INSERT INTO contracts_docs (
    contract_id,
    doc_path,
    doc_type
)
VALUES (
    @cid,
    @path,
    @type
);", conn))
                    {
                        docCmd.Parameters.AddWithValue("@cid", contractId);
                        docCmd.Parameters.AddWithValue("@path", savePath);
                        docCmd.Parameters.AddWithValue("@type", System.IO.Path.GetExtension(savePath).TrimStart('.'));
                        await docCmd.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Произошла ошибка: {ex.Message}\n\n{ex.StackTrace}");
            }
        }
        
        void ReplacePlaceholdersPreserveFormatting(WordprocessingDocument doc, Dictionary<string, string> replacements)
        {
            var textElements = doc.MainDocumentPart.Document.Descendants<Text>().ToList();

            for (int i = 0; i < textElements.Count; i++)
            {
                foreach (var kv in replacements)
                {
                    string placeholder = kv.Key;
                    string value = kv.Value;

                    string combined = "";
                    var buffer = new List<Text>();
                    int j = i;

                    while (j < textElements.Count && combined.Length < placeholder.Length + 20)
                    {
                        combined += textElements[j].Text;
                        buffer.Add(textElements[j]);

                        if (combined.Contains(placeholder))
                            break;

                        j++;
                    }

                    if (combined.Contains(placeholder))
                    {
                        string replaced = combined.Replace(placeholder, value);
                        int currentIndex = 0;

                        for (int k = 0; k < buffer.Count; k++)
                        {
                            var textNode = buffer[k];

                            int remaining = replaced.Length - currentIndex;
                            if (remaining <= 0)
                            {
                                textNode.Text = "";
                                continue;
                            }

                            int take = Math.Min(textNode.Text.Length, remaining);
                            textNode.Text = replaced.Substring(currentIndex, take);
                            currentIndex += take;
                        }

                        if (currentIndex < replaced.Length)
                        {
                            var lastRun = buffer.Last().Parent as DocumentFormat.OpenXml.Wordprocessing.Run;
                            lastRun.AppendChild(new Text(replaced.Substring(currentIndex)));
                        }

                        i = j;
                        break;
                    }
                }
            }

            doc.MainDocumentPart.Document.Save();
        }

        // Метод BackButton_Click: обрабатывает нажатие в интерфейсе: считывает ввод, проверяет ограничения и запускает следующий пользовательский шаг (комментарий #14).
        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}

