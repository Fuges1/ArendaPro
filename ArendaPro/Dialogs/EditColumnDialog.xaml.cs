using System.Collections.Generic;
using System.Windows;

namespace ArendaPro
{

    // Логика класса: EditColumnDialog содержит сценарии этого модуля, управляет данными и координирует взаимодействие UI с сервисами.
    public partial class EditColumnDialog : Window
    {
        public string SelectedColumn => (ColumnSelector.SelectedItem as string) ?? "";
        public string NewName => NewNameBox.Text.Trim();
        public string NewType => NewTypeBox.Text.Trim();

        public EditColumnDialog(List<string> columns)
        {
            InitializeComponent();
            ColumnSelector.ItemsSource = columns;
        }

        // Метод Apply_Click: обрабатывает нажатие в интерфейсе: считывает ввод, проверяет ограничения и запускает следующий пользовательский шаг (комментарий #1).
        private void Apply_Click(object sender, RoutedEventArgs e)
        {
            if (ColumnSelector.SelectedItem == null || string.IsNullOrWhiteSpace(NewName) || string.IsNullOrWhiteSpace(NewType))
            {
                MessageBox.Show("Заполните все поля.");
                return;
            }

            this.DialogResult = true;
            this.Close();
        }

        // Метод Close_Click: обрабатывает нажатие в интерфейсе: считывает ввод, проверяет ограничения и запускает следующий пользовательский шаг (комментарий #2).
        private void Close_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
