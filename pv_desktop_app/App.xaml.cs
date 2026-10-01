using System;
using System.Windows;

namespace DesktopApp
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            // Lancer directement la MainWindow qui contient l'application Web intégrale
            MainWindow mainWindow = new MainWindow();
            mainWindow.Show();
        }
    }
}
