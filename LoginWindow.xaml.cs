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
            string username = txtUsername.Text;
            
            // Récupérer le mot de passe selon sa visibilité
            string password = isPasswordVisible ? txtPasswordVisible.Text : pwdPassword.Password;

            // Vérifier les champs vides et les marquer en rouge
            bool hasError = false;
            
            if (string.IsNullOrWhiteSpace(username))
            {
                txtUsername.Tag = "Error"; // Marquer en rouge
                hasError = true;
            }
            else
            {
                txtUsername.Tag = null; // Pas d'erreur
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                if (isPasswordVisible)
                    txtPasswordVisible.Tag = "Error"; // Marquer en rouge
                else
                    pwdPassword.Tag = "Error"; // Marquer en rouge
                hasError = true;
            }
            else
            {
                if (isPasswordVisible)
                    txtPasswordVisible.Tag = null; // Pas d'erreur
                else
                    pwdPassword.Tag = null; // Pas d'erreur
            }

            // Si les champs sont marqués en rouge, afficher l'erreur
            if (hasError)
            {
                ShowError("⚠️ Veuillez remplir tous les champs.");
                return;
            }

            // Valider les autres critères
            string validationError = ValidateInputs(username, password);
            if (!string.IsNullOrEmpty(validationError))
            {
                ShowError(validationError);
                if (username.Length < 3)
                    txtUsername.Tag = "Error";
                if (password.Length < 1)
                {
                    if (isPasswordVisible)
                        txtPasswordVisible.Tag = "Error";
                    else
                        pwdPassword.Tag = "Error";
                }
                return;
            }

            if (authService.Authenticate(username, password))
            {
                // Connexion réussie - ouvrir la fenêtre principale
                HideError();
                txtUsername.Tag = null;
                if (isPasswordVisible)
                    txtPasswordVisible.Tag = null;
                else
                    pwdPassword.Tag = null;
                
                MainWindow mainWindow = new MainWindow();
                mainWindow.Show();
                this.Close();
            }
            else
            {
                ShowError("❌ Nom d'utilisateur ou mot de passe incorrect.");
                txtUsername.Tag = "Error";
                if (isPasswordVisible)
                    txtPasswordVisible.Tag = "Error";
                else
                    pwdPassword.Tag = "Error";
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
            if (username.Length < 3)
            {
                return "⚠️ Le nom d'utilisateur doit contenir au moins 3 caractères.";
            }

            // Vérifier la longueur minimale du mot de passe
            if (password.Length < 1)
            {
                return "⚠️ Le mot de passe ne peut pas être vide.";
            }

            // Vérifier les caractères autorisés dans le nom d'utilisateur
            if (!System.Text.RegularExpressions.Regex.IsMatch(username, @"^[a-zA-Z0-9._-]+$"))
            {
                return "⚠️ Le nom d'utilisateur ne peut contenir que des lettres, chiffres, points, tirets et underscores.";
            }

            // Vérifier que le nom d'utilisateur ne commence pas par un espace
            if (char.IsWhiteSpace(username[0]))
            {
                return "⚠️ Le nom d'utilisateur ne doit pas commencer par un espace.";
            }

            // Vérifier que le mot de passe ne contient que des caractères valides
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
                txtUsername.Tag = null; // Enlever la couleur rouge
            }
            
            HideError(); // Effacer les erreurs précédentes
            
            string username = txtUsername.Text?.Trim() ?? string.Empty;
            
            // Debug - afficher ce qui est validé
            Console.WriteLine($"[LOGIN] Validation username: '{username}' (longueur: {username.Length})");
            
            // Vérifier si le champ est vide - pas d'erreur pour champ vide
            if (string.IsNullOrWhiteSpace(username))
            {
                return;
            }

            // Vérifier les caractères autorisés - lettres, chiffres, point, underscore, tiret
            var regex = new System.Text.RegularExpressions.Regex(@"^[a-zA-Z0-9._-]+$");
            if (!regex.IsMatch(username))
            {
                Console.WriteLine($"[LOGIN] ERREUR: Caractères non autorisés dans '{username}'");
                ShowError("⚠️ Le nom d'utilisateur contient des caractères non autorisés. Utilisez seulement des lettres, chiffres, points, tirets et underscores.");
                txtUsername.Tag = "Error";
                return;
            }

            // Vérifier la longueur
            if (username.Length > 50)
            {
                Console.WriteLine($"[LOGIN] ERREUR: Nom trop long '{username}' ({username.Length} caractères)");
                ShowError("⚠️ Le nom d'utilisateur ne doit pas dépasser 50 caractères.");
                txtUsername.Tag = "Error";
                return;
            }
            
            // Si on arrive ici, le nom d'utilisateur est valide
            Console.WriteLine($"[LOGIN] ✅ Nom d'utilisateur valide: '{username}'");
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
                
                // Effacer la marque d'erreur si l'utilisateur tape quelque chose
                if (!string.IsNullOrWhiteSpace(pwdPassword.Password))
                {
                    pwdPassword.Tag = null; // Enlever la couleur rouge
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
                
                // Effacer la marque d'erreur si l'utilisateur tape quelque chose
                if (!string.IsNullOrWhiteSpace(txtPasswordVisible.Text))
                {
                    txtPasswordVisible.Tag = null; // Enlever la couleur rouge
                    HideError();
                }
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
    }
}
