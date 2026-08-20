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
        private List<Etudiant> etudiatsActuels; // Stocker les étudiants actuels
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
                etudiatsActuels = new List<Etudiant>();

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
                txtStatutImport.Text = "⏳ Chargement et validation en cours...";
                this.Cursor = Cursors.Wait;

                // Importer les données
                var result = excelService.ImporterDonneesExcel(cheminFichier);

                this.Cursor = null;

                if (result.Succes)
                {
                    // Stocker les étudiants actuels
                    etudiatsActuels = result.Etudiants;

                    // NOUVEAU: Calculer les décisions
                    Console.WriteLine("[UI] Calcul des décisions après import...");
                    var decisionCalcService = new DecisionCalculatorService();
                    etudiatsActuels = decisionCalcService.TraiterEtudiants(etudiatsActuels);

                    // DEBUG: Vérifier le nombre d'étudiants
                    Console.WriteLine($"\n[UI] ═══════════════════════════════════════════════════════");
                    Console.WriteLine($"[UI] DÉBUG: Nombre d'étudiants après TraiterEtudiants():");
                    Console.WriteLine($"[UI]   etudiatsActuels.Count = {etudiatsActuels.Count}");
                    foreach (var et in etudiatsActuels)
                    {
                        Console.WriteLine($"[UI]     - {et.NumeroOrdre}. {et.NomPrenom} ({et.Matricule}) - Décision: {et.Decision}");
                    }
                    Console.WriteLine($"[UI] ═══════════════════════════════════════════════════════\n");

                    // Afficher les données dans le DataGrid
                    Console.WriteLine($"[UI] Liaison des données au DataGrid: {etudiatsActuels.Count} étudiants");
                    dgDonnees.ItemsSource = null; // Forcer le rafraîchissement
                    dgDonnees.ItemsSource = etudiatsActuels;
                    Console.WriteLine($"[UI] DataGrid.Items.Count après binding: {dgDonnees.Items.Count}");
                    
                    // DEBUG: Liste tous les items du DataGrid
                    Console.WriteLine($"[UI] Items dans le DataGrid:");
                    for (int i = 0; i < dgDonnees.Items.Count; i++)
                    {
                        var item = dgDonnees.Items[i] as Etudiant;
                        if (item != null)
                            Console.WriteLine($"[UI]   [{i}] {item.NumeroOrdre}. {item.NomPrenom}");
                    }
                    Console.WriteLine($"[UI] ═══════════════════════════════════════════════════════\n");
                    
                    // S'assurer que le DataGrid est visible ET contient les données
                    dgDonnees.Visibility = Visibility.Visible;
                    dgDonnees.UpdateLayout();
                    
                    // FORCER l'actualisation de l'interface
                    this.UpdateLayout();
                    MettreAJourCompteurEtudiants();
                    
                    // Activer le bouton d'export
                    btnExporterExcel.IsEnabled = true;

                    // Afficher le résumé des décisions
                    string resumeDecisions = decisionCalcService.ObtenirResume(etudiatsActuels);
                    txtStatutImport.Text = $"✅ {result.MessageSucces}\n\n{resumeDecisions}";

                    // Afficher les avertissements s'il y en a
                    if (result.Avertissements.Any())
                    {
                        string avertissements = string.Join("\n", result.Avertissements);
                        MessageBox.Show($"Import réussi avec quelques avertissements :\n\n{avertissements}", 
                            "Avertissements", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                    else
                    {
                        MessageBox.Show($"{result.MessageSucces}\n\n{resumeDecisions}", "Succès", 
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                else
                {
                    // Masquer le DataGrid en cas d'erreur
                    dgDonnees.Visibility = Visibility.Collapsed;
                    
                    // Afficher l'erreur
                    txtStatutImport.Text = $"❌ Erreur : {result.MessageErreur}";
                    MessageBox.Show(result.MessageErreur, "Erreur d'import", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                this.Cursor = null;
                
                // Masquer le DataGrid en cas d'exception
                dgDonnees.Visibility = Visibility.Collapsed;
                
                MessageBox.Show($"Erreur inattendue lors du chargement : {ex.Message}", "Erreur", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
                txtStatutImport.Text = $"❌ Erreur : {ex.Message}";
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
        /// Générer le PV de délibération en Word
        /// </summary>
        public void GenererPVWord()
        {
            try
            {
                if (etudiatsActuels == null || etudiatsActuels.Count == 0)
                {
                    MessageBox.Show("Aucune donnée à générer. Veuillez d'abord importer un fichier Excel.",
                        "Attention", MessageBoxButton.OK, MessageBoxImage.Warning);
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

                string classeGroupe = etudiatsActuels[0].ClasseGroupe ?? "Sans classe";
                string dateActuelle = DateTime.Now.ToString("yyyy-MM-dd_HHmmss");
                string nomFichier = $"PV_{classeGroupe}_{dateActuelle}.docx";

                string dossierSortie = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "PV_Générés"
                );

                if (!Directory.Exists(dossierSortie))
                    Directory.CreateDirectory(dossierSortie);

                this.Cursor = Cursors.Wait;
                bool succes = wordService.GenererPV(etudiatsActuels, dossierSortie, nomFichier, classeGroupe, jury);
                this.Cursor = null;

                if (succes)
                {
                    string cheminComplet = Path.Combine(dossierSortie, nomFichier);

                    // Archiver le PV
                    archiveService.ArchiverPV(cheminComplet, classeGroupe, etudiatsActuels, jury.TypeSession);

                    // Ajouter à l'historique
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
                    try { txtStatutGeneration.Text = $"✅ PV généré : {nomFichier}"; } catch { }

                    var resultMessage = MessageBox.Show(
                        $"✅ PV généré avec succès!\n\nFichier: {nomFichier}\nChemin: {dossierSortie}\n\nVoulez-vous ouvrir le fichier?",
                        "Succès", MessageBoxButton.YesNo, MessageBoxImage.Information);

                    if (resultMessage == MessageBoxResult.Yes)
                        System.Diagnostics.Process.Start(cheminComplet);
                }
                else
                {
                    MessageBox.Show("Erreur lors de la génération du PV.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                this.Cursor = null;
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
    }
}