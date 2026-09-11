using System;
using System.Windows;
using DesktopApp.Services;

namespace DesktopApp
{
    public partial class LoginWindow : Window
    {
        private  AuthenticationService authService;
        private bool isPasswordVisible = false;

        public LoginWindow()
        {
            InitializeComponent();
            authService = new AuthenticationService();
            
            // Ajouter les validations en temps réel
            txtUsername.TextChanged += TxtUsername_TextChanged;
            pwdPassword.PasswordChanged += PwdPassword_PasswordChanged;
            txtPasswordVisible.TextChanged += TxtPasswordVisible_TextChanged;
        }

        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            string username = txtUsername.Text?.Trim();
            
            // Récupérer le mot de passe selon sa visibilité
            string password = (isPasswordVisible ? txtPasswordVisible.Text : pwdPassword.Password)?.Trim();

            // Vérifier les champs vides et les marquer en rouge
            bool hasError = false;
            
            if (string.IsNullOrWhiteSpace(username))
            {
                MarkBorderError(borderUsername, true); // Contour rouge vif
                hasError = true;
            }
            else
            {
                MarkBorderError(borderUsername, false);
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                MarkBorderError(borderPassword, true); // Contour rouge vif
                hasError = true;
            }
            else
            {
                MarkBorderError(borderPassword, false);
            }

            // Si les champs sont marqués en rouge, afficher l'erreur
            if (hasError)
            {
                ShowError("⚠️ Veuillez remplir tous les champs obligatoires (marqués en rouge).");
                return;
            }

            // Valider les autres critères
            string validationError = ValidateInputs(username, password);
            if (!string.IsNullOrEmpty(validationError))
            {
                ShowError(validationError);
                if (username.Length < 3)
                    MarkBorderError(borderUsername, true);
                if (password.Length < 1)
                    MarkBorderError(borderPassword, true);
                return;
            }

            if (authService.Authenticate(username, password))
            {
                // Connexion réussie - ouvrir la fenêtre principale
                HideError();
                MarkBorderError(borderUsername, false);
                MarkBorderError(borderPassword, false);
                
                MainWindow mainWindow = new MainWindow();
                mainWindow.Show();
                this.Close();
            }
            else
            {
                ShowError("❌ Nom d'utilisateur ou mot de passe incorrect.");
                MarkBorderError(borderUsername, true);
                MarkBorderError(borderPassword, true);
                pwdPassword.Clear();
                txtPasswordVisible.Clear();
            }
        }

        /// <summary>
        /// Valider les champs de saisie
        /// </summary>
        private string ValidateInputs(string username, string password)
        {
            // Vérifier que les champs ne sont pas vides
            if (string.IsNullOrWhiteSpace(username))
            {
                return "⚠️ Veuillez entrer votre nom d'utilisateur.";
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                return "⚠️ Veuillez entrer votre mot de passe.";
            }

            // Vérifier la longueur minimale du nom d'utilisateur
            if (username.Trim().Length < 3)
            {
                return "⚠️ Le nom d'utilisateur doit contenir au moins 3 caractères.";
            }

            // Vérifier les caractères autorisés dans le nom d'utilisateur (lettres, chiffres, points, tirets, underscores, accents et espaces)
            if (!System.Text.RegularExpressions.Regex.IsMatch(username.Trim(), @"^[a-zA-Z0-9._\s\-àáâäãåçèéêëìíîïñòóôöõøùúûüýÿÀÁÂÄÃÅÇÈÉÊËÌÍÎÏÑÒÓÔÖÕØÙÚÛÜÝ]+$"))
            {
                return "⚠️ Le nom d'utilisateur contient des caractères non autorisés.";
            }

            // Vérifier que le mot de passe ne contient pas d'injections dangereuses
            if (password.Contains(";") || password.Contains("'") || password.Contains("\""))
            {
                return "⚠️ Le mot de passe contient des caractères non autorisés.";
            }

            return null; // Pas d'erreur
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        /// <summary>
        /// Événement Clic sur 'Mot de passe oublié ?'
        /// Ouvre la fenêtre de réinitialisation de mot de passe
        /// </summary>
        private void BtnForgotPassword_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var forgotWindow = new DesktopApp.Windows.ForgotPasswordWindow();
            forgotWindow.Owner = this;

            bool? result = forgotWindow.ShowDialog();
            if (result == true && !string.IsNullOrEmpty(forgotWindow.ResetUsername))
            {
                txtUsername.Text = forgotWindow.ResetUsername;
                pwdPassword.Clear();
                txtPasswordVisible.Clear();
                ShowError("✅ Mot de passe réinitialisé ! Veuillez vous connecter avec vos nouveaux identifiants.");
            }
        }

