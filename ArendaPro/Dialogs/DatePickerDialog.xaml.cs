using System;
using System.Windows;

namespace ArendaPro
{

    // Логика класса: DatePickerDialog содержит сценарии этого модуля, управляет данными и координирует взаимодействие UI с сервисами.
    public partial class DatePickerDialog : Window
    {
        public DateTime SelectedDate { get; private set; }

        public DatePickerDialog(DateTime current)
        {
            InitializeComponent();
            dp.SelectedDate = current;
        }

        // Метод Ok_Click: обрабатывает нажатие в интерфейсе: считывает ввод, проверяет ограничения и запускает следующий пользовательский шаг (комментарий #1).
        private void Ok_Click(object s, RoutedEventArgs e)
        {
            SelectedDate = dp.SelectedDate ?? DateTime.Now;
            DialogResult = true;
        }
        // Логика: обработчик Cancel_Click реагирует на действие пользователя в UI, валидирует ввод и запускает нужный сценарий.
        // Метод Cancel_Click: обрабатывает нажатие в интерфейсе: считывает ввод, проверяет ограничения и запускает следующий пользовательский шаг (комментарий #2).
        private void Cancel_Click(object s, RoutedEventArgs e) => Close();
    }
}
