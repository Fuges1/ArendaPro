using ClosedXML.Excel;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace ArendaPro
{

    public partial class OtherOborot : Window
    {
        private string Fullusername;
        private readonly string connStr = ConfigurationManager.ConnectionStrings["DbConnection"]?.ConnectionString;
        private readonly BD database;
        public static class CurrentSession
        {
            public static int UserId { get; set; }
            public static string Role { get; set; }
            public static string FullName { get; set; }
            public static string Email { get; set; }

        }
        public OtherOborot(string Fullname)
        {
            Fullusername = Fullname;
            InitializeComponent();
            database = new BD(connStr);
            DateFrom.SelectedDate = DateTime.Today.AddMonths(-1);
            DateTo.SelectedDate = DateTime.Today;
        }

        private void ExportToExcel_PerfectLayout(
     string path,
     DataTable table,
     IList<DataGridColumn> columns,
     DateTime from,
     DateTime to,
     string author,
     string summaryText)
        {
            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Оборот");

            int colCount = columns.Count;
            string lastCol = GetExcelColumnName(colCount);

            ws.Range($"A1:{lastCol}1").Merge();
            ws.Cell("A1").SetValue("ОБОРОТ")
                .Style.Font.SetBold().Font.SetFontSize(16)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            ws.Range($"A2:{lastCol}2").Merge();
            ws.Cell("A2").SetValue($"Период: {from:dd.MM.yyyy} – {to:dd.MM.yyyy}")
                .Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            ws.Range($"A3:{lastCol}3").Merge();
            ws.Cell("A3").SetValue($"Составлен: {DateTime.Now:dd.MM.yyyy HH:mm}    Автор: {author}")
                .Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            var lines = summaryText
                .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < lines.Length; i++)
            {
                int row = 4 + i;
                ws.Range($"A{row}:{lastCol}{row}").Merge();
                ws.Cell(row, 1)
                  .SetValue(lines[i]);
            }

            int headerRow = 4 + lines.Length + 1;
            for (int c = 0; c < colCount; c++)
            {
                ws.Cell(headerRow, c + 1)
                  .SetValue(columns[c].Header.ToString())
                  .Style.Font.SetBold();
            }

            for (int r = 0; r < table.Rows.Count; r++)
            {
                for (int c = 0; c < colCount; c++)
                {
                    object raw;
                    if (columns[c] is DataGridBoundColumn bound
                        && bound.Binding is System.Windows.Data.Binding b
                        && table.Columns.Contains(b.Path.Path))
                    {
                        raw = table.Rows[r][b.Path.Path];
                    }
                    else
                    {
                        raw = table.Rows[r][c];
                    }
                    string text = raw == null || raw == DBNull.Value
                        ? ""
                        : raw.ToString();

                    ws.Cell(headerRow + 1 + r, c + 1)
                      .SetValue(text);
                }
            }

            var dataRange = ws.Range(
                headerRow, 1,
                headerRow + table.Rows.Count, colCount);
            var tbl = dataRange.CreateTable();
            tbl.Theme = XLTableTheme.TableStyleMedium9;

            ws.Columns().AdjustToContents();
            ws.Rows().AdjustToContents();

            wb.SaveAs(path);
        }

        private string GetExcelColumnName(int columnNumber)
        {
            string columnName = "";
            while (columnNumber > 0)
            {
                int rem = (columnNumber - 1) % 26;
                columnName = (char)('A' + rem) + columnName;
                columnNumber = (columnNumber - 1) / 26;
            }
            return columnName;
        }
        private void GenerateReport_Click(object sender, RoutedEventArgs e)
        {
            if (!(ReportGrid.ItemsSource is DataView dv)
                || !DateFrom.SelectedDate.HasValue
                || !DateTo.SelectedDate.HasValue) return;

            DateTime from = DateFrom.SelectedDate.Value.Date;
            DateTime to = DateTo.SelectedDate.Value.Date;

            var table = dv.Table;
            var visibleColumns = ReportGrid.Columns
                .Where(c => c.Header != null && !string.IsNullOrWhiteSpace(c.Header.ToString()))
                .ToList();

            string filter = (FilterComboBox.SelectedItem as ComboBoxItem)?.Content as string ?? "";
            var sb = new StringBuilder();

            switch (filter)
            {
                case "Все операции":
                    decimal totalAll = table.Rows.Cast<DataRow>()
                        .Sum(r => Convert.ToDecimal(r["total_amount"]));
                    sb.AppendLine($"Всего договоров: {table.Rows.Count}");
                    sb.AppendLine($"Общая сумма оборота: {totalAll:N2} ₽");
                    break;

                case "По сотрудникам":
                    sb.AppendLine("По сотрудникам:");
                    var byEmp = table.Rows.Cast<DataRow>()
                        .GroupBy(r => r["employee_name"].ToString())
                        .OrderByDescending(g => g.Sum(r => Convert.ToDecimal(r["total_amount"])));
                    foreach (var grp in byEmp)
                    {
                        int cnt = grp.Count();
                        decimal sum = grp.Sum(r => Convert.ToDecimal(r["total_amount"]));
                        sb.AppendLine($"{grp.Key}: {cnt} шт., сумма {sum:N2} ₽");
                    }
                    break;

                case "По тарифам":
                    sb.AppendLine("По тарифам:");
                    var byRate = table.Rows.Cast<DataRow>()
                        .GroupBy(r => Convert.ToDecimal(r["price"]))
                        .OrderByDescending(g => g.Key);
                    foreach (var grp in byRate)
                    {
                        int cnt = grp.Count();
                        decimal sum = grp.Sum(r => Convert.ToDecimal(r["total_amount"]));
                        sb.AppendLine($"₽{grp.Key:N2}: {cnt} шт., сумма {sum:N2} ₽");
                    }
                    break;

                case "По договорам":
                    sb.AppendLine("Список договоров:");
                    sb.Append(string.Join(", ",
                        table.Rows.Cast<DataRow>()
                             .Select(r => r["contract_number"].ToString())));
                    break;
            }

            string summaryText = sb.ToString().TrimEnd();

            var dlg = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Excel Workbook|*.xlsx",
                FileName = $"Оборот_{from:yyyyMMdd}_{to:yyyyMMdd}.xlsx"
            };
            if (dlg.ShowDialog() != true) return;

            ExportToExcel_PerfectLayout(
                dlg.FileName,
                table,
                visibleColumns,
                from,
                to,
                Fullusername,
                summaryText
            );

            MessageBox.Show("Отчёт успешно сохранён.", "Готово", MessageBoxButton.OK, MessageBoxImage.Information);
        }



        private void Show_Click(object sender, RoutedEventArgs e)
        {
            if (DateFrom.SelectedDate == null || DateTo.SelectedDate == null)
            {
                MessageBox.Show("Выберите обе даты!");
                return;
            }

            DateTime from = DateFrom.SelectedDate.Value.Date;
            DateTime to = DateTo.SelectedDate.Value.Date.AddDays(1);

            try
            {
                string query = @"
SELECT
    c.id               AS contract_number,
    cl.familia||' '||cl.imia||' '||cl.otchestvo   AS client_fullname,
    cr.marka||' ('||cr.gos_nomer||')'             AS car_info,
    c.start_date,
    c.end_date,
    u.last_name||' '||u.first_name                 AS employee_name,
    p1.name                                       AS start_place,
    p2.name                                       AS end_place,
    
     c.price          AS price,          -- ставка за день
    c.extra_amount   AS extra_amount,   -- доплата
    -- вычисляем дни (включительно) * ставка + доплата
    ((c.end_date - c.start_date + 1) * c.price + c.extra_amount)
                     AS total_amount,   -- итог
    c.paid_amount    AS paid_amount,    -- оплачено
    -- долг = итог – оплачено
    ((c.end_date - c.start_date + 1) * c.price + c.extra_amount
     - c.paid_amount)
                     AS debt,           -- долг
    cs.description   AS status
FROM contracts c
  JOIN clients cl  ON c.client_id = cl.id
  JOIN cars    cr  ON c.car_id    = cr.id
  LEFT JOIN users   u   ON c.user_id        = u.id
  LEFT JOIN places  p1  ON c.place_start_id = p1.id
  LEFT JOIN places  p2  ON c.place_end_id   = p2.id
  LEFT JOIN contract_statuses cs ON c.status = cs.code
WHERE c.start_date BETWEEN @from AND @to
ORDER BY c.start_date DESC;

";


                var rentals = database.ExecuteQuery(query, new Dictionary<string, object> {
            { "@from", from },
            { "@to",   to   }
        });

                ReportGrid.AutoGenerateColumns = false;

                ReportGrid.ItemsSource = rentals.DefaultView;
                ReportGrid.UpdateLayout();
                foreach (var col in ReportGrid.Columns)
                {
                    col.Width = new DataGridLength(1, DataGridLengthUnitType.SizeToCells);
                }
                var filter = (FilterComboBox.SelectedItem as ComboBoxItem)?.Content as string;

                var sb = new StringBuilder();
                sb.AppendLine($"Период: {from:dd.MM.yyyy} – {to.AddDays(-1):dd.MM.yyyy}");
                sb.AppendLine($"Всего договоров: {rentals.Rows.Count}");

                switch (filter)
                {
                    case "Все операции":
                        decimal totalAll = rentals.Rows.Cast<DataRow>()
                            .Sum(r => Convert.ToDecimal(r["total_amount"]));
                        sb.AppendLine($"Общая сумма оборота: {totalAll:N2} ₽");
                        break;

                    case "По сотрудникам":
                        sb.AppendLine();
                        sb.AppendLine("По сотрудникам:");
                        var byEmp = rentals.AsEnumerable()
                                    .GroupBy(r => r.Field<string>("employee_name"))
                                    .OrderByDescending(g => g.Sum(r => r.Field<decimal>("total_amount")));
                        foreach (var grp in byEmp)
                        {
                            var count = grp.Count();
                            var sum = grp.Sum(r => r.Field<decimal>("total_amount"));
                            sb.AppendLine($"{grp.Key}: {count} шт., сумма {sum:N2} ₽");
                        }
                        break;

                    case "По тарифам":
                        sb.AppendLine();
                        sb.AppendLine("По тарифам (ставка/день):");
                        var byRate = rentals.AsEnumerable()
                                    .GroupBy(r => r.Field<decimal>("price"))
                                    .OrderByDescending(g => g.Key);
                        foreach (var grp in byRate)
                        {
                            var count = grp.Count();
                            var sum = grp.Sum(r => r.Field<decimal>("total_amount"));
                            sb.AppendLine($"₽{grp.Key:N2}: {count} шт., сумма {sum:N2} ₽");
                        }
                        break;

                    case "По договорам":
                        sb.AppendLine();
                        sb.AppendLine("Список договоров:");
                        foreach (DataRow row in rentals.Rows)
                        {
                            sb.Append($"{row["contract_number"]} ");
                        }
                        break;
                }

                SummaryText.Text = sb.ToString();
                SummaryText.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке данных: {ex.Message}");
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}



