using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Web.WebView2.Core;

namespace DesktopApp
{
    public partial class MainWindow : Window
    {
        private const string WebAppUrl = "http://localhost:4200";

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                string userDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PVDelibDesktop");
                var env = await CoreWebView2Environment.CreateAsync(null, userDataFolder);
                
                await webViewApp.EnsureCoreWebView2Async(env);
                
                if (webViewApp.CoreWebView2 != null)
                {
                    webViewApp.CoreWebView2.Settings.IsStatusBarEnabled = false;
                    webViewApp.CoreWebView2.Settings.AreDevToolsEnabled = true;
                    webViewApp.NavigationCompleted += WebViewApp_NavigationCompleted;
                    
                    webViewApp.Source = new Uri(WebAppUrl);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur d'initialisation du moteur Web : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
                borderLoading.Visibility = Visibility.Collapsed;
            }
        }

        private void WebViewApp_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                borderLoading.Visibility = Visibility.Collapsed;
            });
        }
    }
}