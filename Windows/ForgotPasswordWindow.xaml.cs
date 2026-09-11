using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using DesktopApp.Models;
using DesktopApp.Services;

namespace DesktopApp.Windows
{
    /// <summary>
    /// Logique d'interaction pour ForgotPasswordWindow.xaml
    /// Gère la réinitialisation du mot de passe avec contrôle de saisie en 4 étapes.
    /// </summary>
    public partial class ForgotPasswordWindow : Window
    {
        private readonly AuthenticationService _authService;
        private int _currentStep = 1;
        private User _targetUser = null;
        private string _generatedSecurityCode = "";

        public ForgotPasswordWindow()
        {
            InitializeComponent();
            _authService = new AuthenticationService();
            UpdateStepUI();
        }

        public string ResetUsername { get; private set; }

        private void BtnNext_Click(object sender, RoutedEventArgs e)
        {
            HideError();

            if (_currentStep == 1)
            {
                // Étape 1 : Contrôle de saisie de l'identifiant (Username ou Email)
                string identifier = txtIdentifier.Text?.Trim();

                if (string.IsNullOrWhiteSpace(identifier))
                {
                    ShowError("⚠️ Veuillez entrer votre nom d'utilisateur ou adresse email.");
                    MarkBorderError(borderIdentifier, true);
                    return;
                }

                MarkBorderError(borderIdentifier, false);

                // Rechercher l'utilisateur
                _targetUser = _authService.FindUserByUsernameOrEmail(identifier);
                if (_targetUser == null)
                {
                    ShowError("❌ Aucun compte d'utilisateur correspondant n'a été trouvé.");
                    MarkBorderError(borderIdentifier, true);
                    return;
                }

                // Générer un code de sécurité à 6 chiffres
                Random rng = new Random();
                _generatedSecurityCode = rng.Next(100000, 999999).ToString();

                txtCodeNotice.Text = $"🔑 Code de sécurité généré pour le compte '{_targetUser.Username}' ({_targetUser.Email}) :\n\n" +
                                     $"👉 Votre Code : {_generatedSecurityCode}\n\n" +
                                     $"(Saisissez ce code ci-dessous pour vérifier votre identité)";

                _currentStep = 2;
                UpdateStepUI();
            }
            else if (_currentStep == 2)
            {
                // Étape 2 : Contrôle du code de sécurité
                string inputCode = txtSecurityCode.Text?.Trim();

                if (string.IsNullOrWhiteSpace(inputCode) || inputCode.Length != 6)
                {
                    ShowError("⚠️ Le code de sécurité doit contenir exactement 6 chiffres.");
                    MarkBorderError(borderCode, true);
                    return;
                }

                if (inputCode != _generatedSecurityCode)
                {
                    ShowError("❌ Code de sécurité incorrect. Veuillez vérifier et réessayer.");
                    MarkBorderError(borderCode, true);
                    return;
                }

                MarkBorderError(borderCode, false);
                _currentStep = 3;
                UpdateStepUI();
            }
            else if (_currentStep == 3)
            {
                // Étape 3 : Contrôle du nouveau mot de passe
                string newPwd = pwdNewPassword.Password;
                string confirmPwd = pwdConfirmPassword.Password;

                if (!AuthenticationService.ValidatePassword(newPwd, out string pwdError))
                {
                    ShowError(pwdError);
                    MarkBorderError(borderNewPassword, true);
                    return;
                }

                if (newPwd != confirmPwd)
                {
                    ShowError("⚠️ Les deux mots de passe ne correspondent pas.");
                    MarkBorderError(borderConfirmPassword, true);
                    return;
                }

                MarkBorderError(borderNewPassword, false);
                MarkBorderError(borderConfirmPassword, false);

                // Exécuter la réinitialisation dans la BD et le service
                bool resetOk = _authService.ResetPassword(_targetUser.Username, newPwd);
                if (resetOk)
                {
                    ResetUsername = _targetUser.Username;
                    _currentStep = 4;
                    UpdateStepUI();
                }
                else
                {
                    ShowError("❌ Erreur lors de la réinitialisation du mot de passe dans la base de données.");
                }
            }
            else if (_currentStep == 4)
            {
                // Fermer et revenir à la connexion
                this.DialogResult = true;
                this.Close();
            }
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            HideError();
            if (_currentStep > 1 && _currentStep < 4)
            {
                _currentStep--;
                UpdateStepUI();
            }
        }

