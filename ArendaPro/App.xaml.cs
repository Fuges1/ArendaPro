using System.Windows;

namespace ArendaPro
{
    // Логика класса: App инкапсулирует соответствующий экран/сервис и его сценарии работы.
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