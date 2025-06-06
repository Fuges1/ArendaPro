using System;
using System.Collections.Generic;
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
    /// Логика взаимодействия для SelectColumnDialog.xaml
    /// </summary>
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
