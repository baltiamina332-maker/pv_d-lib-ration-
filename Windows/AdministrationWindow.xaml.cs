using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using DesktopApp.Models;
using DesktopApp.Services;

namespace DesktopApp
{
    /// <summary>
    /// Logique d'interaction pour AdministrationWindow.xaml
    /// Gère l'administration des utilisateurs, le workflow d'approbation des comptes (Approuver / Révoquer) et les changements de rôle (Admin / Enseignant).
    /// </summary>
    public partial class AdministrationWindow : Window
    {
        private readonly AuthenticationService _authService;
        private List<User> _tousLesUtilisateurs;
        
        // Variables pour le drag & drop
        private bool _isDragging = false;
        private Point _startPoint;
        private User _draggedUser;

        public AdministrationWindow()
        {
            InitializeComponent();
            _authService = new AuthenticationService();
            _tousLesUtilisateurs = new List<User>();

            txtRecherche.GotFocus += TxtRecherche_GotFocus;
            txtRecherche.LostFocus += TxtRecherche_LostFocus;

            // Activation du drag & drop pour réorganiser les lignes
            dgUtilisateurs.AllowDrop = true;
            dgUtilisateurs.PreviewMouseLeftButtonDown += DgUtilisateurs_PreviewMouseLeftButtonDown;
            dgUtilisateurs.MouseMove += DgUtilisateurs_MouseMove;
            dgUtilisateurs.DragOver += DgUtilisateurs_DragOver;
            dgUtilisateurs.Drop += DgUtilisateurs_Drop;

            Loaded += (s, e) => ChargerDonnees();
        }

