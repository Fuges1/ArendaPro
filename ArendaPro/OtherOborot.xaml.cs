using System;
using System.Collections.Generic;
using System.Configuration;
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
    /// Логика взаимодействия для OtherOborot.xaml
    /// </summary>
    public partial class OtherOborot : Window
    {
        private readonly string connStr = ConfigurationManager.ConnectionStrings["DbConnection"]?.ConnectionString;
        private readonly BD database;

        public OtherOborot()
        {
            InitializeComponent();
            database = new BD(connStr);
            DateFrom.SelectedDate = DateTime.Today.AddMonths(-1);
            DateTo.SelectedDate = DateTime.Today;
        }

        private void Show_Click(object sender, RoutedEventArgs e)
        {
            if (DateFrom.SelectedDate == null || DateTo.SelectedDate == null)
            {
                MessageBox.Show("Выберите обе даты!");
                return;
            }

            DateTime from = DateFrom.SelectedDate.Value;
            DateTime to = DateTo.SelectedDate.Value;

            string query = @"
               SELECT * FROM rental_report
               WHERE start_date BETWEEN @from AND @to
               ORDER BY start_date ASC;";

            string summaryQuery = @"
                SELECT COUNT(*) AS total_rentals, SUM(price) AS total_revenue
                FROM rental_report
                WHERE start_date BETWEEN @from AND @to;";

            var rentals = database.ExecuteQuery(query, new Dictionary<string, object>
            {
                { "@from", from },
                { "@to", to }
            });

            var summary = database.ExecuteQuery(summaryQuery, new Dictionary<string, object>
            {
                { "@from", from },
                { "@to", to }
            });

            ReportGrid.Columns.Clear(); // очищаем старые столбцы
            ReportGrid.ItemsSource = rentals.DefaultView;

            // Установка русских заголовков
            var headers = new Dictionary<string, string>
            {
                { "client_name", "Клиент" },
                { "car_model", "Автомобиль" },
                { "start_date", "Начало аренды" },
                { "end_date", "Конец аренды" },
                { "price", "Цена" }
            };

            foreach (DataGridColumn column in ReportGrid.Columns)
            {
                if (column is DataGridTextColumn textColumn &&
                    column.Header is string header &&
                    headers.TryGetValue(header, out string russianHeader))
                {
                    column.Header = russianHeader;
                    column.CanUserSort = false; // отключаем ручную сортировку
                }
            }

            if (summary.Rows.Count > 0)
            {
                int count = Convert.ToInt32(summary.Rows[0]["total_rentals"]);
                decimal sum = summary.Rows[0]["total_revenue"] != DBNull.Value
                    ? Convert.ToDecimal(summary.Rows[0]["total_revenue"])
                    : 0;
                SummaryText.Text = $"Количество аренд: {count}, общий оборот: {sum}₽";
            }
        }
    }
}
