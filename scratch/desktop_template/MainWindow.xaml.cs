using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.IO;
using System.Collections.Generic;
using Microsoft.Win32;
using DesktopApp.Services;
using DesktopApp.Models;
using System.Linq;
using System.ComponentModel;
using System.Windows.Controls.Primitives;

namespace DesktopApp
{
    public partial class MainWindow : Window
    {
        private DatabaseHelper dbHelper;
        private ExcelImportService excelService;
        private WordGenerationService wordService;
        private ArchiveService archiveService;
        private HistoriqueService historiqueService;
        private ExcelExportService excelExportService;
        private CsvExportService csvExportService;
        private ExcelExportFromDatabaseService excelExportFromDatabaseService;
        private StudentManagementService studentManagementService; // Service de gestion des étudiants
        private NommageAutomatiqueService nommageService;          // Service de nommage CDC
        private PerformanceMetricsService performanceService;      // Service de métriques CDC  
        private SecuriteService securiteService;                   // Service de sécurité CDC
        private ExempleExcelService exempleExcelService;           // Service d'exemples CDC
        private AiAssistantService aiAssistantService;             // Service d'Assistant IA / Chatbot
        private JuryMemoryService juryMemoryService;               // Service de mémorisation du jury
        private List<Etudiant> etudiatsActuels; // Stocker les étudiants actuellement affichés
        private List<Etudiant> tousLesEtudiants; // Stocker tous les étudiants chargés
        private DecisionRule currentRules;     // Configuration des règles de décision par l'Admin

        public MainWindow()
        {
            InitializeComponent();
            InitializeApplication();
        }

