using System;
using System.Collections.Generic;

namespace DesktopApp.Models
{
    /// <summary>
    /// Modèle représentant un étudiant avec ses résultats
    /// Implémente les nouvelles règles de décision basées sur MG et ECTS
    /// </summary>
    public class Etudiant
    {
        // Propriétés de base (correspondent à la base de données)
        public int Id { get; set; }
        public int NumeroOrdre { get; set; }
        public string Nom { get; set; }
        public string Prenom { get; set; }
        public string NomPrenom { get; set; }
        public string Matricule { get; set; }
        public string CNE => Matricule;         // Alias requis par le CDC
        public string ClasseGroupe { get; set; }
        public string Filiere { get; set; }     // Filière/Spécialité
        public string Statut { get; set; }
        public decimal MoyenneGenerale { get; set; }
        public string MoyenneOriginale { get; set; }  // NOUVEAU: Moyenne exacte du fichier Excel
        public int EctsValides { get; set; }    // Gardé pour compatibilité
        public int EctsTotal { get; set; }      // Total ECTS (par défaut 30)
        public bool EstAncienEtudiant { get; set; }
        public decimal MoyenneUE { get; set; }
        public string Decision { get; set; }
        public string Mention { get; set; }
        public string Validation { get; set; }  // Oui / Non / Partiel (CDC)
        public int Rang { get; set; }           // Rang dans la classe
        public string Observation { get; set; } // Texte explicatif de la décision
        
        // Propriétés de métadonnées (nouveau ajoutées pour suivi)
        public DateTime DateCalcul { get; set; }          // NEW: When decision was calculated
        public string RescueType { get; set; }            // NEW: Which rescue condition applied (if any)
        
        // Propriétés existantes de métadonnées
        public int IdSession { get; set; }
        public string AnneeUniversitaire { get; set; }

        // Propriétés pour stocker les grades bruts (pour calcul de moyenne)
        // Utilisé quand les grades CC, TP, Examen sont fournis au lieu d'une MG pré-calculée
        public List<ModuleGrades> ModulesGrades { get; set; }  // Liste des notes par module

        // Constructeur
        public Etudiant()
        {
            NomPrenom = string.Empty;
            Matricule = string.Empty;
            ClasseGroupe = string.Empty;
            Filiere = string.Empty;
            Decision = string.Empty;
            Mention = string.Empty;
            Validation = string.Empty;
            Observation = string.Empty;
            AnneeUniversitaire = string.Empty;
            RescueType = string.Empty;
            MoyenneOriginale = string.Empty;  // NOUVEAU: Initialiser la moyenne originale
            EctsTotal = 30;
            EstAncienEtudiant = false;
            MoyenneUE = 0;
            Rang = 0;
            DateCalcul = DateTime.Now;
            ModulesGrades = new List<ModuleGrades>();
        }

