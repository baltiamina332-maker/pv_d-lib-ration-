using System;
using System.Collections.Generic;
using System.Linq;
using DesktopApp.Models;

namespace DesktopApp.Services
{
    /// <summary>
    /// Service pour calculer la moyenne et les décisions à partir des données importées d'Excel
    /// </summary>
    public class DecisionCalculatorService
    {
        // Seuils de décision
        private const decimal MOYENNE_ADMIS = 12.0m;      // Moyenne pour Admis
        private const decimal MOYENNE_RATTRAPAGE = 10.0m; // Moyenne pour Rattrapage
        // Moins de 10 = Refusé

        public DecisionCalculatorService()
        {
        }

        /// <summary>
        /// Calculer la moyenne générale à partir des grades des modules
        /// Formule: MG = Σ(Moyenne_Module × Crédit) / Σ(Crédit)
        /// </summary>
        public decimal CalculerMoyenneDepuisModules(List<ModuleGrades> modules)
        {
            try
            {
                if (modules == null || modules.Count == 0)
                {
                    Console.WriteLine("[CALC] ⚠️ Aucun module trouvé pour le calcul de moyenne");
                    return 0;
                }

                decimal totalCredits = 0;
                decimal totalPondere = 0;

                foreach (var module in modules)
                {
                    // Calculer la moyenne pour ce module: (CC + TP + Examen) / 3
                    decimal moyenneModule = module.CalculerMoyenneModule();
                    
                    // Appliquer le crédit: Moyenne_Module × Crédit
                    decimal pondere = moyenneModule * module.Credit;
                    
                    totalPondere += pondere;
                    totalCredits += module.Credit;

                    Console.WriteLine($"  Module: {module.NomModule} | CC:{module.CC:F2} TP:{module.TP:F2} Exam:{module.Examen:F2} | Moy:{moyenneModule:F2} × Crédit:{module.Credit} = {pondere:F2}");
                }

                if (totalCredits == 0)
                {
                    Console.WriteLine("[CALC] ⚠️ Total des crédits est 0");
                    return 0;
                }

                // MG = Σ(Moy_Module × Crédit) / Σ(Crédit)
                decimal mg = totalPondere / totalCredits;
                Console.WriteLine($"  TOTAL PONDÉRÉ: {totalPondere:F2} / TOTAL CRÉDITS: {totalCredits} = MG: {mg:F2}");
                
                return mg;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CALC] Erreur calcul moyenne depuis modules: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Calculer la moyenne générale pour un étudiant
        /// Supporte deux cas: 
        /// - Cas 1: L'étudiant a des modules avec grades (CC, TP, Examen, Crédit)
        /// - Cas 2: La moyenne est déjà pré-calculée
        /// </summary>
        public decimal CalculerMoyenne(Etudiant etudiant)
        {
            try
            {
                // Cas 1: L'étudiant a des modules avec grades (CC, TP, Examen)
                if (etudiant.ModulesGrades != null && etudiant.ModulesGrades.Count > 0)
                {
                    Console.WriteLine($"[CALC] Calcul de moyenne pour {etudiant.NomPrenom} à partir de {etudiant.ModulesGrades.Count} module(s):");
                    decimal moyenneCalculee = CalculerMoyenneDepuisModules(etudiant.ModulesGrades);
                    return moyenneCalculee;
                }

                // Cas 2: La moyenne est déjà calculée et fournie
                if (etudiant.MoyenneGenerale > 0)
                {
                    Console.WriteLine($"[CALC] Moyenne existante pour {etudiant.NomPrenom}: {etudiant.MoyenneGenerale:F2}");
                    return etudiant.MoyenneGenerale;
                }

                Console.WriteLine($"[CALC] ❌ Aucune moyenne trouvée pour {etudiant.NomPrenom}");
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CALC] Erreur calcul moyenne: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Déterminer la décision basée sur la moyenne générale
        /// </summary>
        public string DeterminerDecision(decimal moyenne)
        {
            if (moyenne >= MOYENNE_ADMIS)
            {
                return "Admis";
            }
            else if (moyenne >= MOYENNE_RATTRAPAGE)
            {
                return "Ajourné";
            }
            else
            {
                return "Exclu";
            }
        }

        /// <summary>
        /// Traiter tous les étudiants importés
        /// Calculer moyennes et décisions selon les règles complexes
        /// </summary>
        public List<Etudiant> TraiterEtudiants(List<Etudiant> etudiants)
        {
            Console.WriteLine("\n════════════════════════════════════════════════════════════");
            Console.WriteLine("  CALCUL DES DÉCISIONS D'ADMISSION (RÈGLES COMPLEXES)");
            Console.WriteLine("════════════════════════════════════════════════════════════\n");

            foreach (var etudiant in etudiants)
            {
                try
                {
                    // 1. Calculer la moyenne (si nécessaire)
                    if (etudiant.MoyenneGenerale == 0 && etudiant.ModulesGrades.Count > 0)
                    {
                        decimal moyenne = CalculerMoyenne(etudiant);
                        etudiant.MoyenneGenerale = moyenne;
                    }

                    // 2. Appliquer les NOUVELLES RÈGLES COMPLEXES
                    etudiant.CalculerDecisionEtMention();

                    Console.WriteLine($"✓ {etudiant.NomPrenom} ({etudiant.Matricule}) - MG: {etudiant.MoyenneGenerale:F2} - ECTS: {etudiant.EctsValides} - Décision: {etudiant.Decision} - Mention: {etudiant.Mention}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ Erreur pour {etudiant.NomPrenom}: {ex.Message}");
                    etudiant.Decision = "Erreur";
                    etudiant.Mention = ex.Message;
                }
            }

            // Afficher les statistiques
            AfficherStatistiques(etudiants);

            Console.WriteLine("\n════════════════════════════════════════════════════════════\n");

            return etudiants;
        }

        /// <summary>
        /// Afficher les statistiques des décisions
        /// </summary>
        private void AfficherStatistiques(List<Etudiant> etudiants)
        {
            int admis = etudiants.Count(e => e.Decision != null && e.Decision.StartsWith("Admis"));
            int rattrapage = etudiants.Count(e => e.Decision != null && (e.Decision == "Ajourné" || e.Decision.Contains("rattrapage") || e.Decision == "Conseil d'École"));
            int exclu = etudiants.Count(e => e.Decision != null && (e.Decision == "Refusé" || e.Decision.Contains("Exclu") || e.Decision.Contains("Redouble")));
            int erreur = etudiants.Count(e => e.Decision == "Erreur");
            int total = etudiants.Count;

            decimal moyAdmis = etudiants.Where(e => e.Decision != null && e.Decision.StartsWith("Admis")).Any()
                ? (decimal)etudiants.Where(e => e.Decision != null && e.Decision.StartsWith("Admis"))
                    .Average(e => (double)e.MoyenneGenerale)
                : 0;

            decimal moyGenerale = total > 0
                ? (decimal)etudiants.Average(e => (double)e.MoyenneGenerale)
                : 0;

            Console.WriteLine("[STATS] ════════════════════════════════════════════");
            Console.WriteLine($"[STATS] Résultats de la délibération:");
            Console.WriteLine($"[STATS]   🟢 Admis:                       {admis} ({(total > 0 ? (decimal)admis / total * 100 : 0):F1}%)");
            Console.WriteLine($"[STATS]   🟡 Ajourné (Rattrapage):        {rattrapage} ({(total > 0 ? (decimal)rattrapage / total * 100 : 0):F1}%)");
            Console.WriteLine($"[STATS]   🔴 Refusé / Exclu:              {exclu} ({(total > 0 ? (decimal)exclu / total * 100 : 0):F1}%)");
            if (erreur > 0)
                Console.WriteLine($"[STATS]   ⚠️  Erreurs:                     {erreur}");
            Console.WriteLine($"[STATS]   ────────────────────────────────");
            Console.WriteLine($"[STATS]   Total:                          {total}");
            Console.WriteLine($"[STATS]   Moyenne générale promo:         {moyGenerale:F2}");
            Console.WriteLine($"[STATS]   Moyenne des admis:              {moyAdmis:F2}");
            Console.WriteLine($"[STATS] ════════════════════════════════════════════\n");
        }

        /// <summary>
        /// Obtenir les seuils actuels
        /// </summary>
        public Dictionary<string, decimal> ObtenirSeuils()
        {
            return new Dictionary<string, decimal>
            {
                { "Admis", MOYENNE_ADMIS },
                { "Rattrapage", MOYENNE_RATTRAPAGE }
            };
        }

        /// <summary>
        /// Obtenir un résumé des décisions
        /// </summary>
        public string ObtenirResume(List<Etudiant> etudiants)
        {
            int admis = etudiants.Count(e => e.Decision != null && e.Decision.StartsWith("Admis"));
            int rattrapage = etudiants.Count(e => e.Decision != null && (e.Decision == "Ajourné" || e.Decision.Contains("rattrapage") || e.Decision == "Conseil d'École"));
            int exclu = etudiants.Count(e => e.Decision != null && (e.Decision == "Refusé" || e.Decision.Contains("Exclu") || e.Decision.Contains("Redouble")));
            int total = etudiants.Count;

            decimal moyGenerale = total > 0
                ? Math.Round((decimal)etudiants.Average(e => (double)e.MoyenneGenerale), 2)
                : 0;

            decimal tauxReussite = total > 0 ? Math.Round((decimal)admis / total * 100, 1) : 0;

            return $"🟢 Admis: {admis} ({tauxReussite}%)\n" +
                   $"🟡 Ajourné (Rattrapage): {rattrapage}\n" +
                   $"🔴 Refusé / Exclu: {exclu}\n" +
                   $"─────────────────────────\n" +
                   $"📊 Total: {total} | Moy. promo: {moyGenerale:F2}";
        }
    }
}
