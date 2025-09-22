using EDPA.WPF.Services;
using EDPA.WPF.Views.Pages;
using System.Windows;

namespace EDPA.WPF
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var mainWindow = new MainWindow();
            mainWindow.Show();
        }
    }
}