        /// <summary>
        /// Charger la liste des utilisateurs
        /// </summary>
        public void ChargerDonnees()
        {
            try
            {
                _tousLesUtilisateurs = _authService.GetAllUsers();
                AppliquerFiltres();

                txtStatut.Text = $"Prêt | {_tousLesUtilisateurs.Count} compte(s) utilisateur(s) dans le système.";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement des utilisateurs : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Bouton : Créer un nouvel utilisateur avec contrôle de saisie complet
        /// </summary>
        private void BtnCreerUtilisateur_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                MasquerErreurFormulaire();
                ReinitialiserBorduresChamps();

                string username = txtUsername.Text?.Trim();
                string fullName = txtFullName.Text?.Trim();
                string email = txtEmail.Text?.Trim();
                string password = txtPassword.Text?.Trim();

                // 1. Contrôle de saisie Nom d'utilisateur
                if (!AuthenticationService.ValidateUsername(username, out string usernameErr))
                {
                    AfficherErreurFormulaire(usernameErr);
                    MarquerChampErreur(txtUsername, true);
                    txtUsername.Focus();
                    return;
                }

                // Vérification unicité du login
                var existingUser = _authService.FindUserByUsernameOrEmail(username);
                if (existingUser != null)
                {
                    AfficherErreurFormulaire($"⚠️ Le nom d'utilisateur '{username}' est déjà utilisé.");
                    MarquerChampErreur(txtUsername, true);
                    txtUsername.Focus();
                    return;
                }

                // 2. Contrôle de saisie Nom & Prénom
                if (string.IsNullOrWhiteSpace(fullName))
                {
                    AfficherErreurFormulaire("⚠️ Le nom et prénom sont obligatoires.");
                    MarquerChampErreur(txtFullName, true);
                    txtFullName.Focus();
                    return;
                }

                if (fullName.Length < 2)
                {
                    AfficherErreurFormulaire("⚠️ Le nom et prénom doivent comporter au moins 2 caractères.");
                    MarquerChampErreur(txtFullName, true);
                    txtFullName.Focus();
                    return;
                }

                // 3. Contrôle de saisie Adresse Email
                if (!string.IsNullOrWhiteSpace(email) && !AuthenticationService.ValidateEmail(email, out string emailErr))
                {
                    AfficherErreurFormulaire(emailErr);
                    MarquerChampErreur(txtEmail, true);
                    txtEmail.Focus();
                    return;
                }

                // 4. Contrôle de saisie Mot de Passe
                if (!AuthenticationService.ValidatePassword(password, out string pwdErr))
                {
                    AfficherErreurFormulaire(pwdErr);
                    MarquerChampErreur(txtPassword, true);
                    txtPassword.Focus();
                    return;
                }

                UserRole role = UserRole.Enseignant;
                string selectedRole = (cmbRoleInitial.SelectedItem as ComboBoxItem)?.Content?.ToString();
                if (string.Equals(selectedRole, "Admin", StringComparison.OrdinalIgnoreCase))
                {
                    role = UserRole.Admin;
                }

                // Statut par défaut pour tous les nouveaux comptes
                StatutCompte statut = StatutCompte.EnAttente;

                var nouvelUtilisateur = new User
                {
                    Username = username,
                    FullName = fullName,
                    Email = email ?? "",
                    Password = password,
                    Role = role,
                    Statut = statut,
                    DateCreation = DateTime.Now
                };

                bool succes = _authService.AddUser(nouvelUtilisateur);
                if (succes)
                {
                    MessageBox.Show($"✅ Le compte '{username}' ({role}, Statut: {statut}) a été créé avec succès !", "Création réussie", MessageBoxButton.OK, MessageBoxImage.Information);

                    // Réinitialiser le formulaire
                    txtUsername.Clear();
                    txtFullName.Clear();
                    txtEmail.Clear();
                    txtPassword.Text = "pass123";
                    ReinitialiserBorduresChamps();
                    MasquerErreurFormulaire();

                    ChargerDonnees();
                }
                else
                {
                    AfficherErreurFormulaire($"❌ Erreur lors de l'ajout de l'utilisateur '{username}'.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la création du compte : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Action Bouton : Approuver le compte utilisateur
        /// </summary>
        private void BtnApprouverRow_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is User user)
            {
                bool succes = _authService.ApprouverUtilisateur(user.Id);
                if (succes)
                {
                    MessageBox.Show($"Le compte '{user.Username}' a été APPROUVÉ avec succès !", "Approbation réussie", MessageBoxButton.OK, MessageBoxImage.Information);
                    ChargerDonnees();
                }
            }
        }

        /// <summary>
        /// Action Bouton : Révoquer le compte utilisateur (avec protection contre l'auto-révocation)
        /// </summary>
        private void BtnRevoquerRow_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is User user)
            {
                var currentLoggedIn = AuthenticationService.CurrentUser;
                if (currentLoggedIn != null && currentLoggedIn.Id == user.Id)
                {
                    MessageBox.Show("⚠️ Vous ne pouvez pas révoquer votre propre compte actuellement connecté !", "Action impossible", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var confirm = MessageBox.Show($"Voulez-vous vraiment RÉVOQUER l'accès de l'utilisateur '{user.Username}' ?", 
                    "Confirmation de révocation", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (confirm == MessageBoxResult.Yes)
                {
                    bool succes = _authService.RevoquerUtilisateur(user.Id);
                    if (succes)
                    {
                        MessageBox.Show($"Le compte '{user.Username}' a été RÉVOQUÉ avec succès.", "Révocation effectuée", MessageBoxButton.OK, MessageBoxImage.Information);
                        ChargerDonnees();
                    }
                }
            }
        }

        /// <summary>
        /// Événement : Changement de rôle (ComboBox dans la cellule du DataGrid)
        /// </summary>
        private void CmbRoleRow_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox combo && combo.DataContext is User user && combo.IsLoaded)
            {
                if (combo.SelectedItem is ComboBoxItem item)
                {
                    string roleStr = item.Content?.ToString() ?? item.Tag?.ToString();
                    UserRole nvxRole = string.Equals(roleStr, "Admin", StringComparison.OrdinalIgnoreCase) ? UserRole.Admin : UserRole.Enseignant;

                    if (user.Role != nvxRole)
                    {
                        _authService.ChangerRoleUtilisateur(user.Id, nvxRole);
                        txtStatut.Text = $"Rôle de '{user.Username}' modifié vers : {nvxRole}";
                    }
                }
            }
        }

        /// <summary>
        /// Action Bouton : Supprimer un utilisateur (avec protection contre l'auto-suppression)
        /// </summary>
        private void BtnSupprimerRow_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is User user)
            {
                var currentLoggedIn = AuthenticationService.CurrentUser;
                if (currentLoggedIn != null && currentLoggedIn.Id == user.Id)
                {
                    MessageBox.Show("⚠️ Vous ne pouvez pas supprimer votre propre compte actuellement connecté !", "Action impossible", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var confirm = MessageBox.Show($"Êtes-vous sûr de vouloir supprimer définitivement l'utilisateur '{user.Username}' ?", 
                    "Confirmation de suppression", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (confirm == MessageBoxResult.Yes)
                {
                    _authService.DeleteUser(user.Id);
                    ChargerDonnees();
                }
            }
        }

        #region Helpers de validation visuelle du formulaire Admin

        private void TxtFormField_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox tb)
            {
                MarquerChampErreur(tb, false);
            }

            MasquerErreurFormulaire();
        }

