using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Windows;
using System.Configuration;
using DesktopApp.Models;
using DesktopApp.Services;
using Microsoft.Win32;

namespace DesktopApp.Windows
{
    public partial class MailingWindow : Window
    {
        private ObservableCollection<TeacherMailItem> teachers;
        private string selectedPVFile = "";

        public MailingWindow() : this(null)
        {
        }

        public MailingWindow(string initialPVFilePath)
        {
            InitializeComponent();
            LoadSmtpSettings();
            InitializeTeachers();

            if (!string.IsNullOrEmpty(initialPVFilePath) && File.Exists(initialPVFilePath))
            {
                selectedPVFile = initialPVFilePath;
                txtPVFilePath.Text = Path.GetFileName(selectedPVFile);
                UpdateSummary();
            }
        }

        /// <summary>
        /// Charger les paramètres SMTP depuis App.config dans l'interface
        /// </summary>
        private void LoadSmtpSettings()
        {
            try
            {
                string server = ConfigurationManager.AppSettings["SmtpServer"] ?? "smtp.gmail.com";
                string port = ConfigurationManager.AppSettings["SmtpPort"] ?? "587";
                string email = ConfigurationManager.AppSettings["SmtpEmail"] ?? "";
                string password = ConfigurationManager.AppSettings["SmtpPassword"] ?? "";
                bool ssl = ConfigurationManager.AppSettings["SmtpEnableSsl"] != "false";

                txtSmtpServer.Text = server;
                txtSmtpPort.Text = port;
                txtSmtpEmail.Text = email;
                pwdSmtpPassword.Password = password;
                chkSmtpSsl.IsChecked = ssl;

                // Ouvrir automatiquement le panneau de configuration si les identifiants sont incomplets ou par défaut
                if (string.IsNullOrWhiteSpace(email) || email.Contains("votre_email") || string.IsNullOrWhiteSpace(password) || password.Contains("votre_mot_de_passe"))
                {
                    expanderSmtp.IsExpanded = true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LoadSmtpSettings Error] {ex.Message}");
            }
        }

        /// <summary>
        /// Sauvegarder la configuration SMTP saisie dans App.config
        /// </summary>
        private void SaveSmtpSettingsToConfig(string server, string port, string email, string password, bool ssl)
        {
            try
            {
                var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                
                SetOrAddAppSetting(config, "SmtpServer", server);
                SetOrAddAppSetting(config, "SmtpPort", port);
                SetOrAddAppSetting(config, "SmtpEmail", email);
                SetOrAddAppSetting(config, "SmtpPassword", password);
                SetOrAddAppSetting(config, "SmtpEnableSsl", ssl ? "true" : "false");

                config.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection("appSettings");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SaveSmtpSettings Error] {ex.Message}");
            }
        }

        private void SetOrAddAppSetting(System.Configuration.Configuration config, string key, string value)
        {
            if (config.AppSettings.Settings[key] != null)
                config.AppSettings.Settings[key].Value = value;
            else
                config.AppSettings.Settings.Add(key, value);
        }

        /// <summary>
        /// Charger tous les enseignants depuis la base de données
        /// </summary>
        private void InitializeTeachers()
        {
            try
            {
                teachers = new ObservableCollection<TeacherMailItem>();
                
                var authService = new AuthenticationService();
                var allUsers = authService.GetAllUsers();

                // Filtrer uniquement les enseignants
                foreach (var user in allUsers)
                {
                    if (user.Role == UserRole.Enseignant)
                    {
                        bool hasEmail = !string.IsNullOrWhiteSpace(user.Email);
                        teachers.Add(new TeacherMailItem
                        {
                            Id = user.Id,
                            FullName = string.IsNullOrWhiteSpace(user.FullName) ? user.Username : user.FullName,
                            Email = user.Email ?? "",
                            Role = "Enseignant",
                            IsSelected = hasEmail // Coché par défaut si email présent
                        });
                    }
                }

                dgTeachers.ItemsSource = teachers;
                UpdateSummary();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement des enseignants: {ex.Message}", "Erreur");
            }
        }