        /// <summary>
        /// Basculer la visibilité du mot de passe
        /// </summary>
        private void BtnTogglePassword_Click(object sender, RoutedEventArgs e)
        {
            if (isPasswordVisible)
            {
                // Masquer le mot de passe
                isPasswordVisible = false;
                pwdPassword.Visibility = Visibility.Visible;
                txtPasswordVisible.Visibility = Visibility.Collapsed;
                btnTogglePassword.Content = "👁️";
                btnTogglePassword.ToolTip = "Afficher le mot de passe";
            }
            else
            {
                // Afficher le mot de passe
                isPasswordVisible = true;
                txtPasswordVisible.Text = pwdPassword.Password;
                pwdPassword.Visibility = Visibility.Collapsed;
                txtPasswordVisible.Visibility = Visibility.Visible;
                btnTogglePassword.Content = "🔒";
                btnTogglePassword.ToolTip = "Masquer le mot de passe";
            }
        }

        /// <summary>
        /// Validation en temps réel du nom d'utilisateur
        /// </summary>
        private void TxtUsername_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            // Effacer la marque d'erreur si l'utilisateur tape quelque chose
            if (!string.IsNullOrWhiteSpace(txtUsername.Text))
            {
                MarkBorderError(borderUsername, false);
            }
            
            HideError(); // Effacer les erreurs précédentes
        }

        /// <summary>
        /// Validation en temps réel du mot de passe (PasswordBox)
        /// </summary>
        private void PwdPassword_PasswordChanged(object sender, System.Windows.RoutedEventArgs e)
        {
            if (!isPasswordVisible) // Seulement si c'est le PasswordBox qui est visible
            {
                // Synchroniser avec le TextBox caché
                txtPasswordVisible.Text = pwdPassword.Password;
                
                if (!string.IsNullOrWhiteSpace(pwdPassword.Password))
                {
                    MarkBorderError(borderPassword, false);
                    HideError();
                }
            }
        }

        /// <summary>
        /// Validation en temps réel du mot de passe (TextBox visible)
        /// </summary>
        private void TxtPasswordVisible_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (isPasswordVisible) // Seulement si c'est le TextBox qui est visible
            {
                // Synchroniser avec le PasswordBox caché
                pwdPassword.Password = txtPasswordVisible.Text;
                
                if (!string.IsNullOrWhiteSpace(txtPasswordVisible.Text))
                {
                    MarkBorderError(borderPassword, false);
                    HideError();
                }
            }
        }

        private void MarkBorderError(System.Windows.Controls.Border border, bool isError)
        {
            if (border == null) return;

            if (isError)
            {
                border.BorderBrush = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#EF4444"));
                border.Background = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FEF2F2"));
                border.BorderThickness = new Thickness(1.8);
            }
            else
            {
                border.BorderBrush = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#CBD5E1"));
                border.Background = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#F8FAFC"));
                border.BorderThickness = new Thickness(1.0);
            }
        }

        /// <summary>
        /// Afficher le message d'erreur en rouge
        /// </summary>
        private void ShowError(string message)
        {
            txtError.Text = message;
            borderError.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// Masquer le message d'erreur
        /// </summary>
        private void HideError()
        {
            txtError.Text = "";
            borderError.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// Permettre de déplacer la fenêtre personnalisée au clic-glisser
        /// </summary>
        private void Window_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }
    }
}
