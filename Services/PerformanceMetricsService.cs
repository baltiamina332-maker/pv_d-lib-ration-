using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using DesktopApp.Models;

namespace DesktopApp.Services
{
    /// <summary>
    /// Service pour mesurer les performances selon les exigences CDC Section 7
    /// - Génération PV en moins de 5 secondes
    /// - Traitement de 50 classes en moins de 2 minutes
    /// </summary>
    public class PerformanceMetricsService
    {
        private List<MetriquePerformance> _metriques;
        private Stopwatch _chronometre;

        public PerformanceMetricsService()
        {
            _metriques = new List<MetriquePerformance>();
            _chronometre = new Stopwatch();
        }

        /// <summary>
        /// Démarrer la mesure d'une opération
        /// </summary>
        public void DemarrerMesure(string nomOperation, string details = "")
        {
            _chronometre.Restart();
            Console.WriteLine($"[PERF] Début: {nomOperation} {details}");
        }

        /// <summary>
        /// Arrêter la mesure et enregistrer la métrique
        /// </summary>
        public MetriquePerformance ArreterMesure(string nomOperation, string details = "", int nombreElements = 1)
        {
            _chronometre.Stop();
            
            var metrique = new MetriquePerformance
            {
                NomOperation = nomOperation,
                Details = details,
                DureeMs = _chronometre.ElapsedMilliseconds,
                DureeSecondes = _chronometre.ElapsedMilliseconds / 1000.0,
                NombreElements = nombreElements,
                DateMesure = DateTime.Now
            };

            // Calculer les performances par élément
            if (nombreElements > 0)
            {
                metrique.DureeMoyenneParElement = metrique.DureeSecondes / nombreElements;
            }

            // Évaluer la conformité CDC
            metrique.ConformeCDC = EvaluerConformiteCDC(metrique);
            metrique.MessageConformite = ObtenirMessageConformite(metrique);

            _metriques.Add(metrique);

            Console.WriteLine($"[PERF] Fin: {nomOperation} - {metrique.DureeSecondes:F2}s ({metrique.DureeMs}ms) - {metrique.MessageConformite}");

            return metrique;
        }

        /// <summary>
        /// Évaluer si les performances respectent les seuils CDC
        /// </summary>
        private bool EvaluerConformiteCDC(MetriquePerformance metrique)
        {
            switch (metrique.NomOperation.ToLower())
            {
                case "generation_pv":
                case "generer_pv":
                    // CDC: Génération PV en moins de 5 secondes
                    return metrique.DureeSecondes <= 5.0;

                case "traitement_classes":
                case "traitement_50_classes":
                    // CDC: Traitement de 50 classes en moins de 2 minutes (120s)
                    if (metrique.NombreElements >= 50)
                    {
                        return metrique.DureeSecondes <= 120.0;
                    }
                    // Estimation linéaire pour moins de 50 classes
                    double seuilEstime = (metrique.NombreElements * 120.0) / 50.0;
                    return metrique.DureeSecondes <= seuilEstime;

                case "import_excel":
                    // Estimation: Import Excel devrait être rapide (< 2 secondes pour un fichier normal)
                    return metrique.DureeSecondes <= 2.0;

                case "export_excel":
                    // Estimation: Export Excel devrait être rapide (< 3 secondes)
                    return metrique.DureeSecondes <= 3.0;

                case "calcul_decisions":
                    // Estimation: Calcul des décisions devrait être très rapide (< 1 seconde pour 100 étudiants)
                    double seuilCalcul = Math.Max(1.0, metrique.NombreElements * 0.01);
                    return metrique.DureeSecondes <= seuilCalcul;

                default:
                    // Pour les autres opérations, considérer comme conforme si < 10 secondes
                    return metrique.DureeSecondes <= 10.0;
            }
        }

        /// <summary>
        /// Obtenir le message de conformité
        /// </summary>
        private string ObtenirMessageConformite(MetriquePerformance metrique)
        {
            if (metrique.ConformeCDC)
            {
                return "✅ CONFORME CDC";
            }

            switch (metrique.NomOperation.ToLower())
            {
                case "generation_pv":
                case "generer_pv":
                    return $"❌ NON CONFORME CDC (> 5s): {metrique.DureeSecondes:F2}s";

                case "traitement_classes":
                case "traitement_50_classes":
                    double seuilAttendu = metrique.NombreElements >= 50 ? 120.0 : (metrique.NombreElements * 120.0) / 50.0;
                    return $"❌ NON CONFORME CDC (> {seuilAttendu:F0}s pour {metrique.NombreElements} classes): {metrique.DureeSecondes:F2}s";

                default:
                    return $"⚠️ PERFORMANCE DÉGRADÉE: {metrique.DureeSecondes:F2}s";
            }
        }

