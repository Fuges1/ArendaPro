using System.Text.RegularExpressions;
using System.Windows;

namespace ArendaPro
{

    public partial class InputDialog : Window
    {
        public string InputText { get; private set; }

        public InputDialog()
        {
            InitializeComponent();
        }
        public string RealName { get; private set; }
        public string DisplayName { get; private set; }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            string realName = tableNameRealTextBox.Text.Trim();
            string displayName = tableNameDisplayTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(realName) || string.IsNullOrWhiteSpace(displayName))
            {
                MessageBox.Show("Оба поля обязательны для заполнения.");
                return;
            }

            if (!Regex.IsMatch(realName, @"^[a-zA-Z_][a-zA-Z0-9_]*$"))
            {
                MessageBox.Show("Имя таблицы (в БД) может содержать только латиницу, цифры и подчёркивания, и не начинаться с цифры.");
                return;
            }

            RealName = realName;
            DisplayName = displayName;

            DialogResult = true;
            Close();
        }


        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

    }
}
