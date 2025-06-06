using Npgsql;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
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

namespace ArendaPro
{
    /// <summary>
    /// Логика взаимодействия для Tarifi.xaml
    /// </summary>
    public partial class Tarifi : Window
    {
        private DataTable tariffTable;
        private BD db;

        public Tarifi()
        {
            InitializeComponent();
            string connStr = ConfigurationManager.ConnectionStrings["DbConnection"]?.ConnectionString;
            db = new BD(connStr);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                using (var conn = db.GetConnection())
                {
                    conn.Open();
                    string query = @"
                SELECT t.car_id, c.marka AS ""Модель"", t.price AS ""Цена (₽)""
                FROM tariffs t
                JOIN cars c ON c.id = t.car_id
                ORDER BY t.car_id; -- сортировка по id
            ";

                    var adapter = new NpgsqlDataAdapter(query, conn);
                    tariffTable = new DataTable();
                    adapter.Fill(tariffTable);

                    TariffGrid.AutoGeneratingColumn += TariffGrid_AutoGeneratingColumn;
                    TariffGrid.ItemsSource = tariffTable.DefaultView;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при загрузке тарифов: " + ex.Message);
            }
        }
        private void TariffGrid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            if (e.Column is DataGridTextColumn column)
            {
                column.CanUserSort = false;

                if (e.PropertyName == "car_id")
                {
                    // Скрываем технический столбец
                    column.Visibility = Visibility.Collapsed;
                }
                else if (e.PropertyName == "Модель")
                {
                    // Запрещаем редактирование названия авто
                    column.IsReadOnly = true;
                }
                else if (e.PropertyName == "Цена (₽)")
                {
                    // Разрешаем редактирование только цены
                    column.IsReadOnly = false;
                }
            }
        }
        private void SaveTariffs_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (var conn = db.GetConnection())
                {
                    conn.Open();

                    foreach (DataRow row in tariffTable.Rows)
                    {
                        if (row.RowState == DataRowState.Modified)
                        {
                            // Тебе нужно хранить car_id где-то — добавь в SELECT скрытую колонку
                            int carId = Convert.ToInt32(row["car_id"]);
                            decimal price = Convert.ToDecimal(row["Цена (₽)"]);

                            var cmd = new NpgsqlCommand("UPDATE tariffs SET price = @price WHERE car_id = @carId", conn);
                            cmd.Parameters.AddWithValue("price", price);
                            cmd.Parameters.AddWithValue("carId", carId);
                            cmd.ExecuteNonQuery();
                        }
                    }
                }

                MessageBox.Show("Тарифы успешно обновлены!");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при сохранении тарифов: " + ex.Message);
            }
        }
    }

}