        private void UpdateStepUI()
        {
            panelStep1.Visibility = _currentStep == 1 ? Visibility.Visible : Visibility.Collapsed;
            panelStep2.Visibility = _currentStep == 2 ? Visibility.Visible : Visibility.Collapsed;
            panelStep3.Visibility = _currentStep == 3 ? Visibility.Visible : Visibility.Collapsed;
            panelStep4.Visibility = _currentStep == 4 ? Visibility.Visible : Visibility.Collapsed;

            btnBack.Visibility = (_currentStep > 1 && _currentStep < 4) ? Visibility.Visible : Visibility.Collapsed;

            if (_currentStep == 1)
            {
                txtStepSubtitle.Text = "Étape 1/3 : Entrez votre nom d'utilisateur ou adresse email";
                btnNext.Content = "RECHERCHER LE COMPTE";
            }
            else if (_currentStep == 2)
            {
                txtStepSubtitle.Text = "Étape 2/3 : Saisie du code de validation de sécurité";
                btnNext.Content = "VALIDER LE CODE";
            }
            else if (_currentStep == 3)
            {
                txtStepSubtitle.Text = "Étape 3/3 : Définition du nouveau mot de passe";
                btnNext.Content = "RÉINITIALISER LE MOT DE PASSE";
            }
            else if (_currentStep == 4)
            {
                txtStepSubtitle.Text = "Terminé ! Mot de passe réinitialisé";
                btnNext.Content = "SE CONNECTER MAINTENANT";
            }
        }

        #region Contrôle de saisie en temps réel

        private void TxtIdentifier_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(txtIdentifier.Text))
            {
                MarkBorderError(borderIdentifier, false);
                HideError();
            }
        }

        private void TxtSecurityCode_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(txtSecurityCode.Text))
            {
                MarkBorderError(borderCode, false);
                HideError();
            }
        }

        private void PwdNewPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            ValidatePasswordMatchRealtime();
        }

        private void PwdConfirmPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            ValidatePasswordMatchRealtime();
        }

        private void ValidatePasswordMatchRealtime()
        {
            string newPwd = pwdNewPassword.Password;
            string confirmPwd = pwdConfirmPassword.Password;

            if (string.IsNullOrEmpty(newPwd))
            {
                txtPasswordMatchStatus.Text = "• Le mot de passe doit comporter au moins 4 caractères.";
                txtPasswordMatchStatus.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#64748B"));
                return;
            }

            if (newPwd.Length < 4)
            {
                txtPasswordMatchStatus.Text = "⚠️ Mot de passe trop court (minimum 4 caractères).";
                txtPasswordMatchStatus.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DC2626"));
                MarkBorderError(borderNewPassword, true);
                return;
            }

            MarkBorderError(borderNewPassword, false);

            if (string.IsNullOrEmpty(confirmPwd))
            {
                txtPasswordMatchStatus.Text = "✓ Mot de passe valide. Veuillez le confirmer.";
                txtPasswordMatchStatus.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2563EB"));
            }
            else if (newPwd == confirmPwd)
            {
                txtPasswordMatchStatus.Text = "✅ Les mots de passe correspondent parfaitement !";
                txtPasswordMatchStatus.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#166534"));
                MarkBorderError(borderConfirmPassword, false);
                HideError();
            }
            else
            {
                txtPasswordMatchStatus.Text = "❌ Les mots de passe ne correspondent pas.";
                txtPasswordMatchStatus.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DC2626"));
                MarkBorderError(borderConfirmPassword, true);
            }
        }

        #endregion

        #region Helpers d'affichage des erreurs

        private void ShowError(string message)
        {
            // Afficher l'erreur via MessageBox si txtError n'existe pas
            try
            {
                if (txtError != null && borderError != null)
                {
                    txtError.Text = message;
                    borderError.Visibility = Visibility.Visible;
                }
                else
                {
                    MessageBox.Show(message, "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch
            {
                MessageBox.Show(message, "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void HideError()
        {
            try
            {
                if (txtError != null && borderError != null)
                {
                    txtError.Text = "";
                    borderError.Visibility = Visibility.Collapsed;
                }
            }
            catch
            {
                // Ignore si les contrôles n'existent pas
            }
        }

        private void MarkBorderError(Border border, bool isError)
        {
            if (border == null) return;

            try
            {
                if (isError)
                {
                    border.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444"));
                    border.BorderThickness = new Thickness(1.5);
                }
                else
                {
                    border.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CBD5E1"));
                    border.BorderThickness = new Thickness(1.0);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERREUR] MarkBorderError: {ex.Message}");
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }

        #endregion
    }
}
