using Npgsql;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.IO.Packaging;
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
using Xceed.Words.NET;
namespace ArendaPro
{


    public partial class ContractWindow : Window
    {
        public ContractWindow()
        {
            InitializeComponent();
        }
        public class ContractInfo
        {
            public int ContractId { get; set; }
            public string FilePath { get; set; }
            public string FullName => $"{Familia} {Imia} {Otchestvo}";
            public string Familia { get; set; }
            public string Imia { get; set; }
            public string Otchestvo { get; set; }
        }
        string connStr = ConfigurationManager.ConnectionStrings["DbConnection"]?.ConnectionString;
        private BD bd;
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            StartDatePicker.SelectedDateChanged += RecalculatePrice;
            EndDatePicker.SelectedDateChanged += RecalculatePrice;
            CarComboBox.SelectionChanged += RecalculatePrice;
            using (var conn = new BD(connStr).GetConnection())
            {
                conn.Open();

                // Клиенты
                using (var cmd = new NpgsqlCommand("SELECT id, familia, imia, otchestvo FROM clients", conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string fullName = $"{reader["familia"]} {reader["imia"]} {reader["otchestvo"]}";
                        ClientComboBox.Items.Add(new ComboBoxItem
                        {
                            Content = fullName,
                            Tag = reader["id"]
                        });
                    }
                }

                // Автомобили
                using (var cmd = new NpgsqlCommand("SELECT id, marka, gos_nomer FROM cars", conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string carName = $"{reader["marka"]} ({reader["gos_nomer"]})";
                        CarComboBox.Items.Add(new ComboBoxItem
                        {
                            Content = carName,
                            Tag = reader["id"]
                        });
                    }
                }

                // Адреса
                using (var cmd = new NpgsqlCommand("SELECT name FROM places ORDER BY name", conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string place = reader["name"].ToString();
                        PlaceStartBox.Items.Add(place);
                        PlaceEndBox.Items.Add(place);
                    }
                }


            }
        }
        private void RecalculatePrice(object sender, EventArgs e)
        {
            if (!StartDatePicker.SelectedDate.HasValue || !EndDatePicker.SelectedDate.HasValue)
                return;

            var carItem = (ComboBoxItem)CarComboBox.SelectedItem;
            if (carItem == null) return;

            int carId = (int)carItem.Tag;

            var startDate = StartDatePicker.SelectedDate.Value;
            var endDate = EndDatePicker.SelectedDate.Value;

            int days = (endDate - startDate).Days;
            if (days < 1) days = 1;

            using (var conn = new BD(connStr).GetConnection())
            {
                conn.Open();
                var cmd = new NpgsqlCommand("SELECT price FROM tariffs WHERE car_id = @id", conn);
                cmd.Parameters.AddWithValue("id", carId);

                var result = cmd.ExecuteScalar();
                if (result != null)
                {
                    decimal pricePerDay = Convert.ToDecimal(result);
                    decimal total = pricePerDay * days;
                    PriceBox.Text = total.ToString("F2");
                }
            }
        }

        private void AddPlaceIfNew(string place)
        {
            if (string.IsNullOrWhiteSpace(place))
                return;

            using (var conn = new BD(connStr).GetConnection())
            {
                conn.Open();

                // Проверка, есть ли адрес в БД
                var checkCmd = new NpgsqlCommand("SELECT COUNT(*) FROM places WHERE name = @name", conn);
                checkCmd.Parameters.AddWithValue("name", place);
                var count = (long)checkCmd.ExecuteScalar();

                if (count == 0)
                {
                    var result = MessageBox.Show(
                        $"Обнаружен новый адрес: «{place}».\nДобавить его в список доступных мест аренды?",
                        "Новый адрес",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question
                    );

                    if (result == MessageBoxResult.Yes)
                    {
                        var insertCmd = new NpgsqlCommand("INSERT INTO places (name) VALUES (@name)", conn);
                        insertCmd.Parameters.AddWithValue("status", "not_confirmed");

                        insertCmd.ExecuteNonQuery();
                    }
                }
            }
        }