        private void AfficherErreurFormulaire(string message)
        {
            if (txtFormError != null && borderFormError != null)
            {
                txtFormError.Text = message;
                borderFormError.Visibility = Visibility.Visible;
            }
        }

        private void MasquerErreurFormulaire()
        {
            if (txtFormError != null && borderFormError != null)
            {
                txtFormError.Text = "";
                borderFormError.Visibility = Visibility.Collapsed;
            }
        }

        private void MarquerChampErreur(TextBox textBox, bool isError)
        {
            if (textBox == null) return;

            if (isError)
            {
                textBox.BorderBrush = System.Windows.Media.Brushes.Red;
                textBox.BorderThickness = new Thickness(1.5);
            }
            else
            {
                textBox.BorderBrush = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFromString("#CBD5E0");
                textBox.BorderThickness = new Thickness(1.0);
            }
        }

        private void ReinitialiserBorduresChamps()
        {
            MarquerChampErreur(txtUsername, false);
            MarquerChampErreur(txtFullName, false);
            MarquerChampErreur(txtEmail, false);
            MarquerChampErreur(txtPassword, false);
        }

        #endregion

        private void CmbFiltreStatut_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            AppliquerFiltres();
        }

        private void TxtRecherche_TextChanged(object sender, TextChangedEventArgs e)
        {
            AppliquerFiltres();
        }

        /// <summary>
        /// Appliquer la recherche textuelle et le filtrage par statut d'approbation
        /// </summary>
        private void AppliquerFiltres()
        {
            if (_tousLesUtilisateurs == null)
                return;

            IEnumerable<User> resultats = _tousLesUtilisateurs;

            // 1. Filtre Statut
            string statutFiltre = (cmbFiltreStatut?.SelectedItem as ComboBoxItem)?.Content?.ToString();
            if (!string.IsNullOrEmpty(statutFiltre) && statutFiltre != "(Tous les statuts)")
            {
                if (Enum.TryParse(statutFiltre, true, out StatutCompte searchStatut))
                {
                    resultats = resultats.Where(u => u.Statut == searchStatut);
                }
            }

            // 2. Filtre Texte
            string terme = txtRecherche?.Text?.Trim();
            if (!string.IsNullOrEmpty(terme) && terme != "Rechercher par nom d'utilisateur ou email...")
            {
                terme = terme.ToLower();
                resultats = resultats.Where(u => 
                    (u.Username != null && u.Username.ToLower().Contains(terme)) ||
                    (u.FullName != null && u.FullName.ToLower().Contains(terme)) ||
                    (u.Email != null && u.Email.ToLower().Contains(terme))
                );
            }

            var list = resultats.ToList();
            dgUtilisateurs.ItemsSource = null;
            dgUtilisateurs.ItemsSource = list;

            if (txtStatut != null)
            {
                txtStatut.Text = $"Affichage : {list.Count} utilisateur(s) sur {_tousLesUtilisateurs.Count} au total.";
            }
        }

        private void BtnRafraichir_Click(object sender, RoutedEventArgs e)
        {
            ChargerDonnees();
        }

        private void TxtRecherche_GotFocus(object sender, RoutedEventArgs e)
        {
            if (txtRecherche.Text == "Rechercher par nom d'utilisateur ou email...")
            {
                txtRecherche.Text = "";
                txtRecherche.Foreground = System.Windows.Media.Brushes.Black;
            }
        }

