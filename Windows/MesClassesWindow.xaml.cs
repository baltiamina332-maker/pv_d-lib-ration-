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
    /// Logique d'interaction pour MesClassesWindow.xaml
    /// Espace Enseignant : Version simplifiée en LECTURE SEULE.
    /// Affiche uniquement les classes attribuées à l'enseignant connecté et leurs étudiants.
    /// </summary>
    public partial class MesClassesWindow : Window
    {
        private readonly ClasseService _classeService;
        private readonly EtudiantService _etudiantService;
        private readonly AffectationService _affectationService;

        private List<Classe> _classesAttribuees;
        private List<Etudiant> _etudiantsMesClasses;
        private List<string> _nomsClassesAffectees;

        public MesClassesWindow()
        {
            InitializeComponent();

            _classeService = new ClasseService();
            _etudiantService = new EtudiantService();
            _affectationService = new AffectationService();

            _classesAttribuees = new List<Classe>();
            _etudiantsMesClasses = new List<Etudiant>();
            _nomsClassesAffectees = new List<string>();

            // Placeholders pour la recherche
            txtRecherche.GotFocus += TxtRecherche_GotFocus;
            txtRecherche.LostFocus += TxtRecherche_LostFocus;

            // Charger les données au démarrage
            Loaded += (s, e) => ChargerDonnees();
        }

        /// <summary>
        /// Charger et filtrer les classes & étudiants attribués à l'enseignant connecté (EF-05 / Espace Enseignant)
        /// </summary>
        public void ChargerDonnees()
        {
            try
            {
                var currentUser = AuthenticationService.CurrentUser;
                string nomEnseignant = currentUser?.FullName ?? currentUser?.Username ?? "";

                // 1. Récupérer la liste des classes attribuées à cet enseignant
                _nomsClassesAffectees = _affectationService.ListerClassesPourEnseignant(nomEnseignant);

                // 2. Charger toutes les classes et ne garder que celles attribuées
                var toutesClasses = _classeService.ListerClasses();
                if (_nomsClassesAffectees.Count > 0)
                {
                    _classesAttribuees = toutesClasses
                        .Where(c => _nomsClassesAffectees.Any(a => string.Equals(a, c.NomClasse?.Trim(), StringComparison.OrdinalIgnoreCase)))
                        .ToList();
                }
                else
                {
                    // Si aucune restriction spécifique enregistrée en base, afficher toutes les classes
                    _classesAttribuees = toutesClasses;
                }

                dgClasses.ItemsSource = null;
                dgClasses.ItemsSource = _classesAttribuees;

                // Mettre à jour ComboBox filtre par classe
                string selectionFiltreActuel = cmbFiltreClasse.SelectedItem?.ToString();
                cmbFiltreClasse.Items.Clear();
                cmbFiltreClasse.Items.Add("(Toutes mes classes)");

                foreach (var c in _classesAttribuees)
                {
                    if (!string.IsNullOrWhiteSpace(c.NomClasse) && !cmbFiltreClasse.Items.Contains(c.NomClasse.Trim()))
                    {
                        cmbFiltreClasse.Items.Add(c.NomClasse.Trim());
                    }
                }

                if (!string.IsNullOrEmpty(selectionFiltreActuel) && cmbFiltreClasse.Items.Contains(selectionFiltreActuel))
                {
                    cmbFiltreClasse.SelectedItem = selectionFiltreActuel;
                }
                else
                {
                    cmbFiltreClasse.SelectedIndex = 0;
                }

                // 3. Charger tous les étudiants et filtrer selon les classes attribuées
                var tousEtudiants = _etudiantService.ListerEtudiants();
                if (_nomsClassesAffectees.Count > 0)
                {
                    _etudiantsMesClasses = tousEtudiants
                        .Where(e => _nomsClassesAffectees.Any(a => string.Equals(a, e.ClasseGroupe?.Trim(), StringComparison.OrdinalIgnoreCase)))
                        .ToList();
                }
                else
                {
                    _etudiantsMesClasses = tousEtudiants;
                }

                AppliquerFiltres();

                // Mettre à jour le texte de statut
                txtStatut.Text = $"Enseignant: {nomEnseignant} | {_classesAttribuees.Count} classe(s) attribuée(s) | {_etudiantsMesClasses.Count} étudiant(s) au total (Lecture Seule).";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement des classes de l'enseignant : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Appliquer la recherche texte et le filtre classe sur le DataGrid Étudiants
        /// </summary>
        private void AppliquerFiltres()
        {
            if (_etudiantsMesClasses == null) return;

            IEnumerable<Etudiant> resultats = _etudiantsMesClasses;

            // Filtre par Classe
            string classeSelectionnee = cmbFiltreClasse.SelectedItem?.ToString();
            if (!string.IsNullOrWhiteSpace(classeSelectionnee) && classeSelectionnee != "(Toutes mes classes)")
            {
                resultats = resultats.Where(e => string.Equals(e.ClasseGroupe?.Trim(), classeSelectionnee, StringComparison.OrdinalIgnoreCase));
            }

            // Filtre par recherche Texte (Nom, Prénom, Matricule)
            string recherche = txtRecherche.Text?.Trim();
            if (!string.IsNullOrWhiteSpace(recherche) && recherche != "Rechercher un étudiant par nom, prénom ou matricule...")
            {
                resultats = resultats.Where(e =>
                    (e.NomPrenom != null && e.NomPrenom.IndexOf(recherche, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (e.Matricule != null && e.Matricule.IndexOf(recherche, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (e.ClasseGroupe != null && e.ClasseGroupe.IndexOf(recherche, StringComparison.OrdinalIgnoreCase) >= 0)
                );
            }

            dgEtudiants.ItemsSource = null;
            dgEtudiants.ItemsSource = resultats.ToList();
        }

        private void CmbFiltreClasse_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            AppliquerFiltres();
        }

        private void TxtRecherche_TextChanged(object sender, TextChangedEventArgs e)
        {
            AppliquerFiltres();
        }

        private void BtnRafraichir_Click(object sender, RoutedEventArgs e)
        {
            ChargerDonnees();
        }

        private void TxtRecherche_GotFocus(object sender, RoutedEventArgs e)
        {
            if (txtRecherche.Text == "Rechercher un étudiant par nom, prénom ou matricule...")
            {
                txtRecherche.Text = "";
                txtRecherche.Foreground = System.Windows.Media.Brushes.Black;
            }
        }

        private void TxtRecherche_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtRecherche.Text))
            {
                txtRecherche.Text = "Rechercher un étudiant par nom, prénom ou matricule...";
                txtRecherche.Foreground = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#718096"));
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