        private void GenerateContract_Click(object sender, RoutedEventArgs e)
        {
            var client = (ComboBoxItem)ClientComboBox.SelectedItem;
            var car = (ComboBoxItem)CarComboBox.SelectedItem;

            if (client == null || car == null || !StartDatePicker.SelectedDate.HasValue || !EndDatePicker.SelectedDate.HasValue)
            {
                MessageBox.Show("Пожалуйста, выберите клиента, автомобиль и даты аренды.");
                return;
            }

            string clientId = client.Tag.ToString();
            string carId = car.Tag.ToString();

            string templatePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Shablon", "dogovor.docx");
            string savePath = $"Contracts/Договор_{client.Content}_{DateTime.Now:yyyyMMdd_HHmmss}.docx";

            if (!File.Exists(templatePath))
            {
                MessageBox.Show("Шаблон договора не найден.");
                return;
            }

            Directory.CreateDirectory("Contracts");

            // Переменные для подстановки
            string clientFullName = "", passport = "", birth = "", Kem_vydan_pasport = "", adres_prozhivaniya = "",
                   telefon = "", voditelskoe_udostoverenie = "", data_vydachi_voditelskogo = "";

            string carModel = "", plate = "", vin = "", cvet = "", year = "", registr_svidetelstva = "";

            using (var conn = new BD(connStr).GetConnection())
            {
                conn.Open();

                // Данные клиента
                var cmd = new NpgsqlCommand("SELECT * FROM clients WHERE id = @id", conn);
                cmd.Parameters.AddWithValue("id", int.Parse(clientId));
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        clientFullName = $"{reader["familia"]} {reader["imia"]} {reader["otchestvo"]}";
                        passport = reader["pasport"].ToString();
                        birth = Convert.ToDateTime(reader["data_rozhdeniya"]).ToString("dd.MM.yyyy");
                        Kem_vydan_pasport = reader["Kem_vydan_pasport"].ToString();
                        adres_prozhivaniya = reader["adres_prozhivaniya"].ToString();
                        telefon = reader["telefon"].ToString();
                        voditelskoe_udostoverenie = reader["voditelskoe_udostoverenie"].ToString();
                        data_vydachi_voditelskogo = Convert.ToDateTime(reader["data_vydachi_voditelskogo"]).ToString("dd.MM.yyyy");
                    }
                }

