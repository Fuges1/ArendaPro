using System.Collections.Generic;
using System.Windows;

namespace ArendaPro
{

    public partial class SelectColumnDialog : Window
    {
        public string SelectedColumn => ColumnComboBox.SelectedItem?.ToString();

        public SelectColumnDialog(List<string> columns)
        {
            InitializeComponent();
            ColumnComboBox.ItemsSource = columns;
            if (columns.Count > 0)
                ColumnComboBox.SelectedIndex = 0;
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            if (ColumnComboBox.SelectedItem == null)
            {
                MessageBox.Show("Выберите столбец.");
                return;
            }

            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }

}