        private void TxtRecherche_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtRecherche.Text))
            {
                txtRecherche.Text = "Rechercher par nom d'utilisateur ou email...";
                txtRecherche.Foreground = System.Windows.Media.Brushes.Gray;
            }
        }

        #region Méthodes utilitaires pour la gestion des erreurs

        /// <summary>
        /// Marquer ou démarquer la bordure rouge d'un contrôle pour indiquer une erreur
        /// </summary>
        private void MarquerBordureRouge(Control control, bool marquer)
        {
            try
            {
                if (control == null) return;

                if (marquer)
                {
                    // Appliquer bordure rouge pour erreur
                    control.BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xDC, 0x35, 0x45));
                    control.BorderThickness = new System.Windows.Thickness(2);
                }
                else
                {
                    // Restaurer bordure normale
                    control.BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x8B, 0x3A, 0x3A));
                    control.BorderThickness = new System.Windows.Thickness(1);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERREUR] MarquerBordureRouge: {ex.Message}");
            }
        }

        #endregion

        #region Méthodes pour le Drag & Drop des lignes du tableau

        /// <summary>
        /// Détecter le début du drag
        /// </summary>
        private void DgUtilisateurs_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _startPoint = e.GetPosition(dgUtilisateurs);
            var row = GetDataGridRowFromPoint(_startPoint);
            if (row != null)
            {
                _draggedUser = row.Item as User;
            }
        }

        /// <summary>
        /// Gérer le mouvement de la souris pour initier le drag
        /// </summary>
        private void DgUtilisateurs_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && !_isDragging && _draggedUser != null)
            {
                Point currentPosition = e.GetPosition(dgUtilisateurs);
                
                // Vérifier si la souris s'est suffisamment déplacée pour commencer le drag
                if (Math.Abs(currentPosition.X - _startPoint.X) > SystemParameters.MinimumHorizontalDragDistance ||
                    Math.Abs(currentPosition.Y - _startPoint.Y) > SystemParameters.MinimumVerticalDragDistance)
                {
                    _isDragging = true;
                    DragDrop.DoDragDrop(dgUtilisateurs, _draggedUser, DragDropEffects.Move);
                    _isDragging = false;
                }
            }
        }

        /// <summary>
        /// Gérer le survol pendant le drag
        /// </summary>
        private void DgUtilisateurs_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.Move;
        }

        /// <summary>
        /// Gérer le drop pour réorganiser les lignes
        /// </summary>
        private void DgUtilisateurs_Drop(object sender, DragEventArgs e)
        {
            try
            {
                if (e.Data.GetDataPresent(typeof(User)))
                {
                    User droppedUser = e.Data.GetData(typeof(User)) as User;
                    Point dropPosition = e.GetPosition(dgUtilisateurs);
                    DataGridRow targetRow = GetDataGridRowFromPoint(dropPosition);

                    if (targetRow != null && targetRow.Item is User targetUser && droppedUser != targetUser)
                    {
                        var currentList = dgUtilisateurs.ItemsSource as List<User>;
                        if (currentList != null)
                        {
                            // Réorganiser la liste
                            int oldIndex = currentList.IndexOf(droppedUser);
                            int newIndex = currentList.IndexOf(targetUser);

                            if (oldIndex != -1 && newIndex != -1)
                            {
                                currentList.RemoveAt(oldIndex);
                                currentList.Insert(newIndex, droppedUser);

                                // Rafraîchir l'affichage
                                dgUtilisateurs.ItemsSource = null;
                                dgUtilisateurs.ItemsSource = currentList;

                                // Sélectionner la ligne déplacée
                                dgUtilisateurs.SelectedItem = droppedUser;
                                
                                txtStatut.Text = $"Ligne déplacée | Utilisateur '{droppedUser.Username}' repositionné.";
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du déplacement : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Obtenir la ligne du DataGrid à partir d'une position
        /// </summary>
        private DataGridRow GetDataGridRowFromPoint(Point position)
        {
            var element = dgUtilisateurs.InputHitTest(position) as UIElement;
            while (element != null && !(element is DataGridRow))
            {
                element = VisualTreeHelper.GetParent(element) as UIElement;
            }
            return element as DataGridRow;
        }

        #endregion
    }
}
