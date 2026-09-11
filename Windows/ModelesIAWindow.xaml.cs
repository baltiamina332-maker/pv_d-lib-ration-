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
    /// Logique d'interaction pour ModelesIAWindow.xaml
    /// </summary>
    public partial class ModelesIAWindow : Window
    {
        private readonly MlPredictionService _mlService;
        private readonly ClasseService _classeService;
        private List<EtudiantMlPrediction> _toutesLesPredictions;

        public ModelesIAWindow()
        {
            InitializeComponent();
            _mlService = new MlPredictionService();
            _classeService = new ClasseService();
            _toutesLesPredictions = new List<EtudiantMlPrediction>();

            Loaded += ModelesIAWindow_Loaded;
        }

        private void ModelesIAWindow_Loaded(object sender, RoutedEventArgs e)
        {
            ChargerClasses();
            ChargerDonnees();
        }

        private void ChargerClasses()
        {
            try
            {
                cmbClasse.Items.Clear();
                cmbClasse.Items.Add("Toutes les classes");

                var classes = _classeService.ListerClasses();
                if (classes != null && classes.Count > 0)
                {
                    foreach (var c in classes)
                    {
                        if (!string.IsNullOrWhiteSpace(c.NomClasse) && !cmbClasse.Items.Contains(c.NomClasse.Trim()))
                        {
                            cmbClasse.Items.Add(c.NomClasse.Trim());
                        }
                    }
                }

                cmbClasse.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                txtStatut.Text = $"Erreur chargement classes: {ex.Message}";
            }
        }

        private void ChargerDonnees()
        {
            try
            {
                txtStatut.Text = "⏳ Exécution des modèles ML en cours...";
                
                string classeFiltre = null;
                if (cmbClasse.SelectedItem != null && cmbClasse.SelectedIndex > 0)
                {
                    classeFiltre = cmbClasse.SelectedItem.ToString();
                }

                _toutesLesPredictions = _mlService.ObtenirPredictionsMl(classeFiltre);

                AppliquerFiltreRecherche();

                int total = _toutesLesPredictions != null ? _toutesLesPredictions.Count : 0;
                txtStatut.Text = $"✅ Succès | {total} étudiants analysés par Arbre de Décision, KNN et Random Forest.";
            }
            catch (Exception ex)
            {
                txtStatut.Text = $"Erreur lors du calcul ML: {ex.Message}";
            }
        }

        private void AppliquerFiltreRecherche()
        {
            if (_toutesLesPredictions == null)
            {
                dgPredictions.ItemsSource = null;
                return;
            }

            string recherche = txtRecherche.Text != null ? txtRecherche.Text.Trim() : string.Empty;
            if (recherche == "Rechercher par nom, prénom ou matricule...")
            {
                recherche = string.Empty;
            }

            if (string.IsNullOrWhiteSpace(recherche))
            {
                dgPredictions.ItemsSource = _toutesLesPredictions;
            }
            else
            {
                string query = recherche.ToLower();
                var filtre = _toutesLesPredictions.Where(p =>
                    (!string.IsNullOrEmpty(p.NomPrenom) && p.NomPrenom.ToLower().Contains(query)) ||
                    (!string.IsNullOrEmpty(p.Matricule) && p.Matricule.ToLower().Contains(query)) ||
                    (!string.IsNullOrEmpty(p.ClasseGroupe) && p.ClasseGroupe.ToLower().Contains(query))
                ).ToList();

                dgPredictions.ItemsSource = filtre;
            }
        }

        private void BtnLancerIA_Click(object sender, RoutedEventArgs e)
        {
            ChargerDonnees();
            MessageBox.Show("L'exécution des 3 modèles de Machine Learning (scikit-learn) et du Consensus s'est terminée avec succès !",
                            "Analyse ML Réussie", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnImporterExcelML_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new Microsoft.Win32.OpenFileDialog
                {
                    Title = "Sélectionner un fichier Excel pour l'Analyse ML",
                    Filter = "Fichiers Excel (*.xlsx;*.xls;*.csv)|*.xlsx;*.xls;*.csv|Tous les fichiers (*.*)|*.*",
                    FilterIndex = 1
                };

                if (dialog.ShowDialog() == true)
                {
                    string nomFichier = System.IO.Path.GetFileName(dialog.FileName);
                    txtStatut.Text = $"⏳ Importation & exécution ML du fichier '{nomFichier}'...";

                    _toutesLesPredictions = _mlService.ObtenirPredictionsMlDepuisExcel(dialog.FileName);
                    AppliquerFiltreRecherche();

                    int count = _toutesLesPredictions != null ? _toutesLesPredictions.Count : 0;
                    txtStatut.Text = $"✅ Succès | {count} étudiants extraits de '{nomFichier}' et analysés par Arbre de Décision, KNN et Random Forest.";

                    MessageBox.Show($"L'importation et l'analyse ML du fichier '{nomFichier}' par les 3 modèles (Arbre de Décision, KNN, Random Forest) et le Consensus s'est terminée avec succès !\n\nNombre d'étudiants analysés : {count}",
                                    "Analyse Excel ML Réussie", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'importation ou de l'analyse du fichier Excel : {ex.Message}", "Erreur ML Excel", MessageBoxButton.OK, MessageBoxImage.Error);
                txtStatut.Text = $"Erreur analyse ML Excel: {ex.Message}";
            }
        }

        private void CmbClasse_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded)
            {
                ChargerDonnees();
            }
        }

        private void TxtRecherche_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (IsLoaded)
            {
                AppliquerFiltreRecherche();
            }
        }

        private void BtnRafraichir_Click(object sender, RoutedEventArgs e)
        {
            ChargerDonnees();
        }
    }
}
