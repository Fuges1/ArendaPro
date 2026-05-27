using System;
using System.Windows;

namespace ArendaPro
{

    // Логика класса: DatePickerDialog инкапсулирует соответствующий экран/сервис и его сценарии работы.
    public partial class DatePickerDialog : Window
    {
        public DateTime SelectedDate { get; private set; }

        public DatePickerDialog(DateTime current)
        {
            InitializeComponent();
            dp.SelectedDate = current;
        }

        private void Ok_Click(object s, RoutedEventArgs e)
        {
            SelectedDate = dp.SelectedDate ?? DateTime.Now;
            DialogResult = true;
        }
        // Логика: метод Cancel_Click выполняет соответствующий шаг бизнес-процесса этого окна/сервиса.
        private void Cancel_Click(object s, RoutedEventArgs e) => Close();
    }
}
