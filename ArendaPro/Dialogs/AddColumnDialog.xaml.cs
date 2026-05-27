using System.Windows;

namespace ArendaPro
{

    // Логика класса: AddColumnDialog содержит сценарии этого модуля, управляет данными и координирует взаимодействие UI с сервисами.
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