        private void InitializeApplication()
        {
            try
            {
                currentRules = new DecisionRule();
                
                // Initialiser les services
                dbHelper = new DatabaseHelper();
                excelService = new ExcelImportService();
                wordService = new WordGenerationService();
                archiveService = new ArchiveService();
                historiqueService = new HistoriqueService();
                excelExportService = new ExcelExportService();
                csvExportService = new CsvExportService();
                excelExportFromDatabaseService = new ExcelExportFromDatabaseService();
                studentManagementService = new StudentManagementService(); // Initialiser le service de gestion étudiants
                
                // Initialiser les nouveaux services CDC
                nommageService = new NommageAutomatiqueService();
                performanceService = new PerformanceMetricsService();
                securiteService = new SecuriteService();
                exempleExcelService = new ExempleExcelService();
                aiAssistantService = new AiAssistantService();
                juryMemoryService = new JuryMemoryService();
                etudiatsActuels = new List<Etudiant>();

                // Message de bienvenue initial dans le Chatbot
                InitAiChatWelcome();

                // Afficher le dossier de sortie des PV dans Paramètres
                try
                {
                    if (txtDossierPV != null)
                    {
                        txtDossierPV.Text = Path.Combine(
                            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                            "PV_Générés"
                        );
                    }
                }
                catch { }

                // Afficher les infos utilisateur
                UpdateUserInfo();

                // NOUVEAU: Charger automatiquement un fichier Excel s'il existe
                ChargerFichierExcelAutomatiquement();

                // Configurer l'interface en fonction du rôle
                ConfigureInterfaceByRole();

                // Tester la connexion
                TestDatabaseConnection();

                // Charger les statistiques
                LoadStatistics();
                
                // Charger l'historique
                LoadHistorique();

                // Générer le fichier CSV pour faciliter les imports
                GenerateCsvFromDatabase();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur d'initialisation : {ex.Message}", "Erreur", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Afficher les informations de l'utilisateur connecté
        /// </summary>
        private void UpdateUserInfo()
        {
            var user = AuthenticationService.CurrentUser;
            if (user != null)
            {
                txtUserInfo.Text = $"Connecté en tant que: {user.FullName} ({user.Role})";
            }
        }

        /// <summary>
        /// Configurer l'interface en fonction du rôle de l'utilisateur
        /// </summary>
        private void ConfigureInterfaceByRole()
        {
            var authService = new AuthenticationService();
            bool isAdmin = authService.IsAdmin();

            // Afficher/Masquer l'onglet Admin selon le rôle
            if (tabAdmin != null)
            {
                tabAdmin.Visibility = isAdmin ? Visibility.Visible : Visibility.Collapsed;
            }

            // Afficher/Masquer l'onglet Paramètres selon le rôle
            if (tabParametres != null)
            {
                tabParametres.Visibility = isAdmin ? Visibility.Visible : Visibility.Collapsed;
            }

            // Charger les utilisateurs pour les admins
            if (isAdmin && dgUsers != null)
            {
                try
                {
                    dgUsers.ItemsSource = authService.GetAllUsers();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erreur: {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        /// <summary>
        /// Déconnecter l'utilisateur
        /// </summary>
        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Êtes-vous sûr de vouloir vous déconnecter?", "Confirmation", 
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                // Déconnecter
                var authService = new AuthenticationService();
                authService.Logout();

                // Ouvrir la fenêtre de connexion
                LoginWindow loginWindow = new LoginWindow();
                loginWindow.Show();

                // Fermer cette fenêtre
                this.Close();
            }
        }

        /// <summary>
        /// Ajouter un nouvel utilisateur (Admin uniquement)
        /// </summary>
        private void BtnAddUser_Click(object sender, RoutedEventArgs e)
        {
            string username = txtNewUsername.Text;
            string fullName = txtNewFullName.Text;
            string email = txtNewEmail.Text;
            string role = cmbRole.SelectedItem?.ToString() ?? "Utilisateur";

            // Validation des champs
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(fullName) || string.IsNullOrEmpty(email))
            {
                MessageBox.Show("Veuillez remplir tous les champs.", "Erreur", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Validation du format email (basique)
            if (!email.Contains("@"))
            {
                MessageBox.Show("❌ L'adresse email doit contenir un @.", "Erreur de validation", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtNewEmail.Focus();
                txtNewEmail.SelectAll();
                return;
            }

            try
            {
                var authService = new AuthenticationService();
                bool success = false;
                string message = "";

                // Vérifier si c'est une modification ou un ajout
                if (btnAddUser.Tag != null && int.TryParse(btnAddUser.Tag.ToString(), out int userId))
                {
                    // Modification d'un utilisateur existant
                    var userToUpdate = new User
                    {
                        Id = userId,
                        Username = username,
                        FullName = fullName,
                        Email = email,
                        Password = "password123", // Mot de passe par défaut pour modification
                        Role = role == "Admin" ? UserRole.Admin : UserRole.Utilisateur,
                        IsActive = true
                    };

                    success = authService.UpdateUser(userToUpdate);
                    message = success ? "Utilisateur modifié avec succès." : "Erreur lors de la modification.";
                }
                else
                {
                    // Ajout d'un nouvel utilisateur
                    var newUser = new User
                    {
                        Username = username,
                        FullName = fullName,
                        Email = email,
                        Password = "password123",
                        Role = role == "Admin" ? UserRole.Admin : UserRole.Utilisateur,
                        IsActive = true
                    };

                    success = authService.AddUser(newUser);
                    message = success ? "Utilisateur ajouté avec succès." : "Cet nom d'utilisateur existe déjà.";
                }

                if (success)
                {
                    MessageBox.Show(message, "Succès", 
                        MessageBoxButton.OK, MessageBoxImage.Information);

                    // Actualiser la liste
                    dgUsers.ItemsSource = null;
                    dgUsers.ItemsSource = authService.GetAllUsers();

                    // Réinitialiser les champs
                    txtNewUsername.Clear();
                    txtNewFullName.Clear();
                    txtNewEmail.Clear();
                    cmbRole.SelectedIndex = 0;
                    btnAddUser.Content = "Ajouter";
                    btnAddUser.Tag = null;
                }
                else
                {
                    MessageBox.Show(message, "Erreur", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur: {ex.Message}", "Erreur", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TestDatabaseConnection()
        {
            try
            {
                DatabaseConnection db = new DatabaseConnection();
                if (db.OpenConnection())
                {
                    db.CloseConnection();
                }
            }
            catch (Exception)
            {
                // Erreur de connexion à la base de données
            }
        }

        private void LoadStatistics()
        {
            try
            {
                var dbHelper = new DatabaseHelper();

                // Requête pour le total des PV générés (tous les temps)
                string queryTotal = "SELECT COUNT(*) as total FROM decision_mention";
                var resultTotal = dbHelper.ExecuteSelectQuery(queryTotal);
                if (resultTotal.Rows.Count > 0)
                {
                    txtTotalPV.Text = resultTotal.Rows[0]["total"].ToString();
                }

                // Requête pour les PV générés ce mois
                string queryMois = "SELECT COUNT(*) as total FROM decision_mention WHERE MONTH(date_creation) = MONTH(NOW()) AND YEAR(date_creation) = YEAR(NOW())";
                var resultMois = dbHelper.ExecuteSelectQuery(queryMois);
                if (resultMois.Rows.Count > 0)
                {
                    txtMoisPV.Text = resultMois.Rows[0]["total"].ToString();
                }

                // Requête pour les PV générés aujourd'hui
                string queryJour = "SELECT COUNT(*) as total FROM decision_mention WHERE DATE(date_creation) = DATE(NOW())";
                var resultJour = dbHelper.ExecuteSelectQuery(queryJour);
                if (resultJour.Rows.Count > 0)
                {
                    txtJourPV.Text = resultJour.Rows[0]["total"].ToString();
                }
            }
            catch (Exception ex)
            {
                // En cas d'erreur, afficher 0
                txtTotalPV.Text = "0";
                txtMoisPV.Text = "0";
                txtJourPV.Text = "0";
                Console.WriteLine($"Erreur lors du chargement des statistiques : {ex.Message}");
            }
        }

        /// <summary>
        /// Tester le fonctionnement de l'historique
        /// </summary>
        private void TestHistorique()
        {
            try
            {
                Console.WriteLine("=== TEST HISTORIQUE ===");
                
                // Test du service
                if (historiqueService != null)
                {
                    var historique = historiqueService.GetAllHistorique();
                    Console.WriteLine($"✓ Historique récupéré: {historique.Count} entrées");
                    
                    // Afficher dans le DataGrid
                    dgHistorique.ItemsSource = historique;
                    
                    // Test d'ajout d'une entrée fictive pour démonstration
                    var testEntry = new Historique
                    {
                        DateDeliberation = DateTime.Now,
                        Classe = "DEMO-CLASS",
                        Session = DateTime.Now.Year.ToString(),
                        NomFichier = "PV_DEMO_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".docx",
                        CheminFichier = @"C:\temp\demo_pv.docx",
                        NbEtudiants = 30,
                        NbAdmis = 25,
                        NbAjournes = 5,
                        UtilisateurId = 1
                    };

                    bool added = historiqueService.AddHistorique(testEntry);
                    if (added)
                    {
                        Console.WriteLine("✓ Entrée de démonstration ajoutée");
                        LoadHistorique(); // Recharger l'affichage
                        MessageBox.Show("Test d'historique réussi! Une entrée de démonstration a été ajoutée.", 
                                      "Test Historique", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        Console.WriteLine("✗ Échec de l'ajout de l'entrée de test");
                        MessageBox.Show("Test d'historique: Impossible d'ajouter une entrée de test.", 
                                      "Test Historique", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                else
                {
                    MessageBox.Show("Service d'historique non initialisé.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erreur test historique: {ex.Message}");
                MessageBox.Show($"Erreur lors du test de l'historique:\n\n{ex.Message}", 
                              "Erreur Test", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Charger l'historique des PV générés
        /// </summary>
        private void LoadHistorique()
        {
            try
            {
                if (historiqueService != null)
                {
                    var historique = historiqueService.GetAllHistorique();
                    
                    if (historique != null && historique.Count > 0)
                    {
                        dgHistorique.ItemsSource = historique;
                    }
                    else
                    {
                        // Afficher un message si aucun historique
                        Console.WriteLine("Aucun historique trouvé");
                        dgHistorique.ItemsSource = new List<Historique>();
                    }
                }
            }
            catch (Exception ex)
            {
                // Juste logger l'erreur, ne pas bloquer l'application
                Console.WriteLine($"⚠ Avertissement: Erreur lors du chargement de l'historique: {ex.Message}");
                Console.WriteLine($"   Note: La table 'deliberations' peut ne pas exister encore.");
                Console.WriteLine($"   L'application continue normalement.");
                
                // Afficher un historique vide pour que l'onglet fonctionne
                try
                {
                    dgHistorique.ItemsSource = new List<Historique>();
                }
                catch { }
            }
        }

        /// <summary>
        /// Rafraîchir l'historique des PV
        /// </summary>
        private void RefreshHistorique()
        {
            try
            {
                LoadHistorique();
                MessageBox.Show("Historique rafraîchi avec succès.", "Succès");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du rafraîchissement: {ex.Message}", "Erreur");
            }
        }

        // Gestionnaires d'événements pour les boutons

        private void BtnImportExcel_Click(object sender, RoutedEventArgs e)
        {
            // Trouver le TabControl et basculer vers l'onglet Import
            var mainGrid = this.Content as Grid;
            if (mainGrid != null && mainGrid.Children.Count > 2)
            {
                var tabControl = mainGrid.Children[2] as TabControl;
                if (tabControl != null)
                {
                    tabControl.SelectedIndex = 1;
                }
            }
        }

        private void BtnGenererPV_Click(object sender, RoutedEventArgs e)
        {
            // Basculer vers l'onglet Génération
            var mainGrid = this.Content as Grid;
            if (mainGrid != null && mainGrid.Children.Count > 2)
            {
                var tabControl = mainGrid.Children[2] as TabControl;
                if (tabControl != null)
                {
                    tabControl.SelectedIndex = 2;
                }
            }
        }

        private void BtnHistorique_Click(object sender, RoutedEventArgs e)
        {
            // Basculer vers l'onglet Historique
            var mainGrid = this.Content as Grid;
            if (mainGrid != null && mainGrid.Children.Count > 2)
            {
                var tabControl = mainGrid.Children[2] as TabControl;
                if (tabControl != null)
                {
                    tabControl.SelectedIndex = 3;
                }
            }
        }

        private void BtnParcourir_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Title = "Sélectionner un fichier Excel ou CSV",
                Filter = "Fichiers supportés (*.xlsx;*.xls;*.csv)|*.xlsx;*.xls;*.csv|Fichiers Excel (*.xlsx;*.xls)|*.xlsx;*.xls|Fichiers CSV (*.csv)|*.csv|Tous les fichiers (*.*)|*.*",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
            };

            if (openFileDialog.ShowDialog() == true)
            {
                txtCheminFichier.Text = openFileDialog.FileName;
                btnChargerExcel.IsEnabled = true;
                txtStatutImport.Text = $"📁 Fichier sélectionné : {System.IO.Path.GetFileName(openFileDialog.FileName)}";
            }
        }

        private void BtnChargerExcel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string cheminFichier = txtCheminFichier.Text;

                if (string.IsNullOrEmpty(cheminFichier))
                {
                    MessageBox.Show("Veuillez sélectionner un fichier Excel.", "Erreur", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Afficher un message de chargement
                txtStatutImport.Text = "⏳ Chargement et analyse en cours...";
                this.Cursor = Cursors.Wait;

                // Utiliser notre nouveau service d'import
                var result = ImportExcelEtudiants(cheminFichier);

                this.Cursor = null;

                if (result.Success)
                {
                    // Activer le bouton de validation automatique
                    if (btnValidationAuto != null)
                    {
                        btnValidationAuto.IsEnabled = true;
                    }
                    
                    // Activer le bouton d'export
                    if (btnExporterExcel != null)
                    {
                        btnExporterExcel.IsEnabled = true;
                    }

                    MessageBox.Show($"Import réussi !\n\n{result.Message}\n\nVous pouvez maintenant utiliser la validation automatique.", 
                        "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show($"Erreur lors de l'import :\n\n{result.Message}", 
                        "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                this.Cursor = null;
                MessageBox.Show($"Erreur inattendue :\n\n{ex.Message}", 
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                UpdateImportStatus("❌ Erreur lors de l'import", false);
            }
        }

        /// <summary>
        /// Modifier un utilisateur
        /// </summary>
        private void BtnModifyUser_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var user = button?.DataContext as User;

            if (user == null)
                return;

            // Remplir les champs avec les données de l'utilisateur
            txtNewUsername.Text = user.Username;
            txtNewFullName.Text = user.FullName;
            cmbRole.SelectedItem = user.Role == UserRole.Admin ? "Admin" : "Utilisateur";

            // Changer le texte du bouton et sauvegarder l'ID utilisateur
            btnAddUser.Content = "✏️ Mettre à jour";
            btnAddUser.Tag = user.Id; // Stocker l'ID pour savoir quel utilisateur modifier
        }

        /// <summary>
        /// Supprimer un utilisateur
        /// </summary>
        private void BtnDeleteUser_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var user = button?.DataContext as User;

            if (user == null)
                return;

            var result = MessageBox.Show($"Êtes-vous sûr de vouloir supprimer l'utilisateur {user.Username}?", 
                "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    var authService = new AuthenticationService();
                    if (authService.DeleteUser(user.Id))
                    {
                        MessageBox.Show("Utilisateur supprimé avec succès.", "Succès", 
                            MessageBoxButton.OK, MessageBoxImage.Information);

                        // Actualiser la liste
                        dgUsers.ItemsSource = null;
                        dgUsers.ItemsSource = authService.GetAllUsers();
                    }
                    else
                    {
                        MessageBox.Show("Erreur lors de la suppression de l'utilisateur.", "Erreur", 
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erreur: {ex.Message}", "Erreur", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        /// <summary>
        /// Enregistrer les règles de décision configurées par l'Admin dans l'onglet Paramètres
        /// </summary>
        private void BtnSaveRules_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (currentRules == null)
                    currentRules = new DecisionRule();

                if (decimal.TryParse(txtSeuilAdmission?.Text, out decimal seuilAdmis))
                    currentRules.SeuilAdmission = seuilAdmis;

                if (decimal.TryParse(txtSeuilTB?.Text, out decimal seuilTB))
                    currentRules.SeuilTresBien = seuilTB;

                if (decimal.TryParse(txtSeuilB?.Text, out decimal seuilB))
                    currentRules.SeuilBien = seuilB;

                if (decimal.TryParse(txtSeuilAB?.Text, out decimal seuilAB))
                    currentRules.SeuilAssezBien = seuilAB;

                if (decimal.TryParse(txtSeuilP?.Text, out decimal seuilP))
                    currentRules.SeuilPassable = seuilP;

                if (decimal.TryParse(txtSeuilRattrapage?.Text, out decimal seuilRattrapage))
                    currentRules.SeuilRachatMG = seuilRattrapage;

                // Si des étudiants sont actuellement chargés, recalculer leurs décisions avec les nouvelles règles
                if (etudiatsActuels != null && etudiatsActuels.Count > 0)
                {
                    foreach (var et in etudiatsActuels)
                    {
                        et.CalculerDecisionEtMention(currentRules);
                    }

                    if (dgDonnees != null)
                    {
                        dgDonnees.ItemsSource = null;
                        dgDonnees.ItemsSource = etudiatsActuels;
                        dgDonnees.UpdateLayout();
                    }

                    var decisionCalcService = new DecisionCalculatorService();
                    string resume = decisionCalcService.ObtenirResume(etudiatsActuels);
                    if (txtStatutImport != null)
                    {
                        txtStatutImport.Text = $"✅ Règles mises à jour et appliquées aux étudiants.\n\n{resume}";
                    }
                }

                MessageBox.Show("✅ Les règles de décision ont été enregistrées et appliquées avec succès.",
                    "Succès - Configuration Admin", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la sauvegarde des règles : {ex.Message}", "Erreur",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Générer le PV de délibération en Word - Version corrigée selon CDC
        /// </summary>
        public void GenererPVWord()
        {
            try
            {
                // Démarrer les métriques de performance CDC
                performanceService.DemarrerMesure("generation_pv", $"{etudiatsActuels?.Count ?? 0} étudiants");

                if (etudiatsActuels == null || etudiatsActuels.Count == 0)
                {
                    MessageBox.Show("Aucune donnée à générer. Veuillez d'abord importer un fichier Excel.",
                        "Attention", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Vérification de sécurité CDC - Pas de transmission externe
                var checkSecurite = securiteService.VerifierTransmissionDonnees(etudiatsActuels, "génération_pv", "local");
                if (!checkSecurite.EstSecurise)
                {
                    MessageBox.Show($"Violation de sécurité détectée:\n{checkSecurite.MessageSecurite}", "Sécurité", MessageBoxButton.OK, MessageBoxImage.Warning);
                    performanceService.ArreterMesure("generation_pv", "SÉCURITÉ_VIOLATION", 0);
                    return;
                }

                // Lire les infos du formulaire jury
                var jury = new DesktopApp.Services.InfosJury
                {
                    NomEtablissement = txtEtablissement?.Text ?? "École Supérieure Privée d'Ingénierie et de Technologies",
                    TypeSession = (cmbTypeSession?.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content?.ToString() ?? "Principale",
                    DateDeliberation = dpDateDeliberation?.SelectedDate ?? DateTime.Now,
                    PresidentJury = txtPresidentJury?.Text?.Trim() ?? "",
                    Secretaire = txtSecretaire?.Text?.Trim() ?? "",
                    MembreJury1 = txtMembreJury1?.Text?.Trim() ?? "",
                    MembreJury2 = txtMembreJury2?.Text?.Trim() ?? "",
                    Filiere = etudiatsActuels[0].ClasseGroupe ?? "Sans classe",
                    AnneeUniversitaire = etudiatsActuels[0].AnneeUniversitaire ?? DateTime.Now.Year.ToString()
                };

                // Mémoriser automatiquement la composition du jury pour l'autocomplétion future
                if (juryMemoryService != null)
                {
                    if (!string.IsNullOrWhiteSpace(jury.PresidentJury)) juryMemoryService.AddOrUpdateMember(jury.PresidentJury, "President");
                    if (!string.IsNullOrWhiteSpace(jury.Secretaire)) juryMemoryService.AddOrUpdateMember(jury.Secretaire, "Secretaire");
                    if (!string.IsNullOrWhiteSpace(jury.MembreJury1)) juryMemoryService.AddOrUpdateMember(jury.MembreJury1, "Membre");
                    if (!string.IsNullOrWhiteSpace(jury.MembreJury2)) juryMemoryService.AddOrUpdateMember(jury.MembreJury2, "Membre");
                }

                string classeGroupe = etudiatsActuels[0].ClasseGroupe ?? "Sans classe";
                
                // Utiliser le service de nommage automatique CDC
                string nomFichier = nommageService.GenererNomPV(classeGroupe, jury.DateDeliberation);

                string dossierSortie = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "PV_Générés"
                );

                if (!Directory.Exists(dossierSortie))
                    Directory.CreateDirectory(dossierSortie);

                // Rendre le nom unique si nécessaire
                string cheminComplet = Path.Combine(dossierSortie, nomFichier);
                cheminComplet = nommageService.RendreNomUnique(cheminComplet);
                nomFichier = Path.GetFileName(cheminComplet);

                this.Cursor = Cursors.Wait;
                bool succes = wordService.GenererPV(etudiatsActuels, dossierSortie, nomFichier, classeGroupe, jury);
                this.Cursor = null;

                // Mesurer les performances et vérifier conformité CDC
                var metrique = performanceService.ArreterMesure("generation_pv", classeGroupe, etudiatsActuels.Count);

                if (succes)
                {
                    // Archiver le PV avec les nouvelles métriques
                    archiveService.ArchiverPV(cheminComplet, classeGroupe, etudiatsActuels, jury.TypeSession);

                    // Ajouter à l'historique avec métriques de performance
                    try
                    {
                        var historiqueEntry = new Historique
                        {
                            DateDeliberation = jury.DateDeliberation,
                            Classe = classeGroupe,
                            Session = jury.AnneeUniversitaire,
                            NomFichier = nomFichier,
                            CheminFichier = cheminComplet,
                            NbEtudiants = etudiatsActuels.Count,
                            NbAdmis = etudiatsActuels.Count(e => e.Decision != null && e.Decision.StartsWith("Admis")),
                            NbAjournes = etudiatsActuels.Count(e => e.Decision != null && !e.Decision.StartsWith("Admis")),
                            UtilisateurId = AuthenticationService.CurrentUser?.Id ?? 0
                        };
                        bool ok = historiqueService.AddHistorique(historiqueEntry);
                        if (ok) LoadHistorique();
                    }
                    catch (Exception histEx)
                    {
                        Console.WriteLine($"Historique non enregistré: {histEx.Message}");
                    }

                    // Mettre à jour le statut dans l'onglet
                    try 
                    { 
                        string statutMessage = $"✅ PV généré : {nomFichier}";
                        if (!metrique.ConformeCDC)
                        {
                            statutMessage += $" ⚠️ Performance: {metrique.MessageConformite}";
                        }
                        txtStatutGeneration.Text = statutMessage;
                    } 
                    catch { }

                    // Message de succès avec informations de performance
                    string messageSucces = $"✅ PV généré avec succès!\n\nFichier: {nomFichier}\nChemin: {dossierSortie}";
                    
                    if (!metrique.ConformeCDC)
                    {
                        messageSucces += $"\n\n⚠️ Performance: {metrique.MessageConformite}";
                    }
                    else
                    {
                        messageSucces += $"\n✅ Performance: {metrique.DureeSecondes:F2}s (conforme CDC)";
                    }

                    messageSucces += "\n\nVoulez-vous ouvrir le fichier?";

                    var resultMessage = MessageBox.Show(messageSucces, "Succès", MessageBoxButton.YesNo, MessageBoxImage.Information);

                    if (resultMessage == MessageBoxResult.Yes)
                        System.Diagnostics.Process.Start(cheminComplet);
                }
                else
                {
                    string messageErreur = $"Erreur lors de la génération du PV.\n\nPerformance: {metrique.MessageConformite}";
                    MessageBox.Show(messageErreur, "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                this.Cursor = null;
                performanceService.ArreterMesure("generation_pv", "ERREUR", 0);
                MessageBox.Show($"Erreur: {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Event handler pour le bouton Générer PV
        /// </summary>
        private void BtnGenererPVWord_Click(object sender, RoutedEventArgs e)
        {
            GenererPVWord();
        }

        /// <summary>
        /// Event handler pour le bouton Ouvrir Archive
        /// </summary>
        private void BtnOuvrirArchive_Click(object sender, RoutedEventArgs e)
        {
            OuvrirArchivePV();
        }

        /// <summary>
        /// Ouvrir un fichier PV depuis l'historique
        /// </summary>
        private void BtnOpenHistoriqueFile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var button = sender as Button;
                var historique = button?.DataContext as Historique;

                if (historique != null && !string.IsNullOrEmpty(historique.CheminFichier))
                {
                    if (System.IO.File.Exists(historique.CheminFichier))
                    {
                        System.Diagnostics.Process.Start(historique.CheminFichier);
                    }
                    else
                    {
                        MessageBox.Show($"Le fichier n'existe plus à l'emplacement:\n{historique.CheminFichier}", 
                                      "Fichier introuvable", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                else
                {
                    MessageBox.Show("Aucun chemin de fichier disponible.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'ouverture du fichier:\n{ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Ouvrir le dossier contenant le fichier PV
        /// </summary>
        private void BtnOpenHistoriqueFolder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var button = sender as Button;
                var historique = button?.DataContext as Historique;

                if (historique != null && !string.IsNullOrEmpty(historique.CheminFichier))
                {
                    string dossier = System.IO.Path.GetDirectoryName(historique.CheminFichier);
                    if (System.IO.Directory.Exists(dossier))
                    {
                        System.Diagnostics.Process.Start("explorer.exe", dossier);
                    }
                    else
                    {
                        MessageBox.Show($"Le dossier n'existe plus:\n{dossier}", 
                                      "Dossier introuvable", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                else
                {
                    MessageBox.Show("Aucun chemin de fichier disponible.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'ouverture du dossier:\n{ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Bouton de test de l'historique
        /// </summary>
        private void BtnTestHistorique_Click(object sender, RoutedEventArgs e)
        {
            TestHistorique();
        }

        /// <summary>
        /// Supprimer une entrée de l'historique
        /// </summary>
        private void BtnDeleteHistorique_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var button = sender as Button;
                var historique = button?.DataContext as Historique;

                if (historique == null)
                {
                    MessageBox.Show("Impossible de récupérer les informations de l'entrée d'historique.", 
                        "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Demander confirmation avant suppression
                var result = MessageBox.Show(
                    $"Êtes-vous sûr de vouloir supprimer cette entrée de l'historique ?\n\n" +
                    $"Date : {historique.DateDeliberation:dd/MM/yyyy HH:mm}\n" +
                    $"Classe : {historique.Classe}\n" +
                    $"Fichier : {historique.NomFichier}\n\n" +
                    $"⚠️ Attention : Cette action supprimera uniquement l'entrée de l'historique.\n" +
                    $"Le fichier PV sur le disque ne sera pas supprimé.",
                    "Confirmation de suppression", 
                    MessageBoxButton.YesNo, 
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    // Supprimer l'entrée de l'historique en base de données
                    bool success = historiqueService.DeleteHistorique(historique.Id);

                    if (success)
                    {
                        MessageBox.Show("L'entrée a été supprimée de l'historique avec succès.", 
                            "Suppression réussie", MessageBoxButton.OK, MessageBoxImage.Information);
                        
                        // Actualiser l'affichage de l'historique
                        LoadHistorique();
                        
                        // Actualiser les statistiques
                        LoadStatistics();
                    }
                    else
                    {
                        MessageBox.Show("Erreur lors de la suppression de l'entrée d'historique.\n" +
                            "Vérifiez que la base de données est accessible.", 
                            "Erreur de suppression", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur inattendue lors de la suppression :\n\n{ex.Message}", 
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Rafraîchir l'historique des PV
        /// </summary>
        private void RefreshHistorique(object sender, RoutedEventArgs e)
        {
            try
            {
                LoadHistorique();
                MessageBox.Show("Historique actualisé avec succès.", "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'actualisation de l'historique:\n{ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Exporter les données des étudiants en fichier Excel
        /// </summary>
        private void BtnExporterExcel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Console.WriteLine("\n════════════════════════════════════════════════════════════");
                Console.WriteLine("  DÉBUT EXPORT EXCEL");
                Console.WriteLine("════════════════════════════════════════════════════════════\n");

                if (etudiatsActuels == null || etudiatsActuels.Count == 0)
                {
                    MessageBox.Show("Aucune donnée à exporter. Veuillez d'abord charger un fichier Excel.", 
                        "Attention", MessageBoxButton.OK, MessageBoxImage.Warning);
                    Console.WriteLine("[APP] ❌ Pas de données à exporter");
                    return;
                }

                Console.WriteLine($"[APP] Données prêtes: {etudiatsActuels.Count} étudiants");

                // Déterminer le nom du fichier
                string classeGroupe = etudiatsActuels[0].ClasseGroupe ?? "Étudiants";
                string dateActuelle = DateTime.Now.ToString("yyyy-MM-dd_HHmmss");
                string nomFichier = $"Etudiants_{classeGroupe}_{dateActuelle}.xlsx";

                Console.WriteLine($"[APP] Nom du fichier: {nomFichier}");

                // Dossier de sortie (Documents)
                string dossierSortie = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "Exports_Étudiants"
                );

                Console.WriteLine($"[APP] Dossier de sortie: {dossierSortie}");

                // Exporter les données
                this.Cursor = Cursors.Wait;
                Console.WriteLine("[APP] Appel du service d'export...");
                
                bool succes = excelExportService.ExporterEtudiants(etudiatsActuels, dossierSortie, nomFichier);
                
                this.Cursor = null;

                Console.WriteLine($"[APP] Résultat export: {(succes ? "✓ SUCCÈS" : "❌ ÉCHEC")}");

                if (succes)
                {
                    string cheminComplet = Path.Combine(dossierSortie, nomFichier);
                    Console.WriteLine($"[APP] Fichier créé à: {cheminComplet}");

                    // Vérifier que le fichier existe vraiment
                    if (File.Exists(cheminComplet))
                    {
                        FileInfo fileInfo = new FileInfo(cheminComplet);
                        Console.WriteLine($"[APP] ✓ Fichier vérifié ({fileInfo.Length} bytes)");

                        var result = MessageBox.Show(
                            $"✅ Export réussi!\n\n" +
                            $"Fichier: {nomFichier}\n" +
                            $"Chemin: {dossierSortie}\n" +
                            $"Taille: {fileInfo.Length / 1024} KB\n\n" +
                            $"Voulez-vous ouvrir le fichier?",
                            "Succès",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Information
                        );

                        if (result == MessageBoxResult.Yes)
                        {
                            Console.WriteLine("[APP] Ouverture du fichier...");
                            System.Diagnostics.Process.Start(cheminComplet);
                            Console.WriteLine("[APP] ✓ Fichier ouvert");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"[APP] ❌ Le fichier n'existe pas à: {cheminComplet}");
                        MessageBox.Show($"Erreur: Le fichier n'a pas pu être créé.\n\nChemin attendu: {cheminComplet}", 
                            "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    Console.WriteLine("[APP] ❌ L'export a échoué");
                    MessageBox.Show(
                        "Erreur lors de l'export du fichier Excel.\n\n" +
                        "Vérifiez la console Visual Studio (Output) pour les détails.", 
                        "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                }

                Console.WriteLine("\n════════════════════════════════════════════════════════════");
                Console.WriteLine("  FIN EXPORT EXCEL");
                Console.WriteLine("════════════════════════════════════════════════════════════\n");
            }
            catch (Exception ex)
            {
                this.Cursor = null;
                Console.WriteLine($"[APP] ❌ Exception: {ex.Message}");
                Console.WriteLine($"[APP] StackTrace: {ex.StackTrace}");
                MessageBox.Show($"Erreur: {ex.Message}", "Erreur", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Ouvrir le dossier d'archivage des PV
        /// </summary>
        public void OuvrirArchivePV()
        {
            try
            {
                string cheminArchive = archiveService.ObtenirCheminArchivage();
                System.Diagnostics.Process.Start(cheminArchive);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur: {ex.Message}", "Erreur", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Générer un fichier CSV avec les données des étudiants depuis la base de données
        /// Ce fichier sera disponible pour import
        /// </summary>
        private void GenerateCsvFromDatabase()
        {
            try
            {
                Console.WriteLine("\n════════════════════════════════════════════════════════════");
                Console.WriteLine("  GÉNÉRATION CSV DEPUIS BASE DE DONNÉES");
                Console.WriteLine("════════════════════════════════════════════════════════════\n");

                // Chemin de sortie dans Documents
                string dossierSortie = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
                );

                Console.WriteLine($"[CSV-INIT] Dossier de sortie: {dossierSortie}");

                // Exporter depuis la base de données
                bool succes = csvExportService.ExporterDepuisDatabase(dossierSortie, "etudiant.csv");

                if (succes)
                {
                    string cheminComplet = Path.Combine(dossierSortie, "etudiant.csv");
                    if (File.Exists(cheminComplet))
                    {
                        FileInfo fileInfo = new FileInfo(cheminComplet);
                        Console.WriteLine($"[CSV-INIT] ✓ Fichier CSV créé: {cheminComplet}");
                        Console.WriteLine($"[CSV-INIT] Taille: {fileInfo.Length} bytes");
                    }
                }
                else
                {
                    Console.WriteLine("[CSV-INIT] ✗ Échec de la génération du CSV");
                }

                Console.WriteLine("\n════════════════════════════════════════════════════════════");
                Console.WriteLine("  FIN GÉNÉRATION CSV");
                Console.WriteLine("════════════════════════════════════════════════════════════\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CSV-INIT] ❌ Exception: {ex.Message}");
                Console.WriteLine($"[CSV-INIT] StackTrace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Event handler pour régénérer le fichier CSV depuis la base de données (si un bouton existe)
        /// </summary>
        private void BtnExporterCsv_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Console.WriteLine("\n[CSV] Régénération du fichier CSV à la demande de l'utilisateur");

                string dossierSortie = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
                );

                bool succes = csvExportService.ExporterDepuisDatabase(dossierSortie, "etudiant.csv");

                if (succes)
                {
                    string cheminComplet = Path.Combine(dossierSortie, "etudiant.csv");
                    MessageBox.Show(
                        $"✅ Fichier CSV généré avec succès!\n\n" +
                        $"Chemin: {cheminComplet}\n\n" +
                        $"Le fichier est prêt pour import.",
                        "Succès",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );
                }
                else
                {
                    MessageBox.Show(
                        "❌ Erreur lors de la génération du fichier CSV.\n\n" +
                        "Vérifiez la console pour plus de détails.",
                        "Erreur",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error
                    );
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur: {ex.Message}", "Erreur", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Charger automatiquement un fichier Excel au démarrage de l'application
        /// Cherche dans le dossier courant des fichiers Excel courants et CSV
        /// </summary>
        private void ChargerFichierExcelAutomatiquement()
        {
            try
            {
                // Liste des noms de fichiers prioritaires à chercher (CSV en premier pour éviter les verrous Excel)
                string[] fichiersAChercher = {
                    "etudiants.csv",           // Fichier CSV créé par notre script
                    "donnees_etudiants.csv",   // Autres fichiers CSV possibles
                    "etudiants.xlsx",
                    "donnees_etudiants.xlsx", 
                    "liste_etudiants.xlsx",
                    "import_etudiants.xlsx"
                };

                string fichierTrouve = null;

                // Chercher dans l'ordre de priorité
                foreach (string nomFichier in fichiersAChercher)
                {
                    if (File.Exists(nomFichier))
                    {
                        // Vérifier que le fichier n'est pas verrouillé
                        if (EstFichierAccessible(nomFichier))
                        {
                            fichierTrouve = Path.GetFullPath(nomFichier);
                            Console.WriteLine($"[AUTO] Fichier accessible trouvé: {nomFichier}");
                            break;
                        }
                        else
                        {
                            Console.WriteLine($"[AUTO] Fichier trouvé mais verrouillé: {nomFichier} (ignoré)");
                        }
                    }
                }

                // Si pas trouvé dans la liste prioritaire, chercher d'autres fichiers CSV/Excel non verrouillés
                if (fichierTrouve == null)
                {
                    // Chercher les CSV d'abord (moins de risque de verrouillage)
                    string[] fichiersCsv = Directory.GetFiles(".", "*.csv");
                    foreach (string fichierCsv in fichiersCsv)
                    {
                        if (EstFichierAccessible(fichierCsv))
                        {
                            fichierTrouve = Path.GetFullPath(fichierCsv);
                            Console.WriteLine($"[AUTO] Fichier CSV accessible trouvé: {Path.GetFileName(fichierCsv)}");
                            break;
                        }
                    }

                    // Si pas de CSV, chercher les Excel non verrouillés
                    if (fichierTrouve == null)
                    {
                        string[] fichiersExcel = Directory.GetFiles(".", "*.xlsx");
                        foreach (string fichierExcel in fichiersExcel)
                        {
                            string nomFichier = Path.GetFileName(fichierExcel);
                            if (!nomFichier.StartsWith("~$") && !nomFichier.Contains("(") && EstFichierAccessible(fichierExcel))
                            {
                                fichierTrouve = Path.GetFullPath(fichierExcel);
                                Console.WriteLine($"[AUTO] Fichier Excel accessible trouvé: {nomFichier}");
                                break;
                            }
                        }
                    }
                }

                // Si un fichier accessible est trouvé, l'importer automatiquement
                if (fichierTrouve != null)
                {
                    Console.WriteLine($"[AUTO] Import automatique en cours...");
                    try { txtCheminFichier.Text = fichierTrouve; } catch { }
                    ImporterFichierExcelAutomatique(fichierTrouve);
                }
                else
                {
                    Console.WriteLine("[AUTO] Aucun fichier trouvé - en attente import manuel.");
                    // Ne pas créer de fausses données de test
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AUTO] Erreur lors du chargement automatique: {ex.Message}");
                // En cas d'erreur, créer des données de test
                CreerDonneesTestAffichage();
            }
        }

        /// <summary>
        /// Vérifier si un fichier est accessible en lecture (pas verrouillé)
        /// </summary>
        private bool EstFichierAccessible(string cheminFichier)
        {
            try
            {
                using (FileStream stream = File.Open(cheminFichier, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    return true;
                }
            }
            catch (IOException)
            {
                // Fichier verrouillé ou inaccessible
                return false;
            }
            catch
            {
                // Autre erreur
                return false;
            }
        }

        /// <summary>
        /// Importer un fichier Excel automatiquement (version silencieuse)
        /// </summary>
        private void ImporterFichierExcelAutomatique(string cheminFichier)
        {
            try
            {
                Console.WriteLine($"[AUTO] ═══ IMPORT AUTOMATIQUE DÉMARRÉ ═══");
                Console.WriteLine($"[AUTO] Fichier: {cheminFichier}");

                // Vérifier une dernière fois que le fichier est accessible
                if (!EstFichierAccessible(cheminFichier))
                {
                    Console.WriteLine($"[AUTO] ⚠️ Fichier devenu inaccessible: {cheminFichier}");
                    Console.WriteLine($"[AUTO] Basculement vers les données de test...");
                    CreerDonneesTestAffichage();
                    return;
                }

                // Importer les données
                var result = excelService.ImporterDonneesExcel(cheminFichier);

                if (result.Succes)
                {
                    // Stocker les étudiants actuels
                    etudiatsActuels = result.Etudiants;
                    tousLesEtudiants = result.Etudiants;
                    Console.WriteLine($"[AUTO] ✅ {etudiatsActuels.Count} étudiants importés");

                    // Calculer les décisions automatiquement
                    var decisionCalcService = new DecisionCalculatorService();
                    etudiatsActuels = decisionCalcService.TraiterEtudiants(etudiatsActuels);
                    Console.WriteLine($"[AUTO] ✅ Décisions calculées pour {etudiatsActuels.Count} étudiants");

                    // Afficher les données dans le DataGrid (même si erreurs XAML)
                    try
                    {
                        dgDonnees.ItemsSource = null;
                        dgDonnees.ItemsSource = etudiatsActuels;
                        dgDonnees.Visibility = Visibility.Visible;
                        dgDonnees.UpdateLayout();
                        Console.WriteLine($"[AUTO] ✅ DataGrid mis à jour avec {etudiatsActuels.Count} étudiants");
                    }
                    catch (Exception xamlEx)
                    {
                        Console.WriteLine($"[AUTO] ⚠️ Erreur XAML ignorée: {xamlEx.Message}");
                    }

                    // Activer le bouton d'export (si possible)
                    try
                    {
                        btnExporterExcel.IsEnabled = true;
                    }
                    catch { /* Ignorer erreurs XAML */ }

                    // Afficher le statut (si possible)
                    try
                    {
                        string resumeDecisions = decisionCalcService.ObtenirResume(etudiatsActuels);
                        txtStatutImport.Text = $"✅ Auto-import: {result.MessageSucces}\n\n{resumeDecisions}";
                    }
                    catch { /* Ignorer erreurs XAML */ }

                    // Afficher le résumé dans la console
                    Console.WriteLine($"[AUTO] ═══ RÉSUMÉ DES DONNÉES IMPORTÉES ═══");
                    Console.WriteLine($"[AUTO] Fichier: {Path.GetFileName(cheminFichier)}");
                    Console.WriteLine($"[AUTO] Étudiants: {etudiatsActuels.Count}");
                    
                    var admis = etudiatsActuels.Count(e => e.Decision.Contains("Admis"));
                    var ajournes = etudiatsActuels.Count(e => e.Decision.Contains("Ajourné"));
                    var rattrapage = etudiatsActuels.Count(e => e.Decision.Contains("rattrapage"));
                    
                    Console.WriteLine($"[AUTO] - Admis: {admis}");
                    Console.WriteLine($"[AUTO] - Ajournés: {ajournes}");
                    Console.WriteLine($"[AUTO] - Session de rattrapage: {rattrapage}");
                    Console.WriteLine($"[AUTO] ═══════════════════════════════════");
                    
                    // Lister les premiers étudiants
                    Console.WriteLine($"[AUTO] Aperçu des données:");
                    for (int i = 0; i < Math.Min(5, etudiatsActuels.Count); i++)
                    {
                        var et = etudiatsActuels[i];
                        Console.WriteLine($"[AUTO]   {et.NumeroOrdre}. {et.NomPrenom} - {et.MoyenneGenerale:F2} - {et.Decision}");
                    }
                    if (etudiatsActuels.Count > 5)
                        Console.WriteLine($"[AUTO]   ... et {etudiatsActuels.Count - 5} autres");
                    
                    Console.WriteLine($"[AUTO] ═══ IMPORT AUTOMATIQUE TERMINÉ ✅ ═══");
                }
                else
                {
                    Console.WriteLine($"[AUTO] ❌ Erreur d'import: {result.MessageErreur}");
                    
                    // Vérifier si c'est une erreur de fichier verrouillé
                    if (result.MessageErreur.Contains("accéder au fichier") || 
                        result.MessageErreur.Contains("en cours d'utilisation") ||
                        result.MessageErreur.Contains("locked"))
                    {
                        Console.WriteLine($"[AUTO] ⚠️ Fichier probablement ouvert dans Excel");
                        Console.WriteLine($"[AUTO] Conseil: Fermez le fichier dans Excel et redémarrez l'application");
                    }
                    
                    // En cas d'erreur, créer des données de test
                    CreerDonneesTestAffichage();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AUTO] ❌ Exception durant l'import automatique: {ex.Message}");
                
                // Vérifier si c'est une erreur de fichier verrouillé
                if (ex.Message.Contains("being used by another process") || 
                    ex.Message.Contains("accéder au fichier") ||
                    ex.Message.Contains("en cours d'utilisation"))
                {
                    Console.WriteLine($"[AUTO] ⚠️ Le fichier est ouvert dans une autre application (Excel?)");
                    Console.WriteLine($"[AUTO] Conseil: Fermez le fichier et redémarrez l'application");
                }
                
                // En cas d'erreur, créer des données de test
                CreerDonneesTestAffichage();
            }
        }

        /// <summary>
        /// Créer des données de test pour vérifier l'affichage du tableau
        /// </summary>
        private void CreerDonneesTestAffichage()
        {
            try
            {
                Console.WriteLine("[TEST] Création de données de test pour affichage du tableau");
                
                etudiatsActuels = new List<Etudiant>
                {
                    new Etudiant
                    {
                        NumeroOrdre = 1,
                        Nom = "Balti",
                        Prenom = "Amina",
                        NomPrenom = "Balti Amina",
                        Matricule = "20231045",
                        ClasseGroupe = "3A40",
                        Filiere = "Génie Informatique",
                        MoyenneGenerale = 16.500m,
                        Decision = "Admis",
                        Mention = "Très Bien",
                        Validation = "Oui",
                        Observation = "Passage d'année"
                    },
                    new Etudiant
                    {
                        NumeroOrdre = 2,
                        Nom = "Ben Ali",
                        Prenom = "Karim",
                        NomPrenom = "Ben Ali Karim",
                        Matricule = "20232010",
                        ClasseGroupe = "4TWIN1",
                        Filiere = "Web & Internet",
                        MoyenneGenerale = 17.200m,
                        Decision = "Admis",
                        Mention = "Très Bien",
                        Validation = "Oui",
                        Observation = "Passage d'année"
                    },
                    new Etudiant
                    {
                        NumeroOrdre = 3,
                        Nom = "Dridi",
                        Prenom = "Sonia",
                        NomPrenom = "Dridi Sonia",
                        Matricule = "20233001",
                        ClasseGroupe = "2GL2",
                        Filiere = "Génie Logiciel",
                        MoyenneGenerale = 14.800m,
                        Decision = "Admis",
                        Mention = "Bien",
                        Validation = "Oui",
                        Observation = "Passage d'année"
                    },
                    new Etudiant
                    {
                        NumeroOrdre = 4,
                        Nom = "Bouazizi",
                        Prenom = "Skander",
                        NomPrenom = "Bouazizi Skander",
                        Matricule = "20234020",
                        ClasseGroupe = "5SIM3",
                        Filiere = "Systèmes Mobiles",
                        MoyenneGenerale = 18.000m,
                        Decision = "Admis",
                        Mention = "Très Bien",
                        Validation = "Oui",
                        Observation = "Major de promotion"
                    },
                    new Etudiant
                    {
                        NumeroOrdre = 5,
                        Nom = "Zaibi",
                        Prenom = "Rayen",
                        NomPrenom = "Zaibi Rayen",
                        Matricule = "20235005",
                        ClasseGroupe = "1NFIN1",
                        Filiere = "Informatique",
                        MoyenneGenerale = 8.500m,
                        Decision = "Ajourné",
                        Mention = "Session de rattrapage",
                        Validation = "Non",
                        Observation = "Admis en session de rattrapage"
                    }
                };
                
                // Assigner au DataGrid et forcer l'affichage
                tousLesEtudiants = etudiatsActuels;
                dgDonnees.ItemsSource = null;
                dgDonnees.ItemsSource = etudiatsActuels;
                dgDonnees.Visibility = Visibility.Visible;
                dgDonnees.UpdateLayout();
                
                // Activer le bouton d'export
                if (btnExporterExcel != null)
                    btnExporterExcel.IsEnabled = true;
                
                Console.WriteLine($"[TEST] ✅ {etudiatsActuels.Count} étudiants de test créés et assignés");
                Console.WriteLine($"[TEST] DataGrid.Items.Count = {dgDonnees.Items.Count}");
                
                // Forcer l'actualisation de toute l'interface
                this.UpdateLayout();
                MettreAJourCompteurEtudiants();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TEST] ❌ Erreur création données test: {ex.Message}");
            }
        }

        /// <summary>
        /// Filtrage en temps réel sur le tableau des étudiants
        /// </summary>
        private void TxtRechercheEtudiants_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (dgDonnees?.ItemsSource == null) return;

                var view = System.Windows.Data.CollectionViewSource.GetDefaultView(dgDonnees.ItemsSource);
                if (view == null) return;

                string filterText = txtRechercheEtudiants?.Text?.Trim()?.ToLower() ?? "";

                if (string.IsNullOrWhiteSpace(filterText))
                {
                    view.Filter = null;
                }
                else
                {
                    view.Filter = item =>
                    {
                        if (item is Etudiant et)
                        {
                            return et.NumeroOrdre.ToString().Contains(filterText) ||
                                   (!string.IsNullOrEmpty(et.NomPrenom) && et.NomPrenom.ToLower().Contains(filterText)) ||
                                   (!string.IsNullOrEmpty(et.Matricule) && et.Matricule.ToLower().Contains(filterText)) ||
                                   (!string.IsNullOrEmpty(et.ClasseGroupe) && et.ClasseGroupe.ToLower().Contains(filterText)) ||
                                   et.MoyenneGenerale.ToString("F3").Contains(filterText) ||
                                   et.MoyenneGenerale.ToString("F2").Contains(filterText) ||
                                   (!string.IsNullOrEmpty(et.Decision) && et.Decision.ToLower().Contains(filterText)) ||
                                   (!string.IsNullOrEmpty(et.Mention) && et.Mention.ToLower().Contains(filterText)) ||
                                   (!string.IsNullOrEmpty(et.Validation) && et.Validation.ToLower().Contains(filterText)) ||
                                   (!string.IsNullOrEmpty(et.Observation) && et.Observation.ToLower().Contains(filterText));
                        }
                        return false;
                    };
                }

                MettreAJourCompteurEtudiants();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FILTER] Erreur filtrage étudiants: {ex.Message}");
            }
        }

        /// <summary>
        /// Mettre à jour le compteur d'étudiants affichés
        /// </summary>
        private void MettreAJourCompteurEtudiants()
        {
            try
            {
                if (txtCompteurEtudiants == null) return;
                if (dgDonnees?.ItemsSource == null)
                {
                    txtCompteurEtudiants.Text = "";
                    return;
                }

                var view = System.Windows.Data.CollectionViewSource.GetDefaultView(dgDonnees.ItemsSource);
                int visibleCount = 0;
                if (view != null)
                {
                    foreach (var _ in view) visibleCount++;
                }
                int totalCount = etudiatsActuels?.Count ?? 0;

                if (visibleCount == totalCount)
                {
                    txtCompteurEtudiants.Text = $"Total: {totalCount} étudiant(s)";
                }
                else
                {
                    txtCompteurEtudiants.Text = $"Affichés: {visibleCount} / {totalCount} étudiant(s)";
                }
            }
            catch { }
        }

        /// <summary>
        /// Filtrage en temps réel sur le tableau d'historique
        /// </summary>
        private void TxtRechercheHistorique_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (dgHistorique?.ItemsSource == null) return;

                var view = System.Windows.Data.CollectionViewSource.GetDefaultView(dgHistorique.ItemsSource);
                if (view == null) return;

                string filterText = txtRechercheHistorique?.Text?.Trim()?.ToLower() ?? "";

                if (string.IsNullOrWhiteSpace(filterText))
                {
                    view.Filter = null;
                }
                else
                {
                    view.Filter = item =>
                    {
                        if (item is Historique h)
                        {
                            return h.DateDeliberation.ToString("dd/MM/yyyy HH:mm").ToLower().Contains(filterText) ||
                                   (!string.IsNullOrEmpty(h.Classe) && h.Classe.ToLower().Contains(filterText)) ||
                                   (!string.IsNullOrEmpty(h.Session) && h.Session.ToLower().Contains(filterText)) ||
                                   h.NbEtudiants.ToString().Contains(filterText) ||
                                   h.NbAdmis.ToString().Contains(filterText) ||
                                   h.NbAjournes.ToString().Contains(filterText) ||
                                   (!string.IsNullOrEmpty(h.NomFichier) && h.NomFichier.ToLower().Contains(filterText)) ||
                                   (!string.IsNullOrEmpty(h.CheminFichier) && h.CheminFichier.ToLower().Contains(filterText));
                        }
                        return false;
                    };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FILTER] Erreur filtrage historique: {ex.Message}");
            }
        }

        /// <summary>
        /// Gère le défilement fluide du tableau DataGrid à la molette de la souris
        /// </summary>
        private void DataGrid_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            try
            {
                if (sender is DataGrid dg)
                {
                    var scrollViewer = FindVisualChild<ScrollViewer>(dg);
                    if (scrollViewer != null)
                    {
                        if (e.Delta < 0)
                            scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset + 40);
                        else
                            scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - 40);

                        e.Handled = true;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SCROLL] Erreur molette DataGrid: {ex.Message}");
            }
        }

        private static T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null) return null;
            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
                if (child is T typedChild)
                    return typedChild;

                var childOfChild = FindVisualChild<T>(child);
                if (childOfChild != null)
                    return childOfChild;
            }
            return null;
        }

        #region Gestion des Étudiants - Fonctionnalités Desktop

        /// <summary>
        /// Import Excel - upload d'un fichier Excel, retourne les données parsées
        /// </summary>
        public ImportResult ImportExcelEtudiants(string filePath)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath) || !System.IO.File.Exists(filePath))
                {
                    return new ImportResult
                    {
                        Success = false,
                        Message = "Fichier non trouvé ou chemin invalide"
                    };
                }

                // Utiliser le service de gestion des étudiants
                var result = studentManagementService.ImportEtudiantsFromExcel(filePath);

                if (result.Success && result.Etudiants != null)
                {
                    // Mettre à jour la liste locale
                    tousLesEtudiants = result.Etudiants;
                    etudiatsActuels = result.Etudiants;
                    
                    // Mettre à jour l'interface
                    RefreshStudentDataGrid();
                    
                    // Mettre à jour le statut
                    UpdateImportStatus($"✅ {result.Message}", true);
                }
                else
                {
                    UpdateImportStatus($"❌ {result.Message}", false);
                }

                return result;
            }
            catch (Exception ex)
            {
                var errorResult = new ImportResult
                {
                    Success = false,
                    Message = $"Erreur lors de l'import: {ex.Message}"
                };
                
                UpdateImportStatus($"❌ {errorResult.Message}", false);
                return errorResult;
            }
        }

        /// <summary>
        /// Validation des décisions - applique les règles de décision et retourne un aperçu
        /// </summary>
        public ImportResult ValidateDecisionsEtudiants()
        {
            try
            {
                if (etudiatsActuels == null || !etudiatsActuels.Any())
                {
                    return new ImportResult
                    {
                        Success = false,
                        Message = "Aucun étudiant chargé. Veuillez d'abord importer un fichier Excel."
                    };
                }

                // Appliquer les règles de décision
                var result = studentManagementService.ApplyDecisionRules(etudiatsActuels);

                if (result.Success)
                {
                    // Mettre à jour l'interface
                    RefreshStudentDataGrid();
                    
                    // Afficher les statistiques
                    ShowStatistics(result.Statistics);
                    
                    // Mettre à jour le statut
                    UpdateImportStatus($"✅ {result.Message}", true);
                }
                else
                {
                    UpdateImportStatus($"❌ {result.Message}", false);
                }

                return result;
            }
            catch (Exception ex)
            {
                var errorResult = new ImportResult
                {
                    Success = false,
                    Message = $"Erreur lors de la validation: {ex.Message}"
                };
                
                UpdateImportStatus($"❌ {errorResult.Message}", false);
                return errorResult;
            }
        }

        /// <summary>
        /// Récupérer les étudiants d'une classe
        /// </summary>
        /// <param name="classe">Code de classe (ex: 3A40)</param>
        /// <param name="session">ID de session (optionnel)</param>
        /// <returns>Liste des étudiants</returns>
        public List<Etudiant> GetEtudiantsClasse(string classe = null, int? session = null)
        {
            try
            {
                // S'assurer que tousLesEtudiants contient la liste actuelle si tousLesEtudiants était null
                if ((tousLesEtudiants == null || !tousLesEtudiants.Any()) && etudiatsActuels != null && etudiatsActuels.Any())
                {
                    tousLesEtudiants = etudiatsActuels;
                }

                if (string.IsNullOrWhiteSpace(classe) || classe.Contains("Toutes"))
                {
                    if (tousLesEtudiants != null && tousLesEtudiants.Any())
                    {
                        etudiatsActuels = tousLesEtudiants;
                    }
                    else
                    {
                        var dbEtudiants = studentManagementService.GetEtudiants(null, session);
                        if (dbEtudiants != null && dbEtudiants.Any())
                        {
                            etudiatsActuels = dbEtudiants;
                            tousLesEtudiants = dbEtudiants;
                        }
                    }
                }
                else
                {
                    string classeFiltre = classe.Trim();

                    // 1. Filtrer d'abord les étudiants déjà en mémoire (ex: via import Excel)
                    if (tousLesEtudiants != null && tousLesEtudiants.Any())
                    {
                        string cleanFiltre = classeFiltre.Replace(" ", "").Replace("-", "").ToLower();

                        var etudiantsFiltres = tousLesEtudiants.Where(e => 
                        {
                            if (string.IsNullOrWhiteSpace(e.ClasseGroupe)) return false;
                            string cleanClasse = e.ClasseGroupe.Replace(" ", "").Replace("-", "").ToLower();
                            return cleanClasse.Contains(cleanFiltre) || cleanFiltre.Contains(cleanClasse);
                        }).ToList();

                        if (etudiantsFiltres.Any())
                        {
                            etudiatsActuels = etudiantsFiltres;
                        }
                        else
                        {
                            // Tenter de charger depuis la DB
                            var dbEtudiants = studentManagementService.GetEtudiants(classeFiltre, session);
                            if (dbEtudiants != null && dbEtudiants.Any())
                            {
                                etudiatsActuels = dbEtudiants;
                            }
                            else
                            {
                                var classesPresentes = string.Join(", ", tousLesEtudiants
                                    .Select(e => e.ClasseGroupe)
                                    .Where(c => !string.IsNullOrEmpty(c))
                                    .Distinct());

                                var askImportMem = MessageBox.Show(
                                    $"Aucun étudiant trouvé pour la classe '{classeFiltre}'.\n" +
                                    (string.IsNullOrEmpty(classesPresentes) ? "" : $"Classes disponibles actuellement: {classesPresentes}\n\n") +
                                    $"Souhaitez-vous importer un fichier Excel pour la classe '{classeFiltre}' ?", 
                                    "Classe non trouvée", MessageBoxButton.YesNo, MessageBoxImage.Question);

                                if (askImportMem == MessageBoxResult.Yes)
                                {
                                    ImporterFichierPourClasse(classeFiltre, session);
                                }
                            }
                        }
                    }
                    else
                    {
                        // 2. Sinon, interroger la base de données
                        var dbEtudiants = studentManagementService.GetEtudiants(classeFiltre, session);
                        if (dbEtudiants != null && dbEtudiants.Any())
                        {
                            etudiatsActuels = dbEtudiants;
                            tousLesEtudiants = dbEtudiants;
                        }
                        else
                        {
                            var askImportDb = MessageBox.Show(
                                $"Aucun étudiant trouvé pour la classe '{classeFiltre}' dans la base de données.\n\n" +
                                $"Souhaitez-vous parcourir et importer un fichier Excel / CSV pour la classe '{classeFiltre}' ?", 
                                "Classe non trouvée", MessageBoxButton.YesNo, MessageBoxImage.Question);

                            if (askImportDb == MessageBoxResult.Yes)
                            {
                                ImporterFichierPourClasse(classeFiltre, session);
                            }
                        }
                    }
                }

                RefreshStudentDataGrid();

                var message = string.IsNullOrEmpty(classe) 
                    ? $"✅ {etudiatsActuels?.Count ?? 0} étudiants chargés (toutes les classes)"
                    : $"✅ {etudiatsActuels?.Count ?? 0} étudiant(s) affiché(s) pour la classe {classe}";

                UpdateImportStatus(message, true);

                return etudiatsActuels ?? new List<Etudiant>();
            }
            catch (Exception ex)
            {
                UpdateImportStatus($"❌ Erreur lors du chargement: {ex.Message}", false);
                return new List<Etudiant>();
            }
        }

        /// <summary>
        /// Ouvre le dialogue de fichier pour importer directement un fichier Excel/CSV pour la classe spécifiée
        /// </summary>
        private void ImporterFichierPourClasse(string classe, int? session = null)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Fichiers Excel et CSV (*.xlsx;*.xls;*.csv)|*.xlsx;*.xls;*.csv|Tous les fichiers (*.*)|*.*",
                Title = $"Sélectionner le fichier d'étudiants (Classe {classe})"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                var result = ImportExcelEtudiants(openFileDialog.FileName);
                if (result.Success && etudiatsActuels != null && etudiatsActuels.Any())
                {
                    // Si les étudiants importés n'ont pas de classe définie, leur assigner la classe demandée
                    foreach (var et in etudiatsActuels)
                    {
                        if (string.IsNullOrWhiteSpace(et.ClasseGroupe))
                        {
                            et.ClasseGroupe = classe;
                        }
                    }
                    GetEtudiantsClasse(classe, session);
                }
            }
        }

        /// <summary>
        /// Corriger manuellement une décision/mention d'un étudiant
        /// </summary>
        /// <param name="etudiantId">ID de l'étudiant</param>
        /// <param name="nouvelleDecision">Nouvelle décision</param>
        /// <param name="nouvelleMention">Nouvelle mention</param>
        /// <param name="nouvelleObservation">Nouvelle observation</param>
        /// <returns>True si succès</returns>
        public bool CorrigerDecisionEtudiant(int etudiantId, string nouvelleDecision = null, 
            string nouvelleMention = null, string nouvelleObservation = null)
        {
            try
            {
                var success = studentManagementService.UpdateEtudiant(etudiantId, 
                    nouvelleDecision, nouvelleMention, nouvelleObservation);

                if (success)
                {
                    // Mettre à jour l'étudiant dans la liste locale
                    var etudiant = etudiatsActuels.FirstOrDefault(e => e.Id == etudiantId);
                    if (etudiant != null)
                    {
                        if (!string.IsNullOrEmpty(nouvelleDecision))
                            etudiant.Decision = nouvelleDecision;
                        if (!string.IsNullOrEmpty(nouvelleMention))
                            etudiant.Mention = nouvelleMention;
                        if (!string.IsNullOrEmpty(nouvelleObservation))
                            etudiant.Observation = nouvelleObservation;
                    }
                    
                    // Rafraîchir l'interface
                    RefreshStudentDataGrid();
                    
                    UpdateImportStatus($"✅ Étudiant ID {etudiantId} mis à jour avec succès", true);
                }
                else
                {
                    UpdateImportStatus($"❌ Impossible de mettre à jour l'étudiant ID {etudiantId}", false);
                }

                return success;
            }
            catch (Exception ex)
            {
                UpdateImportStatus($"❌ Erreur lors de la correction: {ex.Message}", false);
                return false;
            }
        }

        /// <summary>
        /// Ouvre une fenêtre de dialogue pour corriger un étudiant
        /// </summary>
        /// <param name="etudiant">Étudiant à corriger</param>
        public void OuvrirDialogueCorrection(Etudiant etudiant)
        {
            if (etudiant == null) return;

            var dialogue = new CorrectionEtudiantWindow(etudiant);
            if (dialogue.ShowDialog() == true)
            {
                // Appliquer les corrections
                CorrigerDecisionEtudiant(etudiant.Id, 
                    dialogue.NouvelleDecision, 
                    dialogue.NouvelleMention, 
                    dialogue.NouvelleObservation);
            }
        }

        /// <summary>
        /// Met à jour l'affichage du DataGrid des étudiants
        /// </summary>
        private void RefreshStudentDataGrid()
        {
            try
            {
                if (dgDonnees != null && etudiatsActuels != null)
                {
                    dgDonnees.ItemsSource = null;
                    dgDonnees.ItemsSource = etudiatsActuels;
                    
                    // Mettre à jour le compteur
                    if (txtCompteurEtudiants != null)
                    {
                        txtCompteurEtudiants.Text = $"{etudiatsActuels.Count} étudiant(s)";
                    }

                    // Mettre à jour le statut des décisions
                    UpdateDecisionStatus();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors du rafraîchissement du DataGrid: {ex.Message}");
            }
        }

        /// <summary>
        /// Met à jour le statut d'import dans l'interface
        /// </summary>
        /// <param name="message">Message à afficher</param>
        /// <param name="success">Succès ou erreur</param>
        private void UpdateImportStatus(string message, bool success)
        {
            try
            {
                if (txtStatutImport != null)
                {
                    txtStatutImport.Text = message;
                    
                    // Changer la couleur selon le statut
                    txtStatutImport.Foreground = success 
                        ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(46, 125, 50)) // Vert
                        : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(211, 47, 47)); // Rouge
                }

                // Mettre à jour les informations supplémentaires
                UpdateDecisionStatus();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de la mise à jour du statut: {ex.Message}");
            }
        }

        /// <summary>
        /// Met à jour le statut des décisions
        /// </summary>
        private void UpdateDecisionStatus()
        {
            try
            {
                if (etudiatsActuels == null || !etudiatsActuels.Any())
                {
                    // Masquer l'indicateur de statut des décisions
                    if (borderStatutDecisions != null)
                        borderStatutDecisions.Visibility = Visibility.Collapsed;
                    if (txtInfoSupplementaire != null)
                        txtInfoSupplementaire.Text = "";
                    return;
                }

                // Compter les décisions
                var totalEtudiants = etudiatsActuels.Count;
                var decisionsCalculees = etudiatsActuels.Count(e => !string.IsNullOrEmpty(e.Decision));
                var decisionsEnAttente = totalEtudiants - decisionsCalculees;

                // Mettre à jour l'affichage
                if (txtInfoSupplementaire != null)
                {
                    txtInfoSupplementaire.Text = $"{totalEtudiants} étudiants chargés";
                }

                if (borderStatutDecisions != null && txtStatutDecisions != null)
                {
                    if (decisionsEnAttente > 0)
                    {
                        borderStatutDecisions.Visibility = Visibility.Visible;
                        borderStatutDecisions.Background = new System.Windows.Media.SolidColorBrush(
                            System.Windows.Media.Color.FromRgb(255, 243, 224)); // Orange clair
                        borderStatutDecisions.BorderBrush = new System.Windows.Media.SolidColorBrush(
                            System.Windows.Media.Color.FromRgb(255, 183, 77)); // Orange
                        
                        txtStatutDecisions.Text = $"⏳ {decisionsEnAttente} décisions en attente";
                        txtStatutDecisions.Foreground = new System.Windows.Media.SolidColorBrush(
                            System.Windows.Media.Color.FromRgb(230, 81, 0)); // Orange foncé
                    }
                    else if (decisionsCalculees > 0)
                    {
                        borderStatutDecisions.Visibility = Visibility.Visible;
                        borderStatutDecisions.Background = new System.Windows.Media.SolidColorBrush(
                            System.Windows.Media.Color.FromRgb(232, 245, 233)); // Vert clair
                        borderStatutDecisions.BorderBrush = new System.Windows.Media.SolidColorBrush(
                            System.Windows.Media.Color.FromRgb(129, 199, 132)); // Vert
                        
                        txtStatutDecisions.Text = $"✅ {decisionsCalculees} décisions calculées";
                        txtStatutDecisions.Foreground = new System.Windows.Media.SolidColorBrush(
                            System.Windows.Media.Color.FromRgb(56, 142, 60)); // Vert foncé
                    }
                    else
                    {
                        borderStatutDecisions.Visibility = Visibility.Collapsed;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de la mise à jour du statut des décisions: {ex.Message}");
            }
        }

        /// <summary>
        /// Affiche les statistiques de délibération
        /// </summary>
        /// <param name="stats">Statistiques à afficher</param>
        private void ShowStatistics(DeliberationStatistics stats)
        {
            if (stats == null) return;

            var message = $"📊 Statistiques: {stats.NbEtudiants} étudiants • " +
                         $"Admis: {stats.NbAdmis} ({stats.PourcentageAdmis:F1}%) • " +
                         $"Ajournés: {stats.NbAjournes} ({stats.PourcentageAjournes:F1}%) • " +
                         $"Moyenne: {stats.MoyenneGenerale:F2}";

            MessageBox.Show(message, "Statistiques de Délibération", 
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// Gestionnaire pour le double-clic sur un étudiant (pour correction)
        /// </summary>
        private void DgDonnees_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (dgDonnees.SelectedItem is Etudiant etudiant)
                {
                    OuvrirDialogueCorrection(etudiant);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'ouverture du dialogue: {ex.Message}", 
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Gestionnaire pour le bouton de chargement des étudiants par classe
        /// </summary>
        private void BtnChargerClasse_Click(object sender, RoutedEventArgs e)
        {
            List<string> classesDisponibles = null;
            if (tousLesEtudiants != null && tousLesEtudiants.Any())
            {
                classesDisponibles = tousLesEtudiants
                    .Where(et => !string.IsNullOrWhiteSpace(et.ClasseGroupe))
                    .Select(et => et.ClasseGroupe.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }

            var dialogue = new ChargerClasseWindow(classesDisponibles);
            if (dialogue.ShowDialog() == true)
            {
                try
                {
                    // Charger les étudiants
                    GetEtudiantsClasse(dialogue.ClasseSelectionnee, dialogue.SessionSelectionnee);
                    
                    // Options post-chargement
                    if (dialogue.RecalculerDecisions && etudiatsActuels != null && etudiatsActuels.Any())
                    {
                        var result = ValidateDecisionsEtudiants();
                        
                        if (dialogue.AfficherStatistiques && result.Success && result.Statistics != null)
                        {
                            ShowStatistics(result.Statistics);
                        }
                    }
                    
                    // Activer les boutons appropriés
                    if (etudiatsActuels != null && etudiatsActuels.Any())
                    {
                        if (btnValidationAuto != null)
                            btnValidationAuto.IsEnabled = true;
                        if (btnExporterExcel != null)
                            btnExporterExcel.IsEnabled = true;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erreur lors du chargement de la classe: {ex.Message}", 
                        "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        /// <summary>
        /// Gestionnaire pour le bouton de validation automatique des décisions
        /// </summary>
        private void BtnValidationAutomatique_Click(object sender, RoutedEventArgs e)
        {
            if (etudiatsActuels == null || !etudiatsActuels.Any())
            {
                MessageBox.Show("Aucun étudiant chargé. Veuillez d'abord importer des données.", 
                    "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show(
                $"Appliquer les règles de décision automatiques sur {etudiatsActuels.Count} étudiants ?", 
                "Validation Automatique", 
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                ValidateDecisionsEtudiants();
            }
        }

        /// <summary>
        /// Recharger les données depuis la base ou le fichier
        /// </summary>
        private void BtnRechargerDonnees_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (etudiatsActuels != null && etudiatsActuels.Any())
                {
                    RefreshStudentDataGrid();
                    UpdateImportStatus($"✅ Données actualisées ({etudiatsActuels.Count} étudiants)", true);
                }
                else
                {
                    UpdateImportStatus("ℹ️ Aucune donnée à actualiser", true);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'actualisation: {ex.Message}", "Erreur", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Vider le tableau et réinitialiser
        /// </summary>
        private void BtnViderTableau_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (etudiatsActuels == null || !etudiatsActuels.Any())
                {
                    MessageBox.Show("Le tableau est déjà vide.", "Information", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var result = MessageBox.Show(
                    $"Êtes-vous sûr de vouloir vider le tableau ?\n\nCela effacera {etudiatsActuels.Count} étudiants de l'affichage.", 
                    "Confirmer la suppression", 
                    MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    etudiatsActuels.Clear();
                    RefreshStudentDataGrid();
                    
                    // Désactiver les boutons
                    if (btnValidationAuto != null)
                        btnValidationAuto.IsEnabled = false;
                    if (btnExporterExcel != null)
                        btnExporterExcel.IsEnabled = false;
                    
                    UpdateImportStatus("🔄 Tableau vidé - Prêt pour un nouvel import", true);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du vidage: {ex.Message}", "Erreur", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Afficher l'aide d'utilisation
        /// </summary>
        private void BtnAideUtilisation_Click(object sender, RoutedEventArgs e)
        {
            var aide = @"🎯 Guide Rapide d'Utilisation

📥 IMPORT EXCEL:
1. Cliquez 'Parcourir' pour sélectionner votre fichier .xlsx
2. Cliquez 'Charger et Analyser' pour importer
3. Vos données apparaissent dans le tableau

✅ VALIDATION AUTOMATIQUE:
1. Après import, cliquez 'Validation Auto'
2. Les décisions sont calculées selon les règles officielles
3. Vérifiez les résultats dans le tableau

📚 CHARGER CLASSE:
1. Cliquez 'Charger Classe'
2. Saisissez le code (ex: 3A40)
3. Les étudiants de la classe s'affichent

✏️ CORRECTIONS MANUELLES:
1. Double-cliquez sur un étudiant dans le tableau
2. Modifiez décision, mention ou observation
3. Validez pour sauvegarder

💾 EXPORT:
1. Cliquez 'Exporter Excel'
2. Sauvegardez vos résultats

🔧 OUTILS:
• 🔄 Actualiser: Rafraîchit l'affichage
• 🗑️ Vider: Remet à zéro le tableau  
• ❓ Aide: Ce message

💡 ASTUCE: Double-cliquez sur un étudiant pour le corriger rapidement !";

            MessageBox.Show(aide, "Guide d'Utilisation", 
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        #endregion

        /// <summary>
        /// Générer un fichier Excel d'exemple conforme au CDC
        /// </summary>
        private void BtnCreerExempleExcel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var saveFileDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Title = "Enregistrer le fichier Excel d'exemple CDC",
                    Filter = "Fichier Excel (*.xlsx)|*.xlsx",
                    DefaultExt = "xlsx",
                    FileName = "Exemple_Deliberation_CDC.xlsx",
                    InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    this.Cursor = Cursors.Wait;

                    bool succes = exempleExcelService.CreerFichierExemple(saveFileDialog.FileName, true);

                    this.Cursor = null;

                    if (succes)
                    {
                        var result = MessageBox.Show(
                            $"✅ Fichier Excel d'exemple créé avec succès!\n\n" +
                            $"📁 Fichier: {Path.GetFileName(saveFileDialog.FileName)}\n" +
                            $"📂 Emplacement: {Path.GetDirectoryName(saveFileDialog.FileName)}\n\n" +
                            $"Le fichier contient:\n" +
                            $"• Structure conforme au CDC Annexe A\n" +
                            $"• 10 étudiants d'exemple avec données réalistes\n" +
                            $"• Feuille d'instructions complète\n" +
                            $"• Formatage et validation intégrés\n\n" +
                            $"Voulez-vous ouvrir le fichier?",
                            "Fichier Excel d'exemple créé",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Information);

                        if (result == MessageBoxResult.Yes)
                        {
                            try
                            {
                                System.Diagnostics.Process.Start(saveFileDialog.FileName);
                            }
                            catch (Exception openEx)
                            {
                                MessageBox.Show($"Impossible d'ouvrir le fichier automatiquement: {openEx.Message}\n\nVous pouvez l'ouvrir manuellement depuis: {saveFileDialog.FileName}", 
                                    "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                            }
                        }
                    }
                    else
                    {
                        MessageBox.Show("❌ Erreur lors de la création du fichier d'exemple.\n\nVérifiez que vous avez les permissions d'écriture dans ce répertoire.", 
                            "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                this.Cursor = null;
                MessageBox.Show($"❌ Erreur: {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Afficher les métriques de performance
        /// </summary>
        private void BtnVoirMetriques_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var rapport = performanceService.GenererRapport();
                var metriques = performanceService.ObtenirMetriques();

                string message = $"📊 RAPPORT DE PERFORMANCE\n\n";
                message += $"📈 Résumé: {rapport.MessageResume}\n";
                message += $"🔢 Opérations: {rapport.NombreOperations}\n";
                message += $"✅ Conformité CDC: {rapport.PourcentageConformite:F1}%\n";
                message += $"⏱️ Durée moyenne: {rapport.DureeMoyenne:F2}s\n\n";

                if (metriques.Any())
                {
                    message += "📋 DERNIÈRES OPÉRATIONS:\n";
                    foreach (var metrique in metriques.Skip(Math.Max(0, metriques.Count - 5)))
                    {
                        message += $"• {metrique.NomOperation} ({metrique.Details}): {metrique.DureeSecondes:F2}s ";
                        message += metrique.ConformeCDC ? "✅" : "❌";
                        message += $"\n";
                    }
                }
                else
                {
                    message += "ℹ️ Aucune métrique enregistrée pour le moment.";
                }

                if (rapport.OperationsLentes.Any())
                {
                    message += $"\n⚠️ OPÉRATIONS NON CONFORMES CDC:\n";
                    foreach (var lente in rapport.OperationsLentes.Take(3))
                    {
                        message += $"• {lente.NomOperation}: {lente.MessageConformite}\n";
                    }
                }

                MessageBox.Show(message, "Métriques de Performance", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'affichage des métriques: {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Afficher l'audit de sécurité
        /// </summary>
        private void BtnAuditSecurite_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var audits = securiteService.ObtenirHistoriqueAudit();
                var domaines = securiteService.ObtenirDomainesAutorises();

                string message = $"🔒 AUDIT DE SÉCURITÉ\n\n";
                message += $"🎯 Conformité CDC: Aucune donnée sensible transmise à des services externes non autorisés\n\n";

                message += $"📋 DOMAINES AUTORISÉS ({domaines.Count}):\n";
                foreach (var domaine in domaines.Take(5))
                {
                    message += $"• {domaine}\n";
                }
                if (domaines.Count > 5)
                    message += $"... et {domaines.Count - 5} autre(s)\n";

                if (audits.Any())
                {
                    message += $"\n📊 DERNIÈRES VÉRIFICATIONS ({audits.Count}):\n";
                    foreach (var audit in audits.Skip(Math.Max(0, audits.Count - 5)))
                    {
                        message += $"• {audit}\n";
                    }
                }
                else
                {
                    message += "\nℹ️ Aucun audit enregistré pour le moment.";
                }

                MessageBox.Show(message, "Audit de Sécurité", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'affichage de l'audit: {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #region --- ASSISTANT IA & CHATBOT ---

        private void InitAiChatWelcome()
        {
            if (panelAiMessages == null) return;
            panelAiMessages.Children.Clear();

            // Vérifier l'état du chatbot Python
            string statusMessage = "🤖 Assistant IA de Délibération";
            string detailMessage = "Bonjour ! Je suis votre Assistant IA de Délibération.\n\n";

            // Note: La vérification Python se fait maintenant dans AiAssistantService
            detailMessage += "✅ **Mode Intelligent Activé** : Support Python + Claude + Fallback local.\n\n";

            detailMessage += "Posez-moi des questions en langage naturel comme :\n" +
                           "• *\"Combien d'étudiants ont eu une mention Bien ce semestre ?\"*\n" +
                           "• *\"Génère-moi le PV de la classe 3A40\"*\n" +
                           "• *\"Quel est le taux de réussite de la promotion ?\"*\n" +
                           "• *\"Exporte les admis en Excel\"*";

            AddAiBubbleToChat(statusMessage, detailMessage,
                new List<string> { "Effectif connecté: " + (etudiatsActuels?.Count ?? 0) },
                new List<string> {
                    "Combien d'étudiants ont eu une mention Bien ce semestre ?",
                    "Génère-moi le PV de la classe 3A40",
                    "Quel est le taux de réussite ?",
                    "Exporte les admis en Excel"
                });
        }

        private void BtnToggleAiDrawer_Click(object sender, RoutedEventArgs e)
        {
            if (gridAiDrawerOverlay == null) return;
            if (gridAiDrawerOverlay.Visibility == Visibility.Visible)
            {
                gridAiDrawerOverlay.Visibility = Visibility.Collapsed;
            }
            else
            {
                gridAiDrawerOverlay.Visibility = Visibility.Visible;
                txtAiInput?.Focus();
            }
        }

        private void BtnCloseAiDrawer_Click(object sender, RoutedEventArgs e)
        {
            if (gridAiDrawerOverlay != null)
                gridAiDrawerOverlay.Visibility = Visibility.Collapsed;
        }

        private void GridAiDrawerOverlay_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource == sender || (e.OriginalSource is Border b && b.Background is System.Windows.Media.SolidColorBrush scb && scb.Color.A == 128))
            {
                if (gridAiDrawerOverlay != null)
                    gridAiDrawerOverlay.Visibility = Visibility.Collapsed;
            }
        }

        private void TxtQuickAiPrompt_GotFocus(object sender, RoutedEventArgs e)
        {
            if (txtQuickAiPrompt != null && txtQuickAiPrompt.Text.StartsWith("Demandez à l'IA"))
            {
                txtQuickAiPrompt.Text = "";
                txtQuickAiPrompt.Foreground = System.Windows.Media.Brushes.White;
            }
        }

        private void TxtQuickAiPrompt_LostFocus(object sender, RoutedEventArgs e)
        {
            if (txtQuickAiPrompt != null && string.IsNullOrWhiteSpace(txtQuickAiPrompt.Text))
            {
                txtQuickAiPrompt.Text = "Demandez à l'IA... (ex: Mentions Bien ?, PV 3A40)";
                txtQuickAiPrompt.Foreground = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFromString("#A0AEC0");
            }
        }

        private void TxtQuickAiPrompt_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                SubmitAiPrompt(txtQuickAiPrompt.Text);
            }
        }

        private void BtnQuickAiSend_Click(object sender, RoutedEventArgs e)
        {
            SubmitAiPrompt(txtQuickAiPrompt?.Text);
        }

        private void TxtAiInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && !Keyboard.IsKeyDown(Key.LeftShift))
            {
                e.Handled = true;
                SubmitAiPrompt(txtAiInput.Text);
                if (txtAiInput != null) txtAiInput.Text = "";
            }
        }

        private void BtnSendAiMessage_Click(object sender, RoutedEventArgs e)
        {
            SubmitAiPrompt(txtAiInput?.Text);
            if (txtAiInput != null) txtAiInput.Text = "";
        }

        private void BtnQuickQuestion_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag != null)
            {
                SubmitAiPrompt(btn.Tag.ToString());
            }
        }

        private void SubmitAiPrompt(string promptText)
        {
            if (string.IsNullOrWhiteSpace(promptText) || promptText.StartsWith("Demandez à l'IA")) return;

            string cleanedPrompt = promptText.Trim();

            if (gridAiDrawerOverlay != null)
            {
                gridAiDrawerOverlay.Visibility = Visibility.Visible;
            }

            AddUserBubbleToChat(cleanedPrompt);

            string currentFilter = txtRechercheEtudiants?.Text?.Trim();

            if (aiAssistantService == null) aiAssistantService = new AiAssistantService();
            var aiResult = aiAssistantService.ProcessPrompt(cleanedPrompt, etudiatsActuels, currentFilter);

            AddAiBubbleToChat(aiResult.Title, aiResult.ResponseText, aiResult.StatHighlights, aiResult.SuggestedFollowUps);

            if (aiResult.Action != null && aiResult.Action.Type != AiActionType.None)
            {
                ExecuteAiAction(aiResult.Action);
            }
        }

        private void AddUserBubbleToChat(string message)
        {
            if (panelAiMessages == null) return;

            var border = new Border
            {
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(139, 58, 58)),
                CornerRadius = new CornerRadius(12, 12, 0, 12),
                Padding = new Thickness(12, 9, 12, 9),
                Margin = new Thickness(40, 6, 0, 6),
                HorizontalAlignment = HorizontalAlignment.Right
            };

            var txt = new TextBlock
            {
                Text = message,
                Foreground = System.Windows.Media.Brushes.White,
                FontSize = 12.5,
                TextWrapping = TextWrapping.Wrap
            };

            border.Child = txt;
            panelAiMessages.Children.Add(border);
            scrollAiChat?.ScrollToBottom();
        }

        private void AddAiBubbleToChat(string title, string text, List<string> highlights = null, List<string> suggestions = null)
        {
            if (panelAiMessages == null) return;

            var mainBorder = new Border
            {
                Background = System.Windows.Media.Brushes.White,
                BorderBrush = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFromString("#CBD5E1"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12, 12, 12, 0),
                Padding = new Thickness(12, 10, 12, 10),
                Margin = new Thickness(0, 6, 40, 6),
                HorizontalAlignment = HorizontalAlignment.Left
            };

            var stack = new StackPanel();

            if (!string.IsNullOrWhiteSpace(title))
            {
                var txtTitle = new TextBlock
                {
                    Text = title,
                    FontWeight = FontWeights.Bold,
                    FontSize = 13,
                    Foreground = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFromString("#2C3E50"),
                    Margin = new Thickness(0, 0, 0, 6)
                };
                stack.Children.Add(txtTitle);
            }

            var txtBody = new TextBlock
            {
                Text = text,
                FontSize = 12.5,
                Foreground = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFromString("#333333"),
                TextWrapping = TextWrapping.Wrap,
                LineHeight = 18
            };
            stack.Children.Add(txtBody);

            if (highlights != null && highlights.Any())
            {
                var wrapPanel = new WrapPanel { Margin = new Thickness(0, 8, 0, 4) };
                foreach (var h in highlights)
                {
                    var badge = new Border
                    {
                        Background = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFromString("#EDF2F7"),
                        BorderBrush = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFromString("#CBD5E1"),
                        BorderThickness = new Thickness(1),
                        CornerRadius = new CornerRadius(10),
                        Padding = new Thickness(8, 3, 8, 3),
                        Margin = new Thickness(0, 0, 6, 4)
                    };
                    badge.Child = new TextBlock
                    {
                        Text = h,
                        FontSize = 11,
                        FontWeight = FontWeights.SemiBold,
                        Foreground = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFromString("#2B5B84")
                    };
                    wrapPanel.Children.Add(badge);
                }
                stack.Children.Add(wrapPanel);
            }

            if (suggestions != null && suggestions.Any())
            {
                var suggPanel = new StackPanel { Margin = new Thickness(0, 8, 0, 0) };
                suggPanel.Children.Add(new TextBlock
                {
                    Text = "Suggestions :",
                    FontSize = 10.5,
                    Foreground = System.Windows.Media.Brushes.Gray,
                    Margin = new Thickness(0, 0, 0, 4)
                });

                foreach (var s in suggestions)
                {
                    var btnSugg = new Button
                    {
                        Content = "💬 " + s,
                        Tag = s,
                        Style = (Style)FindResource("SecondaryButton"),
                        Padding = new Thickness(8, 3, 8, 3),
                        Margin = new Thickness(0, 0, 0, 4),
                        FontSize = 11,
                        HorizontalAlignment = HorizontalAlignment.Left
                    };
                    btnSugg.Click += BtnQuickQuestion_Click;
                    suggPanel.Children.Add(btnSugg);
                }
                stack.Children.Add(suggPanel);
            }

            mainBorder.Child = stack;
            panelAiMessages.Children.Add(mainBorder);
            scrollAiChat?.ScrollToBottom();
        }

        private void ExecuteAiAction(AiAction action)
        {
            if (action == null) return;

            try
            {
                switch (action.Type)
                {
                    case AiActionType.SwitchTab:
                        if (action.TargetIndex >= 0 && tabMain != null && action.TargetIndex < tabMain.Items.Count)
                        {
                            tabMain.SelectedIndex = action.TargetIndex;
                        }
                        break;

                    case AiActionType.GeneratePVWord:
                        if (tabMain != null) tabMain.SelectedIndex = 2; // Onglet Génération PV
                        if (!string.IsNullOrWhiteSpace(action.TargetParameter))
                        {
                            if (txtRechercheEtudiants != null) txtRechercheEtudiants.Text = action.TargetParameter;
                        }
                        break;

                    case AiActionType.ExportExcel:
                        BtnExporterExcel_Click(this, new RoutedEventArgs());
                        break;

                    case AiActionType.FilterClass:
                        if (!string.IsNullOrWhiteSpace(action.TargetParameter) && txtRechercheEtudiants != null)
                        {
                            txtRechercheEtudiants.Text = action.TargetParameter;
                            TxtRechercheEtudiants_TextChanged(this, null);
                        }
                        break;

                    case AiActionType.ClearFilter:
                        if (txtRechercheEtudiants != null)
                        {
                            txtRechercheEtudiants.Text = "";
                            TxtRechercheEtudiants_TextChanged(this, null);
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AI ACTION] Erreur exécution action: {ex.Message}");
            }
        }

        #endregion

        #region --- AIDE À LA SAISIE : AUTOCOMPLÉTION JURY & SUGGESTIONS DATES ---

        private void BtnDateSuggestion_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag != null && dpDateDeliberation != null)
            {
                string tag = btn.Tag.ToString();
                if (tag == "Today")
                {
                    dpDateDeliberation.SelectedDate = DateTime.Now;
                }
                else if (tag == "Janvier")
                {
                    dpDateDeliberation.SelectedDate = new DateTime(DateTime.Now.Year, 1, 31);
                }
                else if (tag == "Juin")
                {
                    dpDateDeliberation.SelectedDate = new DateTime(DateTime.Now.Year, 6, 30);
                }
            }
        }

        private void TxtJury_KeyUp(object sender, KeyEventArgs e)
        {
            if (sender is TextBox txt && txt.Tag != null)
            {
                string role = txt.Tag.ToString();
                ShowJurySuggestions(txt, role);
            }
        }

        private void TxtJury_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox txt && txt.Tag != null)
            {
                string role = txt.Tag.ToString();
                ShowJurySuggestions(txt, role);
            }
        }

        private void TxtJury_LostFocus(object sender, RoutedEventArgs e)
        {
            // Popup fermée automatiquement via StaysOpen=False
        }

        private void ShowJurySuggestions(TextBox txt, string role)
        {
            if (juryMemoryService == null) juryMemoryService = new JuryMemoryService();

            var suggestions = juryMemoryService.GetSuggestions(txt.Text, role);

            Popup targetPopup = null;
            ListBox targetList = null;

            if (txt.Name == "txtPresidentJury") { targetPopup = popPresidentJury; targetList = lstPresidentJury; }
            else if (txt.Name == "txtSecretaire") { targetPopup = popSecretaire; targetList = lstSecretaire; }
            else if (txt.Name == "txtMembreJury1") { targetPopup = popMembreJury1; targetList = lstMembreJury1; }
            else if (txt.Name == "txtMembreJury2") { targetPopup = popMembreJury2; targetList = lstMembreJury2; }

            if (targetPopup == null || targetList == null) return;

            if (suggestions.Any())
            {
                targetList.ItemsSource = suggestions;
                targetPopup.IsOpen = true;
            }
            else
            {
                targetPopup.IsOpen = false;
            }
        }

        private void LstJurySuggestion_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ListBox lst && lst.SelectedItem != null)
            {
                string selectedName = lst.SelectedItem.ToString();
                string targetTxtName = lst.Tag?.ToString();

                TextBox targetTxt = null;
                Popup targetPop = null;

                if (targetTxtName == "txtPresidentJury") { targetTxt = txtPresidentJury; targetPop = popPresidentJury; }
                else if (targetTxtName == "txtSecretaire") { targetTxt = txtSecretaire; targetPop = popSecretaire; }
                else if (targetTxtName == "txtMembreJury1") { targetTxt = txtMembreJury1; targetPop = popMembreJury1; }
                else if (targetTxtName == "txtMembreJury2") { targetTxt = txtMembreJury2; targetPop = popMembreJury2; }

                if (targetTxt != null)
                {
                    targetTxt.Text = selectedName;
                    targetTxt.SelectionStart = selectedName.Length;
                }

                if (targetPop != null)
                {
                    targetPop.IsOpen = false;
                }

                lst.SelectedItem = null;
            }
        }

        #endregion

    }
}