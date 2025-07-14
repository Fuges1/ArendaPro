using Npgsql;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace ArendaPro
{
    public partial class Tarifi : Window
    {
        private readonly string _cs = ConfigurationManager
    .ConnectionStrings["DbConnection"].ConnectionString;
        private bool _ready;
        private DateTime _lastShowClick = DateTime.MinValue;
        private readonly TimeSpan _doubleClickThreshold = TimeSpan.FromSeconds(1);
        private List<TariffRow> rows;

        private readonly bool isAdmin;
        public class TariffSegment
        {
            public int CarId { get; set; }
            public string Model { get; set; }
            public DateTime Start { get; set; }
            public DateTime? End { get; set; }
            public decimal? Price { get; set; }     
            public bool IsHistory { get; set; }
        }
        private List<TariffSegment> _segments = new();
        public Tarifi(bool isAdminFlag)
        {

            InitializeComponent();
            isAdmin = isAdminFlag;
        }
        public class OverlapInfo
        {
            public DateTime StartDate { get; set; }
            public DateTime? EndDate { get; set; }
            public decimal Price { get; set; }
        }

        private List<(DateTime start, DateTime? end)> ComputeGaps(
     List<OverlapInfo> overlaps,
     DateTime s, DateTime? e)
        {
            var gaps = new List<(DateTime start, DateTime? end)>();
            DateTime cursor = s;

            foreach (var o in overlaps)
            {
                if (o.StartDate > cursor)
                    gaps.Add((start: cursor, end: o.StartDate.AddDays(-1)));

               
                var endOfOverlap = o.EndDate ?? (e ?? DateTime.MaxValue);
                cursor = endOfOverlap.AddDays(1);
            }

            if (cursor <= (e ?? DateTime.MaxValue))
                gaps.Add((start: cursor, end: e));

            
            if (gaps.Count == 2)
            {
                var totalDays = (e.Value - s).Days + 1;

                var coveredDays = overlaps.Sum(o =>
                {
                    var overlapEnd = o.EndDate ?? e.Value;
                    return (overlapEnd - o.StartDate).Days + 1;
                });

                var gapDays = (gaps[0].end.Value - gaps[0].start).Days + 1
                            + (gaps[1].end.Value - gaps[1].start).Days + 1;

                if (coveredDays + gapDays == totalDays)
                    return new List<(DateTime, DateTime?)> { (s, e) };
            }

            return gaps;
        }

        private List<OverlapInfo> GetOverlaps(int carId, DateTime start, DateTime? end)
        {
            var list = new List<OverlapInfo>();
            using var cn = new NpgsqlConnection(_cs);
            cn.Open();

            using var cmd = new NpgsqlCommand(@"
        SELECT start_date, end_date, price
          FROM public.car_tariff_history
         WHERE car_id = @car
           AND tstzrange(start_date, COALESCE(end_date, 'infinity')) &&
               tstzrange(@start, COALESCE(@end, 'infinity'))
         ORDER BY start_date;", cn);
            cmd.Parameters.AddWithValue("car", carId);
            cmd.Parameters.AddWithValue("start", start);
            cmd.Parameters.AddWithValue("end", (object?)end ?? DBNull.Value);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new OverlapInfo
                {
                    StartDate = reader.GetDateTime(0),
                    EndDate = reader.IsDBNull(1) ? null : reader.GetDateTime(1),
                    Price = reader.GetDecimal(2)
                });
            }
            return list;
        }


        private void DeleteOverlaps(int carId, DateTime start, DateTime? end, NpgsqlConnection cn, NpgsqlTransaction tr)
        {
            using var cmd = new NpgsqlCommand(@"
        DELETE FROM public.car_tariff_history
         WHERE car_id = @car
           AND tstzrange(start_date, COALESCE(end_date, 'infinity')) &&
               tstzrange(@start, COALESCE(@end, 'infinity'));", cn, tr);
            cmd.Parameters.AddWithValue("car", carId);
            cmd.Parameters.AddWithValue("start", start);
            cmd.Parameters.AddWithValue("end", (object?)end ?? DBNull.Value);
            cmd.ExecuteNonQuery();
        }

        private void UpdateHistory(
            TariffRow rw,
            NpgsqlConnection cn,
            NpgsqlTransaction tr,
            DateTime s,
            DateTime? e)
        {
            using var upd = new NpgsqlCommand(@"
UPDATE car_tariff_history
SET price      = @price,
    end_date   = @e
WHERE car_id    = @car
  AND start_date = @s", cn, tr);
            upd.Parameters.AddWithValue("price", rw.Price);
            upd.Parameters.AddWithValue("car", rw.CarId);
            upd.Parameters.AddWithValue("s", s);
            upd.Parameters.AddWithValue("e", (object?)e ?? DBNull.Value);
            upd.ExecuteNonQuery();
        }

        private void InsertTemp(int carId, decimal price, DateTime start, DateTime? end, NpgsqlConnection cn, NpgsqlTransaction tr)
        {
            using var cmd = new NpgsqlCommand(@"
        INSERT INTO public.car_tariff_history (car_id, price, start_date, end_date)
        VALUES (@car, @price, @start, @end);", cn, tr);
            cmd.Parameters.AddWithValue("car", carId);
            cmd.Parameters.AddWithValue("price", price);
            cmd.Parameters.AddWithValue("start", start);
            cmd.Parameters.AddWithValue("end", (object?)end ?? DBNull.Value);
            cmd.ExecuteNonQuery();
        }
        private sealed class TariffRow : INotifyPropertyChanged
        {
            public int CarId { get; set; }
            public string Model { get; set; }

            private decimal? price;
            public decimal? Price
            {
                get => price;
                set { price = value; OnChanged(nameof(Price)); }
            }

            public bool FromHistory { get; set; }
            public decimal? OriginalPrice { get; set; }
            public event PropertyChangedEventHandler PropertyChanged;
            private void OnChanged(string p) =>
                PropertyChanged?.Invoke(this, new(p));
        }

        private const string SqlStatic = @"
SELECT c.id, c.marka,
       t.daily_rate      AS price
FROM cars c
LEFT JOIN tariffs t ON t.id = c.tariff_id
ORDER BY c.id;";



        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _ready = true;
            PurgeExpiredHistory();
            ChkTemporary.IsChecked = Properties.Settings.Default.SaveTemporaryMode;
            SetUiState();
            LoadRows();
        }

        private void BtnShow_Click(object s, RoutedEventArgs e)
        {

            LoadRows();
            if (!isAdmin || ChkTemporary.IsChecked != true) return;

            var now = DateTime.Now;
            if (now - _lastShowClick <= _doubleClickThreshold)
            {
                ShowExpiringSoon();
                _lastShowClick = DateTime.MinValue;
            }
            else _lastShowClick = now;
        }
        private void BtnClear_Click(object s, RoutedEventArgs e)
        {
            DpStart.SelectedDate = DateTime.Today;
            DpEnd.SelectedDate = null;
            LoadRows();
        }
        private void ShowExpiringSoon()
        {
            var today = DateTime.Today;
            var soon = today.AddDays(3);
            using var cn = new NpgsqlConnection(_cs);
            cn.Open();
            using var cmd = new NpgsqlCommand(@"
SELECT car_id, start_date, end_date, price
FROM car_tariff_history
WHERE end_date BETWEEN @today AND @soon
ORDER BY end_date", cn);
            cmd.Parameters.AddWithValue("today", today);
            cmd.Parameters.AddWithValue("soon", soon);

            var list = new List<string>();
            using var r = cmd.ExecuteReader();
            while (r.Read())
                list.Add($"Авто {r.GetInt32(0)}: {r.GetDateTime(1):d}–{r.GetDateTime(2):d} ({r.GetDecimal(3):N2} ₽)");

            if (list.Count > 0)
                MessageBox.Show("Скоро истекают:\n" + string.Join("\n", list),
                                "Внимание администратору",
                                MessageBoxButton.OK,
                                MessageBoxImage.Information);
        }
        private void LoadRows()
        {

            if (ChkTemporary.IsChecked == true
     && (!DpStart.SelectedDate.HasValue || !DpEnd.SelectedDate.HasValue))
            {
                TariffGrid.ItemsSource = null;
                TariffGrid.Columns.Clear();
                return;
            }
            bool temp = ChkTemporary.IsChecked == true;
            DateTime d1 = DpStart.SelectedDate ?? DateTime.Today;
            DateTime d2 = DpEnd.SelectedDate ?? DateTime.MaxValue;

            _segments.Clear();

            using var cn = new NpgsqlConnection(_cs);
            cn.Open();

            var baseRates = new Dictionary<int, (string model, decimal price)>();
            using (var cmd = new NpgsqlCommand(SqlStatic, cn))
            using (var r = cmd.ExecuteReader())
                while (r.Read())
                    baseRates[r.GetInt32(0)] = (r.GetString(1), r.GetDecimal(2));

            if (temp)
            {
                foreach (var kv in baseRates)
                {
                    int carId = kv.Key;
                    string model = kv.Value.model;
                    decimal basePrice = kv.Value.price;

                    var overlaps = GetOverlaps(carId, d1, d2).OrderBy(o => o.StartDate).ToList();
                    if (overlaps.Count == 0)
                    {
                        _segments.Add(new TariffSegment
                        {
                            CarId = carId,
                            Model = model,
                            Start = d1,
                            End = d2,
                            Price = null,
                            IsHistory = false
                        });
                        continue;
                    }

                    var gaps = ComputeGaps(overlaps, d1, d2);

                    foreach (var o in overlaps)
                        _segments.Add(new TariffSegment
                        {
                            CarId = carId,
                            Model = model,
                            Start = o.StartDate,
                            End = o.EndDate,
                            Price = o.Price,
                            IsHistory = true
                        });

                    foreach (var g in gaps)
                        _segments.Add(new TariffSegment
                        {
                            CarId = carId,
                            Model = model,
                            Start = g.start,
                            End = g.end,
                            Price = null,         
                            IsHistory = false
                        });
                }
            }
            else
            {
                foreach (var kv in baseRates)
                    _segments.Add(new TariffSegment
                    {
                        CarId = kv.Key,
                        Model = kv.Value.model,
                        Start = DateTime.MinValue,
                        End = null,
                        Price = kv.Value.price,
                        IsHistory = false
                    });
            }

            
            TariffGrid.ItemsSource = null;
            TariffGrid.Columns.Clear();
            TariffGrid.ItemsSource = _segments;


            TariffGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "ID авто",
                Binding = new Binding(nameof(TariffSegment.CarId)),
                Width = 60
            });

            TariffGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "Модель",
                Binding = new Binding(nameof(TariffSegment.Model)),
                Width = 150
            });

            TariffGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "Начало",
                Binding = new Binding(nameof(TariffSegment.Start))
                { StringFormat = "dd.MM.yyyy" },
                Width = 100
            });

            TariffGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "Конец",
                Binding = new Binding(nameof(TariffSegment.End))
                { Converter = new EndDateConverter() },
                Width = 100
            });

            TariffGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "Ставка (₽)",
                Binding = new Binding(nameof(TariffSegment.Price))
                {
                    StringFormat = "N2",
                    TargetNullValue = ""
                },
                Width = 100,
                IsReadOnly = false

            });

        }

        public class EndDateConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
                => value is DateTime dt ? dt.ToString("dd.MM.yyyy") : "∞";
            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
                => Binding.DoNothing;
        }


        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            var segments = (TariffGrid.ItemsSource as IEnumerable<TariffSegment>)?.ToList()
                           ?? new List<TariffSegment>();
            if (segments.Count == 0)
            {
                MessageBox.Show("Нет строк для сохранения.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            foreach (var seg in segments)
            {
                if (!seg.Price.HasValue)
                {
                    MessageBox.Show(
                        $"Строка {seg.Model} / {seg.Start:dd.MM.yyyy} имеет пустую ставку.",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }
            if (ChkTemporary.IsChecked != true)
            {
                MessageBox.Show("Временные тарифы не включены.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (!DpStart.SelectedDate.HasValue || !DpEnd.SelectedDate.HasValue)
            {
                MessageBox.Show("Укажите обе даты периода.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            var sDate = DpStart.SelectedDate.Value;
            var eDate = DpEnd.SelectedDate.Value;
            if (sDate == eDate)
            {
                MessageBox.Show("Период должен быть хотя бы на один день длиннее.", "Ошибка",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            using var cn = new NpgsqlConnection(_cs);
            cn.Open();
            using (var tr = cn.BeginTransaction())
            {
                try
                {
                    foreach (var seg in segments)
                    {
                        var overlaps = GetOverlaps(seg.CarId, sDate, eDate)
                                       .OrderBy(o => o.StartDate)
                                       .ToList();

                        bool allSame = overlaps.Count > 0
                            && overlaps.All(o =>
                                o.Price == seg.Price.Value
                             && o.StartDate == sDate
                             && (o.EndDate ?? DateTime.MaxValue) == eDate);
                        if (allSame)
                            continue;

                        if (overlaps.Count > 0)
                        {
                         
                            var listText = overlaps.Select(o =>
                            {
                                string txt = $"{o.StartDate:dd.MM.yyyy} – {(o.EndDate?.ToString("dd.MM.yyyy") ?? "∞")} : {o.Price:N2} ₽";
                                if (o.Price == seg.Price.Value && o.StartDate == sDate && (o.EndDate ?? DateTime.MaxValue) == eDate)
                                    txt = "** " + txt + " **";
                                return txt;
                            });
                            string message = $"Модель «{seg.Model}» уже имеет временные тарифы:\n\n"
                                           + string.Join("\n", listText)
                                           + $"\n\nПериод: {sDate:dd.MM.yyyy} – {eDate:dd.MM.yyyy}\n\n"
                                           + "Заменить все существующие тарифы на новый?";

                            var res = MessageBox.Show(message,
                                                      "Конфликт временных тарифов",
                                                      MessageBoxButton.YesNo,
                                                      MessageBoxImage.Question);

                            if (res == MessageBoxResult.Yes)
                            {
                                DeleteOverlaps(seg.CarId, sDate, eDate, cn, tr);
                                InsertTemp(seg.CarId, seg.Price.Value, sDate, eDate, cn, tr);
                            }
                            else
                            {
                                tr.Rollback();
                                MessageBox.Show("Операция отменена.", "Отмена", MessageBoxButton.OK, MessageBoxImage.Information);
                                return;
                            }
                        }
                        else
                        {
                            InsertTemp(seg.CarId, seg.Price.Value, sDate, eDate, cn, tr);
                        }
                    }

                    tr.Commit();
                }
                catch (Exception ex)
                {
                    tr.Rollback();
                    MessageBox.Show("Ошибка сохранения: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }

            LoadRows();
            MessageBox.Show("Временные тарифы успешно сохранены.", "Готово", MessageBoxButton.OK, MessageBoxImage.Information);
        }







        private void DpStart_OnSelectedDateChanged(object s, SelectionChangedEventArgs e)
        {
            if (DpStart.SelectedDate is DateTime start)
            {
                DpEnd.IsEnabled = true;
                DpEnd.DisplayDateStart = start;
                if (DpEnd.SelectedDate < start)
                    DpEnd.SelectedDate = null;
            }
            else
            {
                DpEnd.IsEnabled = false;
                DpEnd.SelectedDate = null;
            }
        }
        private void SetUiState()
        {
            bool temp = ChkTemporary.IsChecked == true;
            DpStart.IsEnabled =
            DpEnd.IsEnabled =
            BtnShow.IsEnabled =
            BtnClear.IsEnabled = temp;
        }
        private void DpEnd_OnSelectedDateChanged(object s, SelectionChangedEventArgs e)
        {
            if (DpEnd.SelectedDate < DpStart.SelectedDate)
                DpEnd.SelectedDate = null;
        }
        private void PurgeExpiredHistory()
        {
            using var cn = new NpgsqlConnection(_cs);
            cn.Open();
            using var cmd = new NpgsqlCommand(
                "DELETE FROM car_tariff_history WHERE end_date < @today", cn);
            cmd.Parameters.AddWithValue("today", DateTime.Today);
            cmd.ExecuteNonQuery();
        }
        private void ChkTemporary_OnChanged(object s, RoutedEventArgs e)
        {
            if (!_ready) return;
            SetUiState();
            Properties.Settings.Default.SaveTemporaryMode = ChkTemporary.IsChecked == true;
            Properties.Settings.Default.Save();

            if (ChkTemporary.IsChecked != true)
            {
                DpStart.SelectedDate = null;
                DpEnd.SelectedDate = null;
            }

            PurgeExpiredHistory();
            LoadRows();
        }

        private void BtnClose_Click(object s, RoutedEventArgs e) => Close();
    }
}
