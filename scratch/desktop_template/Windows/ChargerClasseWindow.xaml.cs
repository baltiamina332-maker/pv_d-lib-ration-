using System.Windows;

namespace DesktopApp
{
    /// <summary>
    /// Fenêtre de dialogue pour charger les étudiants d'une classe
    /// </summary>
    public partial class ChargerClasseWindow : Window
    {
        public string ClasseSelectionnee { get; private set; }
        public int? SessionSelectionnee { get; private set; }
        public bool RecalculerDecisions { get; private set; }
        public bool AfficherStatistiques { get; private set; }

        public ChargerClasseWindow() : this(null)
        {
        }

        public ChargerClasseWindow(System.Collections.Generic.List<string> classesDisponibles)
        {
            InitializeComponent();

            if (classesDisponibles != null && classesDisponibles.Count > 0)
            {
                cmbClasse.Items.Clear();
                cmbClasse.Items.Add(new System.Windows.Controls.ComboBoxItem { Content = "(Toutes les classes)", IsSelected = true });
                foreach (var cls in classesDisponibles)
                {
                    cmbClasse.Items.Add(new System.Windows.Controls.ComboBoxItem { Content = cls });
                }
            }
        }

        private void BtnCharger_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Récupérer la classe
                var classeText = cmbClasse.Text?.Trim();
                if (string.IsNullOrEmpty(classeText))
                {
                    MessageBox.Show("Veuillez saisir un code de classe.", "Validation", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    cmbClasse.Focus();
                    return;
                }

                ClasseSelectionnee = classeText;

                // Récupérer la session
                var sessionItem = cmbSession.SelectedItem as System.Windows.Controls.ComboBoxItem;
                if (sessionItem != null && sessionItem.Content.ToString().Contains("1 -"))
                {
                    SessionSelectionnee = 1;
                }
                else if (sessionItem != null && sessionItem.Content.ToString().Contains("2 -"))
                {
                    SessionSelectionnee = 2;
                }
                else
                {
                    SessionSelectionnee = null; // Toutes les sessions
                }

                // Récupérer les options
                RecalculerDecisions = chkRecalculerDecisions.IsChecked == true;
                AfficherStatistiques = chkAfficherStatistiques.IsChecked == true;

                DialogResult = true;
                Close();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Erreur: {ex.Message}", "Erreur", 
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