        /// <summary>
        /// Obtenir toutes les métriques
        /// </summary>
        public List<MetriquePerformance> ObtenirMetriques()
        {
            return new List<MetriquePerformance>(_metriques);
        }

        /// <summary>
        /// Obtenir les métriques par opération
        /// </summary>
        public List<MetriquePerformance> ObtenirMetriques(string nomOperation)
        {
            return _metriques.FindAll(m => m.NomOperation.Equals(nomOperation, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Obtenir un rapport de performance
        /// </summary>
        public RapportPerformance GenererRapport()
        {
            var rapport = new RapportPerformance();
            rapport.DateGeneration = DateTime.Now;
            rapport.NombreOperations = _metriques.Count;

            if (_metriques.Count == 0)
            {
                rapport.MessageResume = "Aucune métrique enregistrée";
                return rapport;
            }

            // Statistiques générales
            int conformes = 0;
            double dureeTotal = 0;
            var operationsLentes = new List<MetriquePerformance>();

            foreach (var m in _metriques)
            {
                if (m.ConformeCDC) conformes++;
                dureeTotal += m.DureeSecondes;

                if (!m.ConformeCDC)
                {
                    operationsLentes.Add(m);
                }
            }

            rapport.PourcentageConformite = (conformes * 100.0) / _metriques.Count;
            rapport.DureeMoyenne = dureeTotal / _metriques.Count;
            rapport.OperationsLentes = operationsLentes;

            // Message de résumé
            if (rapport.PourcentageConformite >= 95)
            {
                rapport.MessageResume = $"✅ EXCELLENT - {rapport.PourcentageConformite:F1}% conforme CDC";
            }
            else if (rapport.PourcentageConformite >= 80)
            {
                rapport.MessageResume = $"⚠️ BON - {rapport.PourcentageConformite:F1}% conforme CDC";
            }
            else
            {
                rapport.MessageResume = $"❌ À AMÉLIORER - {rapport.PourcentageConformite:F1}% conforme CDC";
            }

            return rapport;
        }

        /// <summary>
        /// Exporter les métriques vers un fichier CSV
        /// </summary>
        public bool ExporterMetriques(string cheminFichier)
        {
            try
            {
                using (var writer = new StreamWriter(cheminFichier, false, System.Text.Encoding.UTF8))
                {
                    // En-tête CSV
                    writer.WriteLine("Date,Operation,Details,DureeMs,DureeSecondes,NombreElements,DureeMoyenneParElement,ConformeCDC,MessageConformite");

                    // Données
                    foreach (var m in _metriques)
                    {
                        writer.WriteLine($"{m.DateMesure:yyyy-MM-dd HH:mm:ss},{m.NomOperation},{m.Details},{m.DureeMs},{m.DureeSecondes:F3},{m.NombreElements},{m.DureeMoyenneParElement:F6},{m.ConformeCDC},{m.MessageConformite}");
                    }
                }

                Console.WriteLine($"[PERF] Métriques exportées vers: {cheminFichier}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PERF] Erreur export métriques: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Effacer toutes les métriques
        /// </summary>
        public void EffacerMetriques()
        {
            _metriques.Clear();
            Console.WriteLine("[PERF] Métriques effacées");
        }
    }

    /// <summary>
    /// Métrique de performance d'une opération
    /// </summary>
    public class MetriquePerformance
    {
        public DateTime DateMesure { get; set; }
        public string NomOperation { get; set; } = "";
        public string Details { get; set; } = "";
        public long DureeMs { get; set; }
        public double DureeSecondes { get; set; }
        public int NombreElements { get; set; }
        public double DureeMoyenneParElement { get; set; }
        public bool ConformeCDC { get; set; }
        public string MessageConformite { get; set; } = "";
    }

    /// <summary>
    /// Rapport de performance global
    /// </summary>
    public class RapportPerformance
    {
        public DateTime DateGeneration { get; set; }
        public int NombreOperations { get; set; }
        public double PourcentageConformite { get; set; }
        public double DureeMoyenne { get; set; }
        public string MessageResume { get; set; } = "";
        public List<MetriquePerformance> OperationsLentes { get; set; } = new List<MetriquePerformance>();
    }
}