        /// <summary>
        /// Parcourir et sélectionner un fichier PV
        /// </summary>
        private void BtnBrowsePV_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Fichiers Word (*.docx)|*.docx|Fichiers PDF (*.pdf)|*.pdf|Tous les fichiers (*.*)|*.*",
                Title = "Sélectionner un fichier PV"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                selectedPVFile = openFileDialog.FileName;
                txtPVFilePath.Text = Path.GetFileName(selectedPVFile);
                UpdateSummary();
            }
        }

        /// <summary>
        /// Sélectionner tous les enseignants
        /// </summary>
        private void BtnSelectAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var teacher in teachers)
            {
                teacher.IsSelected = true;
            }
            dgTeachers.Items.Refresh();
            UpdateSummary();
        }

        /// <summary>
        /// Désélectionner tous les enseignants
        /// </summary>
        private void BtnDeselectAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var teacher in teachers)
            {
                teacher.IsSelected = false;
            }
            dgTeachers.Items.Refresh();
            UpdateSummary();
        }

        /// <summary>
        /// Mettre à jour le résumé du mailing
        /// </summary>
        private void UpdateSummary()
        {
            string fileName = string.IsNullOrEmpty(selectedPVFile) ? "Aucun fichier" : Path.GetFileName(selectedPVFile);
            int selectedCount = teachers.Count(t => t.IsSelected);
            txtSummary.Text = $"Fichier: {fileName} | {selectedCount} enseignant(s) sélectionné(s)";
        }

        /// <summary>
        /// Envoyer le mailing à tous les enseignants sélectionnés (Asynchrone)
        /// </summary>
        private async void BtnSendMailing_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 1. Récupérer et valider la configuration SMTP saisie
                string smtpEmail = txtSmtpEmail.Text?.Trim() ?? "";
                string smtpPassword = pwdSmtpPassword.Password?.Trim() ?? "";
                string smtpServer = txtSmtpServer.Text?.Trim() ?? "smtp.gmail.com";
                string smtpPortStr = txtSmtpPort.Text?.Trim() ?? "587";
                bool enableSsl = chkSmtpSsl.IsChecked ?? true;

                if (string.IsNullOrWhiteSpace(smtpEmail) || smtpEmail.Contains("votre_email"))
                {
                    expanderSmtp.IsExpanded = true;
                    txtSmtpEmail.Focus();
                    MessageBox.Show(
                        "Veuillez saisir votre adresse email d'expéditeur dans la section '⚙️ Configuration du Compte d'Envoi SMTP'.", 
                        "Email expéditeur manquant", 
                        MessageBoxButton.OK, 
                        MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(smtpPassword) || smtpPassword.Contains("votre_mot_de_passe"))
                {
                    expanderSmtp.IsExpanded = true;
                    pwdSmtpPassword.Focus();
                    MessageBox.Show(
                        "Veuillez saisir votre mot de passe d'application dans la section '⚙️ Configuration du Compte d'Envoi SMTP'.\n\nPour Gmail : Utilisez un mot de passe d'application à 16 caractères (Sécurité Google > Mots de passe d'application).", 
                        "Mot de passe SMTP manquant", 
                        MessageBoxButton.OK, 
                        MessageBoxImage.Warning);
                    return;
                }

                if (!int.TryParse(smtpPortStr, out int smtpPort))
                {
                    smtpPort = 587;
                }

                // Sauvegarder dans App.config pour les envois futurs
                SaveSmtpSettingsToConfig(smtpServer, smtpPortStr, smtpEmail, smtpPassword, enableSsl);

                // 2. Validation du fichier PV et des destinataires
                if (string.IsNullOrEmpty(selectedPVFile) || !File.Exists(selectedPVFile))
                {
                    MessageBox.Show("Veuillez sélectionner un fichier PV valide.", "Fichier manquant", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var selectedTeachers = teachers.Where(t => t.IsSelected).ToList();
                if (selectedTeachers.Count == 0)
                {
                    MessageBox.Show("Veuillez sélectionner au moins un enseignant destinataire.", "Destinataire manquant", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string subject = string.IsNullOrWhiteSpace(txtEmailSubject.Text) ? "📄 Procès-Verbal de Délibération" : txtEmailSubject.Text.Trim();
                string messageBody = txtEmailMessage.Text?.Trim() ?? "";

                // Confirmation avant envoi
                var result = MessageBox.Show(
                    $"Êtes-vous sûr de vouloir envoyer le PV par email à {selectedTeachers.Count} enseignant(s) ?\n\nExpéditeur : {smtpEmail}",
                    "Confirmation de Mailing",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes)
                    return;

                // Verrouiller les boutons pendant l'envoi
                btnSendMailing.IsEnabled = false;
                btnBrowsePV.IsEnabled = false;
                pbSendingProgress.Visibility = Visibility.Visible;
                pbSendingProgress.Maximum = selectedTeachers.Count;
                pbSendingProgress.Value = 0;
                txtSendingStatus.Visibility = Visibility.Visible;

                int successCount = 0;
                int failureCount = 0;
                string currentFile = selectedPVFile;
                string firstErrorMessage = "";

                await System.Threading.Tasks.Task.Run(() =>
                {
                    for (int i = 0; i < selectedTeachers.Count; i++)
                    {
                        var teacher = selectedTeachers[i];
                        int index = i + 1;

                        Dispatcher.Invoke(() =>
                        {
                            txtSendingStatus.Text = $"Envoi en cours ({index}/{selectedTeachers.Count}) à {teacher.FullName} ({teacher.Email})...";
                            pbSendingProgress.Value = index;
                        });

                        if (string.IsNullOrWhiteSpace(teacher.Email))
                        {
                            failureCount++;
                            continue;
                        }

                        try
                        {
                            SendEmailWithPVInternal(
                                teacher.Email, 
                                teacher.FullName, 
                                currentFile, 
                                subject, 
                                messageBody,
                                smtpServer,
                                smtpPort,
                                smtpEmail,
                                smtpPassword,
                                enableSsl);
                            successCount++;
                        }
                        catch (Exception ex)
                        {
                            failureCount++;
                            if (string.IsNullOrEmpty(firstErrorMessage))
                            {
                                firstErrorMessage = ex.Message;
                            }
                            Console.WriteLine($"Erreur envoi à {teacher.Email}: {ex.Message}");
                        }
                    }
                });

                txtSendingStatus.Text = $"Envoi terminé ! Réussis : {successCount}, Échoués : {failureCount}";

                if (failureCount > 0 && successCount == 0)
                {
                    // Échec total : Afficher une explication claire avec guide de résolution
                    string detailedHelp = $"❌ Échec de l'envoi SMTP ({failureCount} erreur(s))\n\nDétail de l'erreur :\n{firstErrorMessage}\n\n" +
                        "──────────────────────────────────────────────\n" +
                        "💡 COMMENT RÉSOUDRE CETTE ERREUR GMAIL / SMTP :\n\n" +
                        "1. Allez dans votre compte Google (https://myaccount.google.com)\n" +
                        "2. Activez 'Validation en 2 étapes' (dans Sécurité)\n" +
                        "3. Allez sur 'Mots de passe d'application'\n" +
                        "4. Générez un mot de passe (sélectionnez 'Autre' > nommez 'App Deliberation')\n" +
                        "5. Copiez le code à 16 caractères généré et collez-le dans le champ 'Mot de passe' SMTP ci-dessus.";

                    expanderSmtp.IsExpanded = true;
                    MessageBox.Show(detailedHelp, "Erreur d'Authentification SMTP", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else
                {
                    MessageBox.Show(
                        $"Mailing terminé !\n\n✓ Mails envoyés avec succès : {successCount}\n✗ Échecs : {failureCount}",
                        "Résultat du Mailing",
                        MessageBoxButton.OK,
                        (failureCount == 0) ? MessageBoxImage.Information : MessageBoxImage.Warning);

                    if (failureCount == 0)
                    {
                        this.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur: {ex.Message}", "Erreur", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                btnSendMailing.IsEnabled = true;
                btnBrowsePV.IsEnabled = true;
            }
        }

        /// <summary>
        /// Envoyer un email SMTP avec le PV en pièce jointe
        /// </summary>
        private void SendEmailWithPVInternal(
            string toEmail, 
            string teacherName, 
            string pvFilePath, 
            string subject, 
            string customMessage,
            string smtpServer,
            int smtpPort,
            string smtpEmail,
            string smtpPassword,
            bool enableSsl)
        {
            try
            {
                using (var client = new SmtpClient(smtpServer, smtpPort))
                {
                    client.EnableSsl = enableSsl;
                    client.Credentials = new NetworkCredential(smtpEmail, smtpPassword);
                    client.Timeout = 20000;

                    using (var mailMessage = new MailMessage(smtpEmail, toEmail))
                    {
                        mailMessage.Subject = subject;
                        mailMessage.Body = $"Bonjour {teacherName},\n\n" +
                                         customMessage +
                                         "\n\nCordialement,\nL'Administration";
                        mailMessage.IsBodyHtml = false;

                        // Ajouter la pièce jointe PV
                        if (File.Exists(pvFilePath))
                        {
                            mailMessage.Attachments.Add(new Attachment(pvFilePath));
                        }

                        // Envoyer l'email
                        client.Send(mailMessage);
                        Console.WriteLine($"[MAILING SUCCESS] Email envoyé à {toEmail}");
                    }
                }
            }
            catch (SmtpException smtpEx)
            {
                if (smtpEx.Message.Contains("5.7.0") || smtpEx.Message.Contains("Authentication Required") || smtpEx.Message.Contains("535"))
                {
                    throw new Exception("5.7.0 Authentication Required: Le serveur SMTP exige un mot de passe d'application valide.");
                }
                throw new Exception($"Erreur SMTP ({smtpEx.StatusCode}): {smtpEx.Message}");
            }
            catch (Exception ex)
            {
                throw new Exception($"Erreur lors de l'envoi à {toEmail}: {ex.Message}");
            }
        }

        /// <summary>
        /// <summary>
        /// Événement : Édition terminée d'une cellule (Mise à jour directe de l'email en BD)
        /// </summary>
        private void DgTeachers_CellEditEnding(object sender, System.Windows.Controls.DataGridCellEditEndingEventArgs e)
        {
            if (e.Row.Item is TeacherMailItem teacher && e.EditingElement is System.Windows.Controls.TextBox tb)
            {
                string newEmail = tb.Text?.Trim() ?? "";
                teacher.Email = newEmail;

                // Sauvegarder dans la base de données MySQL
                var authService = new AuthenticationService();
                authService.MettreAJourEmailUtilisateur(teacher.Id, newEmail);

                // Auto-cocher si un email valide est renseigné
                if (!string.IsNullOrWhiteSpace(newEmail) && newEmail.Contains("@"))
                {
                    teacher.IsSelected = true;
                }

                UpdateSummary();
            }
        }

        /// <summary>
        /// Annuler et fermer la fenêtre
        /// </summary>
        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }

    /// <summary>
    /// Modèle pour afficher les enseignants dans le DataGrid
    /// </summary>
    public class TeacherMailItem : System.ComponentModel.INotifyPropertyChanged
    {
        private int _id;
        private string _fullName;
        private string _email;
        private string _role;
        private bool _isSelected;

        public int Id
        {
            get => _id;
            set { _id = value; OnPropertyChanged(nameof(Id)); }
        }
        public string FullName
        {
            get => _fullName;
            set { _fullName = value; OnPropertyChanged(nameof(FullName)); }
        }
        public string Email
        {
            get => _email;
            set { _email = value; OnPropertyChanged(nameof(Email)); }
        }
        public string Role
        {
            get => _role;
            set { _role = value; OnPropertyChanged(nameof(Role)); }
        }
        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; OnPropertyChanged(nameof(IsSelected)); }
        }

        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propName) => PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propName));
    }
}
