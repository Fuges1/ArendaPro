using System.Windows;

namespace ArendaPro
{
    // Логика класса: App содержит сценарии этого модуля, управляет данными и координирует взаимодействие UI с сервисами.
    public partial class App : Application
    {


        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            LoginWindow loginWindow = new();
            loginWindow.Show();
        }
    }
}
