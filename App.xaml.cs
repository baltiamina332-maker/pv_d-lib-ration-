using System;
using System.Windows;
using DesktopApp.Services;

namespace DesktopApp
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            // Afficher la fenêtre de connexion au lieu de MainWindow
            LoginWindow loginWindow = new LoginWindow();
            loginWindow.Show();
        }
    }
}
