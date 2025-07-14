using System.Windows;

namespace ArendaPro
{
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