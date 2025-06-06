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
    /// Логика взаимодействия для EditColumnDialog.xaml
    /// </summary>
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

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