        /// <summary>
        /// Calculer automatiquement la décision selon les règles du Cahier des Charges
        /// MG >= 10: Admis (Passable, Assez Bien, Bien, Très Bien)
        /// 8 <= MG < 10: Ajourné (Session de rattrapage)
        /// MG < 8: Refusé
        /// </summary>
        /// <summary>
        /// Calculer automatiquement la décision selon les règles du Cahier des Charges (Annexe C)
        /// Mode 1: Si Décision et Mention sont déjà fournies (dans le fichier Excel), les conserver.
        /// Mode 2 (Calcul automatique):
        ///   MG >= 16: Admis (Très Bien)
        ///   14 <= MG < 16: Admis (Bien)
        ///   12 <= MG < 14: Admis (Assez Bien)
        ///   10 <= MG < 12: Admis (Passable)
        ///   8 <= MG < 10: Ajourné (Session de rattrapage)
        ///   MG < 8: Exclu / Ajourné (—)
        /// </summary>
        public void CalculerDecisionEtMention(DecisionRule rule = null)
        {
            DateCalcul = DateTime.Now;

            // Si la décision est déjà fournie (Mode 1 du CDC) et non vide, on la conserve
            if (!string.IsNullOrWhiteSpace(Decision) && Decision != "Erreur")
            {
                // Si la mention est manquante, on la calcule selon la MG
                if (string.IsNullOrWhiteSpace(Mention) && Decision.StartsWith("Admis", StringComparison.OrdinalIgnoreCase))
                {
                    Mention = ObtenirMention(MoyenneGenerale, 
                        rule?.SeuilTresBien ?? 16.0m, 
                        rule?.SeuilBien ?? 14.0m, 
                        rule?.SeuilAssezBien ?? 12.0m, 
                        rule?.SeuilPassable ?? 10.0m);
                }

                if (string.IsNullOrWhiteSpace(Validation))
                {
                    Validation = Decision.StartsWith("Admis", StringComparison.OrdinalIgnoreCase) ? "Oui" : "Non";
                }

                if (string.IsNullOrWhiteSpace(Observation))
                {
                    Observation = Decision.StartsWith("Admis", StringComparison.OrdinalIgnoreCase)
                        ? $"Admis avec mention {(string.IsNullOrWhiteSpace(Mention) ? "Passable" : Mention)}"
                        : "Session de rattrapage / Exclu";
                }
                return;
            }

            // Mode 2: Calcul automatique des décisions et mentions selon l'Annexe C
            decimal seuilAdmis = rule?.SeuilAdmission ?? 10.0m;
            decimal seuilTresBien = rule?.SeuilTresBien ?? 16.0m;
            decimal seuilBien = rule?.SeuilBien ?? 14.0m;
            decimal seuilAssezBien = rule?.SeuilAssezBien ?? 12.0m;
            decimal seuilPassable = rule?.SeuilPassable ?? 10.0m;
            decimal seuilRattrapage = 8.0m;

            if (MoyenneGenerale >= seuilAdmis)
            {
                Decision = "Admis";
                Mention = ObtenirMention(MoyenneGenerale, seuilTresBien, seuilBien, seuilAssezBien, seuilPassable);
                Validation = "Oui";
                if (string.IsNullOrWhiteSpace(Observation))
                {
                    Observation = $"Admis avec mention {Mention}";
                }
            }
            else if (MoyenneGenerale >= seuilRattrapage)
            {
                Decision = "Ajourné";
                Mention = "Session de rattrapage";
                Validation = "Non";
                if (string.IsNullOrWhiteSpace(Observation))
                {
                    Observation = "Session de rattrapage";
                }
            }
            else
            {
                Decision = "Exclu";
                Mention = "—";
                Validation = "Non";
                if (string.IsNullOrWhiteSpace(Observation))
                {
                    Observation = "Exclu / Non admis selon règlement";
                }
            }
        }

        /// <summary>
        /// Déterminer la mention selon la moyenne générale et les seuils
        /// </summary>
        private string ObtenirMention(decimal moyenneGenerale, decimal tb = 16m, decimal b = 14m, decimal ab = 12m, decimal p = 10m)
        {
            if (moyenneGenerale >= tb)
                return "Très Bien";
            else if (moyenneGenerale >= b)
                return "Bien";
            else if (moyenneGenerale >= ab)
                return "Assez Bien";
            else if (moyenneGenerale >= p)
                return "Passable";
            else
                return "—";
        }

        public override string ToString()
        {
            return $"{NumeroOrdre}. {NomPrenom} ({Matricule}) - MG: {MoyenneGenerale:0.000} - {Decision} - {Mention}";
        }
    }

    /// <summary>
    /// Classe représentant les grades d'un étudiant pour un module donné
    /// Contient CC (Contrôle Continu), TP (Travaux Pratiques), Examen et le crédit du module
    /// </summary>
    public class ModuleGrades
    {
        public string NomModule { get; set; }  // Nom du module
        public decimal CC { get; set; }         // Note de Contrôle Continu
        public decimal TP { get; set; }         // Note de Travaux Pratiques
        public decimal Examen { get; set; }     // Note d'Examen
        public int Credit { get; set; }         // Crédit du module
        
        public ModuleGrades()
        {
            NomModule = string.Empty;
            CC = 0;
            TP = 0;
            Examen = 0;
            Credit = 0;
        }

        /// <summary>
        /// Calculer la moyenne pour ce module: (CC + TP + Examen) / 3
        /// </summary>
        public decimal CalculerMoyenneModule()
        {
            if (CC == 0 && TP == 0 && Examen == 0)
                return 0;
            
            return (CC + TP + Examen) / 3m;
        }
    }
}