                // Данные автомобиля
                cmd = new NpgsqlCommand("SELECT * FROM cars WHERE id = @id", conn);
                cmd.Parameters.AddWithValue("id", int.Parse(carId));
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        carModel = reader["marka"].ToString();
                        plate = reader["gos_nomer"].ToString();
                        vin = reader["vin"].ToString();
                        cvet = reader["cvet"].ToString();
                        year = reader["god_vipuska"].ToString();
                        registr_svidetelstva = reader["registr_svidetelstva"].ToString();
                    }
                }
            }

            string startDate = StartDatePicker.SelectedDate.Value.ToString("dd.MM.yyyy");
            string endDate = EndDatePicker.SelectedDate.Value.ToString("dd.MM.yyyy");

            string timeStart = TimeStartPicker.Text;
            string timeEnd = TimeEndPicker.Text;
            string placestart = PlaceStartBox.Text;
            string placeend = PlaceEndBox.Text;

            AddPlaceIfNew(placestart);
            AddPlaceIfNew(placeend);

            int nextContractNumber = 1;

            using (var conn = new BD(connStr).GetConnection())
            {
                conn.Open();
                var cmd = new NpgsqlCommand("SELECT MAX(contract_number) \r\nFROM contracts \r\nWHERE contract_number::text ~ '^[0-9]+$';\r\n", conn);
                var result = cmd.ExecuteScalar();
                if (result != DBNull.Value && result != null)
                {
                    nextContractNumber = Convert.ToInt32(result) + 1;
                }
            }

            string nomer_dogovora = nextContractNumber.ToString();
            string data_zapolnenia = DateTime.Now.ToString("dd.MM.yyyy");
            string stoimost = PriceBox.Text;

            var doc = DocX.Load(templatePath);


            // Замены в шаблоне
            doc.ReplaceText("{ClientFullName}", clientFullName);
            doc.ReplaceText("{ClientPassport}", passport);
            doc.ReplaceText("{ClientBirth}", birth);
            doc.ReplaceText("{Kem_vydan_pasport}", Kem_vydan_pasport);
            doc.ReplaceText("{adres_prozhivaniya}", adres_prozhivaniya);
            doc.ReplaceText("{telefon}", telefon);
            doc.ReplaceText("{voditelskoe_udostoverenie}", voditelskoe_udostoverenie);
            doc.ReplaceText("{data_vydachi_voditelskogo}", data_vydachi_voditelskogo);

            doc.ReplaceText("{CarModel}", carModel);
            doc.ReplaceText("{gos_nomer}", plate);
            doc.ReplaceText("{vin}", vin);
            doc.ReplaceText("{cvet}", cvet);
            doc.ReplaceText("{god_vipuska}", year);
            doc.ReplaceText("{registr_svidetelstva}", registr_svidetelstva);

            doc.ReplaceText("{StartDate}", startDate);
            doc.ReplaceText("{EndDate}", endDate);
            doc.ReplaceText("{timeStart}", timeStart);
            doc.ReplaceText("{timeEnd}", timeEnd);
            doc.ReplaceText("{placestart}", placestart);
            doc.ReplaceText("{placeend}", placeend);

            doc.ReplaceText("{nomer_dogovora}", nomer_dogovora);
            doc.ReplaceText("{data_zapolnenia}", data_zapolnenia);
            doc.ReplaceText("{stoimost}", stoimost);

            doc.SaveAs(savePath);
            MessageBox.Show("Договор успешно сохранён:\n" + savePath);
            int insertedContractId;
using (var conn = new BD(connStr).GetConnection())
{
    conn.Open();

    var insertCmd = new NpgsqlCommand(@"
       INSERT INTO contracts (
        client_id, car_id, start_date, end_date, 
        time_start, time_end, place_start, place_end, 
        contract_number, creation_date, price, file_path, status
    ) VALUES (
        @client_id, @car_id, @start_date, @end_date, 
        @time_start, @time_end, @place_start, @place_end, 
        @contract_number, @creation_date, @price, @file_path, @status
    )
    RETURNING id;", conn);

    insertCmd.Parameters.AddWithValue("client_id", int.Parse(clientId));
    insertCmd.Parameters.AddWithValue("car_id", int.Parse(carId));
    insertCmd.Parameters.AddWithValue("start_date", StartDatePicker.SelectedDate.Value);
    insertCmd.Parameters.AddWithValue("end_date", EndDatePicker.SelectedDate.Value);
    insertCmd.Parameters.AddWithValue("time_start", timeStart);
    insertCmd.Parameters.AddWithValue("time_end", timeEnd);
    insertCmd.Parameters.AddWithValue("place_start", placestart);
    insertCmd.Parameters.AddWithValue("place_end", placeend);
    insertCmd.Parameters.AddWithValue("contract_number", nextContractNumber);
    insertCmd.Parameters.AddWithValue("creation_date", DateTime.Now);
    insertCmd.Parameters.AddWithValue("price", decimal.Parse(stoimost));
    insertCmd.Parameters.AddWithValue("file_path", savePath);
    insertCmd.Parameters.AddWithValue("status", "not_confirmed");  // Добавлено

    insertedContractId = (int)insertCmd.ExecuteScalar();
}

            // 2. Вставка аренды и обновление договора
            using (var conn = new BD(connStr).GetConnection())
            {
                conn.Open();

               
            }
        }
    }
}

