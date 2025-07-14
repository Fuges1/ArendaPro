using System;
using System.Data;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ArendaPro
{
    public partial class ClientAdd : Window
    {
        public DataRow NewClientRow { get; private set; }
        private readonly DataTable _clientsSchema;

        public ClientAdd(DataTable clientsSchema)
        {
            InitializeComponent();
            _clientsSchema = clientsSchema;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            var error = ValidateInputs();
            if (error != null)
            {
                MessageBox.Show(error, "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            NewClientRow = _clientsSchema.NewRow();
            NewClientRow["pasport"] = PassportBox.Text.Trim();
            NewClientRow["familia"] = SurnameBox.Text.Trim();
            NewClientRow["imia"] = NameBox.Text.Trim();
            NewClientRow["otchestvo"] = PatronymicBox.Text.Trim();
            NewClientRow["data_rozhdeniya"] = BirthDatePicker.SelectedDate.Value;
            NewClientRow["kem_vydan_pasport"] = IssuerPassportBox.Text.Trim();
            NewClientRow["data_vydachi_pasporta"] = PassportIssueDatePicker.SelectedDate.Value;
            NewClientRow["adres_prozhivaniya"] = AddressBox.Text.Trim();
            NewClientRow["telefon"] = PhoneBox.Text.Trim();
            NewClientRow["voditelskoe_udostoverenie"] = LicenseBox.Text.Trim();
            NewClientRow["data_vydachi_voditelskogo"] = LicenseIssueDatePicker.SelectedDate.Value;

            DialogResult = true;
        }
        private void PassportBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !char.IsDigit(e.Text, 0);
        }
        private void PassportBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            var tb = (TextBox)sender;
            if ((e.Key == Key.Back && tb.CaretIndex == 5) ||
                (e.Key == Key.Delete && tb.CaretIndex == 4))
            {
                e.Handled = true;
                tb.Text = RemoveAt(tb.Text, e.Key == Key.Back ? tb.CaretIndex - 1 : tb.CaretIndex);
                tb.CaretIndex = e.Key == Key.Back ? 4 : 4;
            }
        }
        private void PassportBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var tb = (TextBox)sender;
            int sel = tb.CaretIndex;

            string digits = Regex.Replace(tb.Text, @"\D", "");
            if (digits.Length > 10) digits = digits.Substring(0, 10);

            string formatted = digits.Length > 4
                ? digits.Insert(4, " ")
                : digits;

            tb.Text = formatted;

            if (sel == 5 && formatted.Length > 5)
                tb.CaretIndex = 6;
            else
                tb.CaretIndex = formatted.Length;
        }
        private string RemoveAt(string text, int index)
        {
            if (index < 0 || index >= text.Length) return text;
            return text.Remove(index, 1);
        }
        private void PhoneBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            var tb = (TextBox)sender;
            if (e.Text == "+" && tb.CaretIndex == 0 && !tb.Text.Contains("+"))
                return;
            if (!char.IsDigit(e.Text, 0))
                e.Handled = true;
        }
        private void PhoneBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var tb = (TextBox)sender;
            string txt = tb.Text;

            string filtered = Regex.Replace(txt, @"[^\d+]", "");
            if (filtered.IndexOf('+') > 0)
                filtered = filtered.Replace("+", "");
            if (filtered.StartsWith("+"))
                filtered = "+" + filtered.Substring(1).Replace("+", "");

            if (filtered.Length > 12)
                filtered = filtered.Substring(0, 12);

            if (filtered != txt)
            {
                int pos = tb.CaretIndex;
                tb.Text = filtered;
                tb.CaretIndex = Math.Min(filtered.Length, pos);
            }
        }

        private void LicenseBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !(char.IsLetterOrDigit(e.Text, 0));
        }

        private void LicenseBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var tb = (TextBox)sender;
            string upper = tb.Text.ToUpperInvariant();
            if (tb.Text != upper)
            {
                int pos = tb.CaretIndex;
                tb.Text = upper;
                tb.CaretIndex = pos;
            }
        }
        private string ValidateInputs()
        {
            if (string.IsNullOrWhiteSpace(SurnameBox.Text) ||
                string.IsNullOrWhiteSpace(NameBox.Text))
                return "Укажите фамилию и имя.";

            if (!BirthDatePicker.SelectedDate.HasValue)
                return "Выберите дату рождения.";
            var bd = BirthDatePicker.SelectedDate.Value;
            if (bd > DateTime.Today)
                return "Дата рождения не может быть в будущем.";
            int age = DateTime.Today.Year - bd.Year;
            if (bd > DateTime.Today.AddYears(-age)) age--;
            if (age < 18 || age > 120)
                return "Возраст должен быть от 18 до 120 лет.";

            if (!Regex.IsMatch(PassportBox.Text, @"^\d{4}\s\d{6}$"))
                return "Паспорт: введите 4 цифры, пробел и 6 цифр.";

            if (string.IsNullOrWhiteSpace(IssuerPassportBox.Text))
                return "Укажите орган, выдавший паспорт.";

            if (!PassportIssueDatePicker.SelectedDate.HasValue)
                return "Укажите дату выдачи паспорта.";
            var pi = PassportIssueDatePicker.SelectedDate.Value;
            if (pi > DateTime.Today)
                return "Дата выдачи паспорта не может быть в будущем.";
            if (pi < bd.AddYears(14))
                return "Паспорт выдают не раньше 14 лет.";

            if (string.IsNullOrWhiteSpace(AddressBox.Text))
                return "Укажите адрес проживания.";

            string phone = PhoneBox.Text.Trim();
            if (!Regex.IsMatch(phone, @"^(\+7|8)\d{10}$"))
                return "Телефон должен быть в формате +7XXXXXXXXXX или 8XXXXXXXXXX.";

            if (!Regex.IsMatch(LicenseBox.Text.Trim(), @"^[А-ЯA-Z]{2}\d{6}$", RegexOptions.IgnoreCase))
                return "ВУ: 2 буквы и 6 цифр.";

            if (!LicenseIssueDatePicker.SelectedDate.HasValue)
                return "Укажите дату выдачи водительского удостоверения.";
            var li = LicenseIssueDatePicker.SelectedDate.Value;
            if (li > DateTime.Today)
                return "Дата выдачи ВУ не может быть в будущем.";
            if (li < bd.AddYears(16))
                return "ВУ выдают не раньше 16 лет.";

            return null;
        }
    }
}
