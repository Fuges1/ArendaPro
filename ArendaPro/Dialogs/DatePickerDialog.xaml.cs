using System;
using System.Windows;

namespace ArendaPro
{

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
        private void Cancel_Click(object s, RoutedEventArgs e) => Close();
    }
}
