using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using DesktopApp.Models;
using DesktopApp.Services;

namespace DesktopApp
{
    /// <summary>
    /// Logique d'interaction pour AffectationsWindow.xaml
    /// Permet d'affecter un enseignant et une matière à une classe et de gérer le Registre des Affectations (Ajout, Modification, Suppression, Filtres).
    /// </summary>
    public partial class AffectationsWindow : Window
    {
        private readonly AffectationService _affectationService;
        private readonly ClasseService _classeService;

        private List<Affectation> _toutesLesAffectations;
        private Affectation _affectationEnCours;

        public AffectationsWindow()
        {
            InitializeComponent();
            _affectationService = new AffectationService();
            _classeService = new ClasseService();
            _toutesLesAffectations = new List<Affectation>();

            txtRecherche.GotFocus += TxtRecherche_GotFocus;
            txtRecherche.LostFocus += TxtRecherche_LostFocus;

            Loaded += (s, e) => ChargerDonnees();
        }

        /// <summary>
        /// Charger toutes les données (Classes et Affectations)
        /// </summary>
        public void ChargerDonnees()
        {
            try
            {
                // 1. Charger les classes pour les ComboBox
                var classes = _classeService.ListerClasses();
                cmbClasse.Items.Clear();
                cmbFiltreClasse.Items.Clear();
                cmbFiltreClasse.Items.Add("(Toutes les classes)");

                foreach (var c in classes)
                {
                    if (!string.IsNullOrWhiteSpace(c.NomClasse))
                    {
                        string nomClean = c.NomClasse.Trim();
                        if (!cmbClasse.Items.Contains(nomClean))
                        {
                            cmbClasse.Items.Add(nomClean);
                        }
                        if (!cmbFiltreClasse.Items.Contains(nomClean))
                        {
                            cmbFiltreClasse.Items.Add(nomClean);
                        }
                    }
                }
                if (cmbClasse.Items.Count > 0)
                {
                    cmbClasse.SelectedIndex = 0;
                }
                cmbFiltreClasse.SelectedIndex = 0;

                // 2. Charger les affectations depuis la base
                _toutesLesAffectations = _affectationService.ListerAffectations();
                AppliquerFiltres();

                txtStatut.Text = $"Prêt | {_toutesLesAffectations.Count} affectation(s) enregistrée(s) dans le registre.";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement des affectations : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Bouton : Enregistrer l'Affectation (Création ou Modification)
        /// </summary>
        private void BtnEnregistrer_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string enseignant = cmbEnseignant.Text?.Trim();
                string matiere = txtMatiere.Text?.Trim();
                string classe = cmbClasse.Text?.Trim();
                string anneeUniv = txtAnneeUniv.Text?.Trim() ?? "2025-2026";
                // Statut par défaut pour toutes les affectations
                string statut = "Actif";

                if (string.IsNullOrWhiteSpace(enseignant))
                {
                    MessageBox.Show("Veuillez sélectionner ou saisir le nom de l'enseignant.", "Champ requis", MessageBoxButton.OK, MessageBoxImage.Warning);
                    cmbEnseignant.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(matiere))
                {
                    MessageBox.Show("Veuillez saisir le nom de la matière / module.", "Champ requis", MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtMatiere.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(classe))
                {
                    MessageBox.Show("Veuillez sélectionner ou saisir le code de la classe.", "Champ requis", MessageBoxButton.OK, MessageBoxImage.Warning);
                    cmbClasse.Focus();
                    return;
                }

                bool succes = false;

                if (_affectationEnCours != null && _affectationEnCours.Id > 0)
                {
                    // Modification
                    _affectationEnCours.Enseignant = enseignant;
                    _affectationEnCours.Matiere = matiere;
                    _affectationEnCours.NomClasse = classe;
                    _affectationEnCours.AnneeUniversitaire = anneeUniv;
                    _affectationEnCours.Statut = statut;

                    succes = _affectationService.ModifierAffectation(_affectationEnCours);
                    if (succes)
                    {
                        MessageBox.Show($"L'affectation #{_affectationEnCours.Id} a été mise à jour avec succès !", "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                else
                {
                    // Création
                    var nouvelleAffectation = new Affectation
                    {
                        Enseignant = enseignant,
                        Matiere = matiere,
                        NomClasse = classe,
                        AnneeUniversitaire = anneeUniv,
                        Statut = statut,
                        DateAffectation = DateTime.Now
                    };

                    succes = _affectationService.AjouterAffectation(nouvelleAffectation);
                    if (succes)
                    {
                        MessageBox.Show($"L'affectation de '{enseignant}' pour le cours '{matiere}' ({classe}) a été enregistrée avec succès !", "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }

                if (succes)
                {
                    ReinitialiserFormulaire();
                    ChargerDonnees();
                    // Force refresh of DataGrid
                    dgAffectations.Items.Refresh();
                }
                else
                {
                    string msgErreur = !string.IsNullOrWhiteSpace(_affectationService.DerniereErreur)
                        ? _affectationService.DerniereErreur
                        : "Erreur lors de l'enregistrement en base de données. Vérifiez la connexion MySQL.";
                    MessageBox.Show(msgErreur, "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'enregistrement de l'affectation : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Action : Préparer le formulaire pour la modification d'une ligne sélectionnée
        /// </summary>
        private void BtnEditer_Click(object sender, RoutedEventArgs e)
        {
            if (dgAffectations.SelectedItem is Affectation selection)
            {
                _affectationEnCours = selection;
                cmbEnseignant.Text = selection.Enseignant;
                txtMatiere.Text = selection.Matiere;
                cmbClasse.Text = selection.NomClasse;
                txtAnneeUniv.Text = selection.AnneeUniversitaire;

                // Pas de sélection de statut - défaut à "Actif"

                borderMode.Visibility = Visibility.Visible;
                txtModeFormulaire.Text = $"✏️ Mode Édition : Modification de l'affectation #{selection.Id}";
                btnEnregistrer.Content = "💾 Mettre à Jour l'Affectation";
            }
            else
            {
                MessageBox.Show("Veuillez sélectionner une affectation dans le tableau à modifier.", "Aucune sélection", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        /// <summary>
        /// Action : Supprimer l'affectation sélectionnée
        /// </summary>
        private void BtnSupprimer_Click(object sender, RoutedEventArgs e)
        {
            if (dgAffectations.SelectedItem is Affectation selection)
            {
                var result = MessageBox.Show($"Êtes-vous sûr de vouloir supprimer l'affectation #{selection.Id} ({selection.Enseignant} - {selection.Matiere} - {selection.NomClasse}) ?", 
                    "Confirmation de suppression", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    bool succes = _affectationService.SupprimerAffectation(selection.Id);
                    if (succes)
                    {
                        MessageBox.Show("L'affectation a été supprimée avec succès.", "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
                        ReinitialiserFormulaire();
                        ChargerDonnees();
                    }
                    else
                    {
                        MessageBox.Show("Échec de la suppression de l'affectation.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show("Veuillez sélectionner une affectation dans le tableau à supprimer.", "Aucune sélection", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        /// <summary>
        /// Bouton Réinitialiser le formulaire
        /// </summary>
        private void BtnReinitialiser_Click(object sender, RoutedEventArgs e)
        {
            ReinitialiserFormulaire();
        }

        /// <summary>
        /// Réinitialiser les champs du formulaire
        /// </summary>
        private void ReinitialiserFormulaire()
        {
            _affectationEnCours = null;
            cmbEnseignant.Text = "";
            txtMatiere.Clear();
            cmbClasse.Text = "";
            txtAnneeUniv.Text = "2025-2026";

            borderMode.Visibility = Visibility.Collapsed;
            btnEnregistrer.Content = "💾 Enregistrer l'Affectation";
        }

        /// <summary>
        /// Événement Changement de Sélection dans la DataGrid
        /// </summary>
        private void DgAffectations_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgAffectations.SelectedItem is Affectation selection)
            {
                // Selection made
                // TODO: Update UI if needed
            }
            else
            {
                // No selection
            }
        }

        /// <summary>
        /// Événement changement de filtre par classe
        /// </summary>
        private void CmbFiltreClasse_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            AppliquerFiltres();
        }

        /// <summary>
        /// Événement modification du champ de recherche textuelle
        /// </summary>
        private void TxtRecherche_TextChanged(object sender, TextChangedEventArgs e)
        {
            AppliquerFiltres();
        }

        /// <summary>
        /// Filtrer les affectations affichées dans la DataGrid
        /// </summary>
        private void AppliquerFiltres()
        {
            if (_toutesLesAffectations == null)
                return;

            IEnumerable<Affectation> affectationsFiltrees = _toutesLesAffectations;

            // 1. Filtre par Classe
            string classeFiltre = cmbFiltreClasse?.SelectedItem?.ToString();
            if (!string.IsNullOrEmpty(classeFiltre) && classeFiltre != "(Toutes les classes)")
            {
                affectationsFiltrees = affectationsFiltrees.Where(x => string.Equals(x.NomClasse, classeFiltre, StringComparison.OrdinalIgnoreCase));
            }

            // 2. Filtre par Recherche textuelle (Enseignant ou Matière)
            string termeRecherche = txtRecherche?.Text?.Trim();
            if (!string.IsNullOrEmpty(termeRecherche) && termeRecherche != "Rechercher...")
            {
                termeRecherche = termeRecherche.ToLower();
                affectationsFiltrees = affectationsFiltrees.Where(x =>
                    (x.Enseignant != null && x.Enseignant.ToLower().Contains(termeRecherche)) ||
                    (x.Matiere != null && x.Matiere.ToLower().Contains(termeRecherche)) ||
                    (x.NomClasse != null && x.NomClasse.ToLower().Contains(termeRecherche))
                );
            }

            var listeFinale = affectationsFiltrees.ToList();
            dgAffectations.ItemsSource = null;
            dgAffectations.ItemsSource = listeFinale;

            if (txtStatut != null)
            {
                txtStatut.Text = $"Affichage : {listeFinale.Count} affectation(s) sur {_toutesLesAffectations.Count} au total.";
            }
        }

        private void BtnRafraichir_Click(object sender, RoutedEventArgs e)
        {
            ChargerDonnees();
        }

        private void TxtRecherche_GotFocus(object sender, RoutedEventArgs e)
        {
            if (txtRecherche.Text == "Rechercher...")
            {
                txtRecherche.Text = "";
                txtRecherche.Foreground = System.Windows.Media.Brushes.Black;
            }
        }

        private void TxtRecherche_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtRecherche.Text))
            {
                txtRecherche.Text = "Rechercher...";
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
    }
}
