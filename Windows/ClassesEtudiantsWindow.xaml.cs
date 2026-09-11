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
    /// Logique d'interaction pour ClassesEtudiantsWindow.xaml
    /// Permet la création des classes, l'inscription des étudiants et l'affichage dynamique.
    /// </summary>
    public partial class ClassesEtudiantsWindow : Window
    {
        private readonly ClasseService _classeService;
        private readonly EtudiantService _etudiantService;

        private List<Classe> _toutesLesClasses;
        private List<Etudiant> _tousLesEtudiants;

        private Etudiant _etudiantEnCoursDeModification = null;
        private Classe _classeEnCoursDeModification = null;

        public ClassesEtudiantsWindow()
        {
            InitializeComponent();
            _classeService = new ClasseService();
            _etudiantService = new EtudiantService();
            _toutesLesClasses = new List<Classe>();
            _tousLesEtudiants = new List<Etudiant>();

            // Initialiser les placeholder pour la recherche
            txtRecherche.GotFocus += TxtRecherche_GotFocus;
            txtRecherche.LostFocus += TxtRecherche_LostFocus;

            // Charger les données au démarrage
            Loaded += (s, e) => ChargerDonnees();
        }

        /// <summary>
        /// Charger et rafraîchir toutes les données (classes et étudiants) depuis la base
        /// </summary>
        public void ChargerDonnees()
        {
            try
            {
                // 1. Charger les classes
                _toutesLesClasses = _classeService.ListerClasses();
                dgClasses.ItemsSource = null;
                dgClasses.ItemsSource = _toutesLesClasses;

                // Mettre à jour ComboBox sélection de classe pour inscription étudiant
                cmbClasseEtudiant.Items.Clear();
                foreach (var c in _toutesLesClasses)
                {
                    if (!string.IsNullOrWhiteSpace(c.NomClasse) && !cmbClasseEtudiant.Items.Contains(c.NomClasse.Trim()))
                    {
                        cmbClasseEtudiant.Items.Add(c.NomClasse.Trim());
                    }
                }
                if (cmbClasseEtudiant.Items.Count > 0)
                {
                    cmbClasseEtudiant.SelectedIndex = 0;
                }

                // Mettre à jour ComboBox filtre par classe
                string selectionFiltreActuel = cmbFiltreClasse.SelectedItem?.ToString();
                cmbFiltreClasse.Items.Clear();
                cmbFiltreClasse.Items.Add("(Toutes les classes)");

                foreach (var c in _toutesLesClasses)
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

                // 2. Charger les étudiants
                _tousLesEtudiants = _etudiantService.ListerEtudiants();
                AppliquerFiltres();

                // Mettre à jour le texte de statut
                txtStatut.Text = $"Prêt | {_toutesLesClasses.Count} classe(s) et {_tousLesEtudiants.Count} étudiant(s) au total dans la base.";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement des données : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Bouton : Créer ou Modifier une Classe
        /// <summary>
        /// Bouton : Créer une Nouvelle Classe
        /// </summary>
        private void BtnCreerClasse_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                MasquerErreurClasse();
                MarquerBordureRouge(txtNomClasse, false);

                string nomClasse = txtNomClasse.Text?.Trim();
                if (string.IsNullOrWhiteSpace(nomClasse))
                {
                    AfficherErreurClasse("⚠️ Veuillez saisir le nom ou code de la classe (ex: 3A40).");
                    MarquerBordureRouge(txtNomClasse, true);
                    txtNomClasse.Focus();
                    return;
                }

                if (nomClasse.Length < 2)
                {
                    AfficherErreurClasse("⚠️ Le nom de la classe doit comporter au moins 2 caractères.");
                    MarquerBordureRouge(txtNomClasse, true);
                    txtNomClasse.Focus();
                    return;
                }

                string niveau = (cmbNiveau.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "";
                string filiere = txtFiliereClasse.Text?.Trim() ?? "";
                string anneeUniv = txtAnneeUniv.Text?.Trim() ?? "2025-2026";
                string description = txtDescriptionClasse.Text?.Trim() ?? "";

                if (_classeEnCoursDeModification != null)
                {
                    // Mode Modification
                    _classeEnCoursDeModification.NomClasse = nomClasse;
                    _classeEnCoursDeModification.Niveau = niveau;
                    _classeEnCoursDeModification.Filiere = filiere;
                    _classeEnCoursDeModification.AnneeUniversitaire = anneeUniv;
                    _classeEnCoursDeModification.Description = description;

                    bool succesModif = _classeService.ModifierClasse(_classeEnCoursDeModification);
                    if (succesModif)
                    {
                        MessageBox.Show($"La classe '{nomClasse}' a été modifiée avec succès !", "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
                        AnnulerModeEditionClasse();
                        ChargerDonnees();
                    }
                    else
                    {
                        AfficherErreurClasse("❌ Échec de la modification de la classe.");
                    }
                    return;
                }

                // Mode Création
                var nouvelleClasse = new Classe
                {
                    NomClasse = nomClasse,
                    Niveau = niveau,
                    Filiere = filiere,
                    AnneeUniversitaire = anneeUniv,
                    Description = description,
                    DateCreation = DateTime.Now
                };

                bool succes = _classeService.AjouterClasse(nouvelleClasse);
                if (succes)
                {
                    MessageBox.Show($"✅ La classe '{nomClasse}' a été créée avec succès !", "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
                    
                    // Réinitialiser les champs du formulaire de classe
                    txtNomClasse.Clear();
                    txtFiliereClasse.Clear();
                    txtDescriptionClasse.Clear();
                    MarquerBordureRouge(txtNomClasse, false);

                    // Rafraîchir les tableaux
                    ChargerDonnees();
                }
                else
                {
                    string details = !string.IsNullOrEmpty(_classeService.DerniereErreur) 
                        ? _classeService.DerniereErreur 
                        : "Veuillez vérifier que les informations saisies sont valides.";
                    AfficherErreurClasse($"❌ Échec de la création de la classe : {details}");
                    MarquerBordureRouge(txtNomClasse, true);
                }
            }
            catch (Exception ex)
            {
                AfficherErreurClasse($"❌ Erreur : {ex.Message}");
            }
        }

        /// <summary>
        /// Bouton : Inscrire ou Modifier un Étudiant avec contrôle de saisie complet
        /// </summary>
        private void BtnInscrireEtudiant_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                MasquerErreurEtudiant();
                ReinitialiserBorduresEtudiant();

                string classeSelectionnee = cmbClasseEtudiant.SelectedItem?.ToString() ?? cmbClasseEtudiant.Text?.Trim();
                if (string.IsNullOrWhiteSpace(classeSelectionnee))
                {
                    AfficherErreurEtudiant("⚠️ Veuillez sélectionner une classe pour l'étudiant.");
                    MarquerBordureRouge(cmbClasseEtudiant, true);
                    cmbClasseEtudiant.Focus();
                    return;
                }

                string nom = txtNomEtudiant.Text?.Trim();
                string prenom = txtPrenomEtudiant.Text?.Trim();

                if (string.IsNullOrWhiteSpace(nom))
                {
                    AfficherErreurEtudiant("⚠️ Le nom de l'étudiant est obligatoire.");
                    MarquerBordureRouge(txtNomEtudiant, true);
                    txtNomEtudiant.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(prenom))
                {
                    AfficherErreurEtudiant("⚠️ Le prénom de l'étudiant est obligatoire.");
                    MarquerBordureRouge(txtPrenomEtudiant, true);
                    txtPrenomEtudiant.Focus();
                    return;
                }

                string matricule = txtMatriculeEtudiant.Text?.Trim();

                // Validation numérique de la moyenne (0.00 à 20.00)
                decimal moyenne = 0m;
                if (!string.IsNullOrWhiteSpace(txtMoyenneEtudiant.Text))
                {
                    string moyenneStr = txtMoyenneEtudiant.Text.Trim().Replace(',', '.');
                    if (!decimal.TryParse(moyenneStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out moyenne) || moyenne < 0m || moyenne > 20m)
                    {
                        AfficherErreurEtudiant("⚠️ La moyenne générale doit être un nombre valide compris entre 0.00 et 20.00.");
                        MarquerBordureRouge(txtMoyenneEtudiant, true);
                        txtMoyenneEtudiant.Focus();
                        return;
                    }
                }

                int ects = 30;
                if (!string.IsNullOrWhiteSpace(txtEctsEtudiant.Text))
                {
                    if (!int.TryParse(txtEctsEtudiant.Text.Trim(), out ects) || ects < 0 || ects > 300)
                    {
                        AfficherErreurEtudiant("⚠️ Le nombre d'ECTS doit être un entier positif (ex: 30).");
                        MarquerBordureRouge(txtEctsEtudiant, true);
                        txtEctsEtudiant.Focus();
                        return;
                    }
                }

                string statut = (cmbStatutEtudiant.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Nouveau";

                if (_etudiantEnCoursDeModification != null)
                {
                    // Mode Modification
                    _etudiantEnCoursDeModification.Nom = nom;
                    _etudiantEnCoursDeModification.Prenom = prenom;
                    _etudiantEnCoursDeModification.NomPrenom = $"{nom} {prenom}";
                    if (!string.IsNullOrWhiteSpace(matricule))
                    {
                        _etudiantEnCoursDeModification.Matricule = matricule;
                    }
                    _etudiantEnCoursDeModification.ClasseGroupe = classeSelectionnee;
                    _etudiantEnCoursDeModification.MoyenneGenerale = moyenne;
                    _etudiantEnCoursDeModification.EctsValides = ects;
                    _etudiantEnCoursDeModification.Statut = statut;
                    _etudiantEnCoursDeModification.Filiere = txtFiliereClasse.Text?.Trim() ?? "";

                    _etudiantEnCoursDeModification.CalculerDecisionEtMention();

                    bool succesModif = _etudiantService.ModifierEtudiant(_etudiantEnCoursDeModification);
                    if (succesModif)
                    {
                        MessageBox.Show($"L'étudiant '{_etudiantEnCoursDeModification.NomPrenom}' a été modifié avec succès !", "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
                        AnnulerModeEditionEtudiant();
                        ChargerDonnees();
                    }
                    else
                    {
                        MessageBox.Show("Échec de la modification de l'étudiant en base de données.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    return;
                }

                // Mode Inscription / Création
                int prochainNumOrdre = 1;
                var etudiantsClasse = _tousLesEtudiants.Where(x => string.Equals(x.ClasseGroupe, classeSelectionnee, StringComparison.OrdinalIgnoreCase)).ToList();
                if (etudiantsClasse.Any())
                {
                    prochainNumOrdre = etudiantsClasse.Max(x => x.NumeroOrdre) + 1;
                }

                var nouvelEtudiant = new Etudiant
                {
                    NumeroOrdre = prochainNumOrdre,
                    Nom = nom,
                    Prenom = prenom,
                    NomPrenom = $"{nom} {prenom}",
                    Matricule = matricule, // Si vide, EtudiantService générera le matricule auto (Année + seq)
                    ClasseGroupe = classeSelectionnee,
                    Filiere = txtFiliereClasse.Text?.Trim() ?? "",
                    Statut = statut,
                    MoyenneGenerale = moyenne,
                    EctsValides = ects,
                    DateCreation = DateTime.Now
                };

                // Calcul automatique de décision
                nouvelEtudiant.CalculerDecisionEtMention();

                bool succes = _etudiantService.AjouterEtudiant(nouvelEtudiant);
                if (succes)
                {
                    MessageBox.Show($"L'étudiant '{nouvelEtudiant.NomPrenom}' a été inscrit avec succès (Matricule: {nouvelEtudiant.Matricule}) !", "Succès", MessageBoxButton.OK, MessageBoxImage.Information);

                    // Réinitialiser les champs du formulaire étudiant
                    txtNomEtudiant.Clear();
                    txtPrenomEtudiant.Clear();
                    txtMatriculeEtudiant.Clear();

                    // Rafraîchir les tableaux
                    ChargerDonnees();
                }
                else
                {
                    MessageBox.Show("Échec de l'inscription de l'étudiant en base de données.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'enregistrement de l'étudiant : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ActiverModeEditionEtudiant(Etudiant etudiant)
        {
            if (etudiant == null) return;
            _etudiantEnCoursDeModification = etudiant;

            txtNomEtudiant.Text = etudiant.Nom;
            txtPrenomEtudiant.Text = etudiant.Prenom;
            txtMatriculeEtudiant.Text = etudiant.Matricule;
            txtMoyenneEtudiant.Text = etudiant.MoyenneGenerale.ToString("0.000");
            txtEctsEtudiant.Text = etudiant.EctsValides.ToString();
            
            if (!string.IsNullOrEmpty(etudiant.ClasseGroupe) && cmbClasseEtudiant.Items.Contains(etudiant.ClasseGroupe))
            {
                cmbClasseEtudiant.SelectedItem = etudiant.ClasseGroupe;
            }
            else if (!string.IsNullOrEmpty(etudiant.ClasseGroupe))
            {
                cmbClasseEtudiant.Text = etudiant.ClasseGroupe;
            }

            foreach (ComboBoxItem item in cmbStatutEtudiant.Items)
            {
                if (item.Content?.ToString() == etudiant.Statut)
                {
                    cmbStatutEtudiant.SelectedItem = item;
                    break;
                }
            }

            btnInscrireEtudiant.Content = "💾 Enregistrer Modification";
            btnInscrireEtudiant.Background = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#27AE60"));
            btnAnnulerEditEtudiant.Visibility = Visibility.Visible;
            txtNomEtudiant.Focus();
        }

        private void AnnulerModeEditionEtudiant()
        {
            _etudiantEnCoursDeModification = null;
            txtNomEtudiant.Clear();
            txtPrenomEtudiant.Clear();
            txtMatriculeEtudiant.Clear();
            txtMoyenneEtudiant.Text = "12.500";
            txtEctsEtudiant.Text = "30";
            btnInscrireEtudiant.Content = "➕ Inscrire l'Étudiant";
            btnInscrireEtudiant.Background = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#2C3E50"));
            btnAnnulerEditEtudiant.Visibility = Visibility.Collapsed;
        }

        private void BtnAnnulerEditEtudiant_Click(object sender, RoutedEventArgs e)
        {
            AnnulerModeEditionEtudiant();
        }

        private void ActiverModeEditionClasse(Classe classe)
        {
            if (classe == null) return;
            _classeEnCoursDeModification = classe;

            txtNomClasse.Text = classe.NomClasse;
            txtFiliereClasse.Text = classe.Filiere;
            txtAnneeUniv.Text = classe.AnneeUniversitaire;
            txtDescriptionClasse.Text = classe.Description;

            foreach (ComboBoxItem item in cmbNiveau.Items)
            {
                if (item.Content?.ToString() == classe.Niveau)
                {
                    cmbNiveau.SelectedItem = item;
                    break;
                }
            }

            btnCreerClasse.Content = "💾 Enregistrer Modification";
            btnCreerClasse.Background = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#27AE60"));
            btnAnnulerEditClasse.Visibility = Visibility.Visible;
            txtNomClasse.Focus();
        }

        private void AnnulerModeEditionClasse()
        {
            _classeEnCoursDeModification = null;
            txtNomClasse.Clear();
            txtFiliereClasse.Clear();
            txtDescriptionClasse.Clear();
            btnCreerClasse.Content = "➕ Créer la Classe";
            btnCreerClasse.Background = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#8B3A3A"));
            btnAnnulerEditClasse.Visibility = Visibility.Collapsed;
        }

        private void BtnAnnulerEditClasse_Click(object sender, RoutedEventArgs e)
        {
            AnnulerModeEditionClasse();
        }

        /// <summary>
        /// Bouton Modifier Sélection (Barre d'outils haut)
        /// </summary>
        private void BtnModifierSelection_Click(object sender, RoutedEventArgs e)
        {
            if (dgEtudiants.SelectedItem is Etudiant etudiantSelectionne)
            {
                ActiverModeEditionEtudiant(etudiantSelectionne);
            }
            else if (dgClasses.SelectedItem is Classe classeSelectionnee)
            {
                ActiverModeEditionClasse(classeSelectionnee);
            }
            else
            {
                MessageBox.Show("Veuillez sélectionner un étudiant ou une classe dans le tableau à modifier.", "Aucune sélection", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        /// <summary>
        /// Bouton Modifier dans une ligne du DataGrid Étudiants
        /// </summary>
        private void BtnModifierEtudiantRow_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is Etudiant etudiant)
            {
                ActiverModeEditionEtudiant(etudiant);
            }
        }

        /// <summary>
        /// Bouton Modifier dans une ligne du DataGrid Classes
        /// </summary>
        private void BtnModifierClasseRow_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is Classe classe)
            {
                ActiverModeEditionClasse(classe);
            }
        }

        /// <summary>
        /// Double-clic sur une ligne du DataGrid Étudiants
        /// </summary>
        private void DgEtudiants_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (dgEtudiants.SelectedItem is Etudiant etudiant)
            {
                ActiverModeEditionEtudiant(etudiant);
            }
        }

        /// <summary>
        /// Double-clic sur une ligne du DataGrid Classes
        /// </summary>
        private void DgClasses_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (dgClasses.SelectedItem is Classe classe)
            {
                ActiverModeEditionClasse(classe);
            }
        }

        /// <summary>
        /// Événement changement de sélection de filtre de classe
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
        /// Appliquer la combinaison des filtres (Classe + Recherche textuelle) sur la DataGrid étudiants
        /// </summary>
        private void AppliquerFiltres()
        {
            if (_tousLesEtudiants == null)
                return;

            IEnumerable<Etudiant> etudiantsFiltres = _tousLesEtudiants;

            // 1. Filtre par Classe
            string classeFiltre = cmbFiltreClasse?.SelectedItem?.ToString();
            if (!string.IsNullOrEmpty(classeFiltre) && classeFiltre != "(Toutes les classes)")
            {
                etudiantsFiltres = etudiantsFiltres.Where(x => string.Equals(x.ClasseGroupe, classeFiltre, StringComparison.OrdinalIgnoreCase));
            }

            // 2. Filtre par Recherche textuelle
            string termeRecherche = txtRecherche?.Text?.Trim();
            if (!string.IsNullOrEmpty(termeRecherche) && termeRecherche != "Rechercher par nom, prénom ou matricule...")
            {
                termeRecherche = termeRecherche.ToLower();
                etudiantsFiltres = etudiantsFiltres.Where(x =>
                    (x.NomPrenom != null && x.NomPrenom.ToLower().Contains(termeRecherche)) ||
                    (x.Matricule != null && x.Matricule.ToLower().Contains(termeRecherche)) ||
                    (x.ClasseGroupe != null && x.ClasseGroupe.ToLower().Contains(termeRecherche)) ||
                    (x.Decision != null && x.Decision.ToLower().Contains(termeRecherche))
                );
            }

            var listeFinale = etudiantsFiltres.ToList();
            dgEtudiants.ItemsSource = null;
            dgEtudiants.ItemsSource = listeFinale;

            if (txtStatut != null)
            {
                txtStatut.Text = $"Affichage : {listeFinale.Count} étudiant(s) sur {_tousLesEtudiants.Count} au total | {_toutesLesClasses.Count} classe(s)";
            }
        }

        /// <summary>
        /// Bouton Rafraîchir tout
        /// </summary>
        private void BtnRafraichir_Click(object sender, RoutedEventArgs e)
        {
            ChargerDonnees();
        }

        /// <summary>
        /// Bouton Supprimer Sélection
        /// </summary>
        private void BtnSupprimerSelection_Click(object sender, RoutedEventArgs e)
        {
            if (dgEtudiants.SelectedItem is Etudiant etudiantSelectionne)
            {
                var result = MessageBox.Show($"Êtes-vous sûr de vouloir supprimer l'étudiant '{etudiantSelectionne.NomPrenom}' ({etudiantSelectionne.Matricule}) ?", 
                    "Confirmation de suppression", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    _etudiantService.SupprimerEtudiant(etudiantSelectionne.Id);
                    ChargerDonnees();
                }
            }
            else if (dgClasses.SelectedItem is Classe classeSelectionnee)
            {
                var result = MessageBox.Show($"Êtes-vous sûr de vouloir supprimer la classe '{classeSelectionnee.NomClasse}' ?", 
                    "Confirmation de suppression", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    _classeService.SupprimerClasse(classeSelectionnee.Id);
                    ChargerDonnees();
                }
            }
            else
            {
                MessageBox.Show("Veuillez sélectionner un étudiant ou une classe à supprimer dans le tableau.", "Aucune sélection", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void TxtRecherche_GotFocus(object sender, RoutedEventArgs e)
        {
            if (txtRecherche.Text == "Rechercher par nom, prénom ou matricule...")
            {
                txtRecherche.Text = "";
                txtRecherche.Foreground = System.Windows.Media.Brushes.Black;
            }
        }

        private void TxtRecherche_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtRecherche.Text))
            {
                txtRecherche.Text = "Rechercher par nom, prénom ou matricule...";
                txtRecherche.Foreground = System.Windows.Media.Brushes.Gray;
            }
        }

        #region Méthodes utilitaires pour la gestion des erreurs et styles

        /// <summary>
        /// Afficher un message d'erreur pour la section Classes
        /// </summary>
        private void AfficherErreurClasse(string message)
        {
            MessageBox.Show(message, "Erreur - Gestion des Classes", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        /// <summary>
        /// Masquer les erreurs de la section Classes (méthode vide car on utilise des MessageBox)
        /// </summary>
        private void MasquerErreurClasse()
        {
            // Cette méthode est appelée pour cohérence mais les erreurs sont affichées en MessageBox
        }

        /// <summary>
        /// Afficher un message d'erreur pour la section Étudiants
        /// </summary>
        private void AfficherErreurEtudiant(string message)
        {
            MessageBox.Show(message, "Erreur - Gestion des Étudiants", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        /// <summary>
        /// Masquer les erreurs de la section Étudiants (méthode vide car on utilise des MessageBox)
        /// </summary>
        private void MasquerErreurEtudiant()
        {
            // Cette méthode est appelée pour cohérence mais les erreurs sont affichées en MessageBox
        }

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
                    control.BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xDC, 0x35, 0x45)); // Rouge erreur
                    control.BorderThickness = new System.Windows.Thickness(2);
                }
                else
                {
                    // Restaurer bordure normale
                    control.BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x8B, 0x3A, 0x3A)); // Rouge normal
                    control.BorderThickness = new System.Windows.Thickness(1);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERREUR] MarquerBordureRouge: {ex.Message}");
            }
        }

        /// <summary>
        /// Réinitialiser toutes les bordures de la section Étudiant
        /// </summary>
        private void ReinitialiserBorduresEtudiant()
        {
            try
            {
                // Réinitialiser les bordures des contrôles d'étudiant
                // Note: Les contrôles doivent exister dans le XAML
                /*
                MarquerBordureRouge(txtNomEtudiant, false);
                MarquerBordureRouge(txtPrenomEtudiant, false);
                MarquerBordureRouge(txtMatriculeEtudiant, false);
                MarquerBordureRouge(txtMoyenneEtudiant, false);
                MarquerBordureRouge(txtEctsEtudiant, false);
                MarquerBordureRouge(cmbClasseEtudiant, false);
                MarquerBordureRouge(cmbStatutEtudiant, false);
                */
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERREUR] ReinitialiserBorduresEtudiant: {ex.Message}");
            }
        }

        #endregion
    }
}
