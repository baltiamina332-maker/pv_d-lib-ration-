using System.Windows;
using DesktopApp.Models;

namespace DesktopApp
{
    /// <summary>
    /// Fenêtre de dialogue pour corriger manuellement un étudiant
    /// </summary>
    public partial class CorrectionEtudiantWindow : Window
    {
        private readonly Etudiant _etudiant;

        public string NouvelleDecision { get; private set; }
        public string NouvelleMention { get; private set; }
        public string NouvelleObservation { get; private set; }

        public CorrectionEtudiantWindow(Etudiant etudiant)
        {
            InitializeComponent();
            _etudiant = etudiant;
            InitializeForm();
        }

        private void InitializeForm()
        {
            if (_etudiant == null) return;

            // Afficher les informations de l'étudiant
            txtEtudiantInfo.Text = $"Étudiant: {_etudiant.NomPrenom} - {_etudiant.Matricule}";
            
            // Afficher les valeurs actuelles
            txtMoyenneActuelle.Text = _etudiant.MoyenneGenerale.ToString("F2");
            txtDecisionActuelle.Text = string.IsNullOrEmpty(_etudiant.Decision) ? "(Non calculée)" : _etudiant.Decision;
            txtMentionActuelle.Text = string.IsNullOrEmpty(_etudiant.Mention) ? "(Non calculée)" : _etudiant.Mention;
            
            // Préremplir l'observation actuelle
            if (!string.IsNullOrEmpty(_etudiant.Observation))
            {
                txtNouvelleObservation.Text = _etudiant.Observation;
            }

            // Gérer le placeholder
            UpdatePlaceholderVisibility();
        }

        /// <summary>
        /// Gestionnaire pour le changement de texte dans la TextBox d'observation
        /// </summary>
        private void TxtNouvelleObservation_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            UpdatePlaceholderVisibility();
        }

        /// <summary>
        /// Met à jour la visibilité du placeholder
        /// </summary>
        private void UpdatePlaceholderVisibility()
        {
            if (txtPlaceholder != null && txtNouvelleObservation != null)
            {
                txtPlaceholder.Visibility = string.IsNullOrEmpty(txtNouvelleObservation.Text) 
                    ? System.Windows.Visibility.Visible 
                    : System.Windows.Visibility.Collapsed;
            }
        }

        private void BtnValider_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Récupérer les nouvelles valeurs
                var decisionItem = cmbNouvelleDecision.SelectedItem as System.Windows.Controls.ComboBoxItem;
                var mentionItem = cmbNouvelleMention.SelectedItem as System.Windows.Controls.ComboBoxItem;

                // Vérifier si des changements ont été faits
                bool hasChanges = false;

                if (decisionItem != null && decisionItem.Content.ToString() != "(Pas de changement)")
                {
                    NouvelleDecision = decisionItem.Content.ToString();
                    hasChanges = true;
                }

                if (mentionItem != null && mentionItem.Content.ToString() != "(Pas de changement)")
                {
                    NouvelleMention = mentionItem.Content.ToString();
                    hasChanges = true;
                }

                var observationText = txtNouvelleObservation.Text?.Trim();
                if (!string.IsNullOrEmpty(observationText) && observationText != _etudiant.Observation)
                {
                    NouvelleObservation = observationText;
                    hasChanges = true;
                }

                if (!hasChanges)
                {
                    MessageBox.Show("Aucune modification détectée.", "Information", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Confirmer les changements
                var message = "Confirmer les corrections suivantes :\n\n";
                if (!string.IsNullOrEmpty(NouvelleDecision))
                    message += $"• Nouvelle décision: {NouvelleDecision}\n";
                if (!string.IsNullOrEmpty(NouvelleMention))
                    message += $"• Nouvelle mention: {NouvelleMention}\n";
                if (!string.IsNullOrEmpty(NouvelleObservation))
                    message += $"• Observation mise à jour\n";

                var result = MessageBox.Show(message, "Confirmer les Corrections", 
                    MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    DialogResult = true;
                    Close();
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Erreur lors de la validation: {ex.Message}", "Erreur", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnAnnuler_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}