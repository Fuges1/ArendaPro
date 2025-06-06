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
    /// Логика взаимодействия для AddColumnDialog.xaml
    /// </summary>
    public partial class AddColumnDialog : Window
    {
        public string ColumnName { get; private set; }
        public string DataType { get; private set; }

        public AddColumnDialog()
        {
            InitializeComponent();
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            ColumnName = ColumnNameBox.Text.Trim();
            DataType = DataTypeBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(ColumnName) || string.IsNullOrWhiteSpace(DataType))
            {
                MessageBox.Show("Заполните все поля.");
                return;
            }

            DialogResult = true;
            Close();
        }
    }
}
