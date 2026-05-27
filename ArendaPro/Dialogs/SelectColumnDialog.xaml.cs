using System.Collections.Generic;
using System.Windows;

namespace ArendaPro
{

    // Логика класса: SelectColumnDialog содержит сценарии этого модуля, управляет данными и координирует взаимодействие UI с сервисами.
    public partial class SelectColumnDialog : Window
    {
        public string SelectedColumn => ColumnComboBox.SelectedItem?.ToString();

        public SelectColumnDialog(IReadOnlyList<string> columns)
        {
            InitializeComponent();
            ColumnComboBox.ItemsSource = columns;
            if (columns.Count > 0)
                ColumnComboBox.SelectedIndex = 0;
        }

        // Метод Ok_Click: обрабатывает нажатие в интерфейсе: считывает ввод, проверяет ограничения и запускает следующий пользовательский шаг (комментарий #1).
        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            if (ColumnComboBox.SelectedItem == null)
            {
                MessageBox.Show("Выберите столбец.");
                return;
            }

            DialogResult = true;
        }

        // Метод Cancel_Click: обрабатывает нажатие в интерфейсе: считывает ввод, проверяет ограничения и запускает следующий пользовательский шаг (комментарий #2).
        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }

}
