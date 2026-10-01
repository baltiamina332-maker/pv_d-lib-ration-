using System;

namespace DesktopApp.Models
{
    /// <summary>
    /// Modèle représentant un enregistrement d'archivage des PV générés
    /// Utilisé pour traçabilité et audit des délibérations
    /// </summary>
    public class ArchiveRecord
    {
        public int Id { get; set; }
        
        // Informations du document
        public string NomFichier { get; set; }
        public string CheminArchive { get; set; }
        public string TypeDocument { get; set; }  // "PV_Word", "Export_Excel", etc.
        
        // Métadonnées de la délibération
        public string ClasseGroupe { get; set; }
        public string AnneeUniversitaire { get; set; }
        public string TypeSession { get; set; }   // "Principale", "Rattrapage"
        public DateTime DateDeliberation { get; set; }
        
        // Statistiques des étudiants
        public int NombreEtudiants { get; set; }
        public int NombreAdmis { get; set; }
        public int NombreAdmisAvecModeration { get; set; }
        public int NombreConseilEcole { get; set; }
        public int NombreDecisionConseil { get; set; }
        public int NombreRedoubleExclu { get; set; }
        
        // Statistiques des moyennes
        public decimal MoyenneGeneraleGlobale { get; set; }
        public decimal MoyenneAdmis { get; set; }
        public decimal MoyenneConseil { get; set; }
        
        // Statistiques des mentions
        public int NombreTresBien { get; set; }
        public int NombreBien { get; set; }
        public int NombreAssezBien { get; set; }
        public int NombrePassable { get; set; }
        
        // Statistiques des ECTS
        public decimal MoyenneEctsValides { get; set; }
        public int NombreEtudiants15EctsOuMoins { get; set; }
        public int NombreEtudiants16A22Ects { get; set; }
        public int NombreEtudiantsPlus22Ects { get; set; }
        
        // Statistiques des rachats
        public int NombreRachatAncien { get; set; }
        public int NombreRachatNouveau { get; set; }
        public int NombreRachatUE { get; set; }
        
        // Informations d'audit
        public DateTime DateArchivage { get; set; }
        public string GenerePar { get; set; }      // Nom d'utilisateur
        public string CommentaireArchive { get; set; }
        public long TailleFichierOctets { get; set; }
        public string HashMD5 { get; set; }        // Pour vérifier intégrité du fichier
        
        // Métriques de performance CDC (nouvelles propriétés)
        public int DureeGeneration { get; set; }        // Durée en millisecondes
        public bool ConformeCDC { get; set; }           // Respecte les seuils CDC
        public string MessagePerformance { get; set; }   // Message de conformité
        
        // Informations de session
        public int IdSession { get; set; }
        public DateTime DateCreation { get; set; }
        public DateTime? DateModification { get; set; }

        /// <summary>
        /// Constructeur par défaut
        /// </summary>
        public ArchiveRecord()
        {
            NomFichier = string.Empty;
            CheminArchive = string.Empty;
            TypeDocument = string.Empty;
            ClasseGroupe = string.Empty;
            AnneeUniversitaire = string.Empty;
            TypeSession = string.Empty;
            GenerePar = string.Empty;
            CommentaireArchive = string.Empty;
            HashMD5 = string.Empty;
            MessagePerformance = string.Empty;  // Nouvelle propriété
            DateArchivage = DateTime.Now;
            DateCreation = DateTime.Now;
        }

        /// <summary>
        /// Calculer le pourcentage d'admis (tous types confondus)
        /// </summary>
        public decimal PourcentageAdmis
        {
            get
            {
                if (NombreEtudiants == 0) return 0;
                return Math.Round((decimal)(NombreAdmis + NombreAdmisAvecModeration) / NombreEtudiants * 100, 2);
            }
        }

        /// <summary>
        /// Calculer le pourcentage d'étudiants nécessitant une décision de conseil
        /// </summary>
        public decimal PourcentageConseil
        {
            get
            {
                if (NombreEtudiants == 0) return 0;
                return Math.Round((decimal)(NombreConseilEcole + NombreDecisionConseil) / NombreEtudiants * 100, 2);
            }
        }

        /// <summary>
        /// Calculer le pourcentage d'étudiants en situation de redoublement/exclusion
        /// </summary>
        public decimal PourcentageRedoubleExclu
        {
            get
            {
                if (NombreEtudiants == 0) return 0;
                return Math.Round((decimal)NombreRedoubleExclu / NombreEtudiants * 100, 2);
            }
        }

        /// <summary>
        /// Calculer le taux de réussite avec rachat
        /// </summary>
        public decimal TauxReussiteAvecRachat
        {
            get
            {
                if (NombreEtudiants == 0) return 0;
                int totalRachat = NombreRachatAncien + NombreRachatNouveau + NombreRachatUE;
                return Math.Round((decimal)totalRachat / NombreEtudiants * 100, 2);
            }
        }

        /// <summary>
        /// Vérifier la cohérence des données statistiques
        /// </summary>
        public bool VerifierCoherence()
        {
            // Vérifier que la somme des décisions égale le nombre total d'étudiants
            int totalDecisions = NombreAdmis + NombreAdmisAvecModeration + NombreConseilEcole + 
                               NombreDecisionConseil + NombreRedoubleExclu;
            
            if (totalDecisions != NombreEtudiants)
                return false;

            // Vérifier que la somme des mentions ne dépasse pas le nombre d'admis
            int totalMentions = NombreTresBien + NombreBien + NombreAssezBien + NombrePassable;
            int totalAdmisCalcule = NombreAdmis; // Seuls les "Admis" directs ont des mentions normales
            
            if (totalMentions > totalAdmisCalcule)
                return false;

            // Vérifier que la somme des catégories ECTS égale le nombre d'étudiants
            int totalEcts = NombreEtudiants15EctsOuMoins + NombreEtudiants16A22Ects + NombreEtudiantsPlus22Ects;
            
            if (totalEcts != NombreEtudiants)
                return false;

            return true;
        }

        /// <summary>
        /// Générer un résumé textuel de l'archive
        /// </summary>
        public string GenererResume()
        {
            return $"Archive PV - {ClasseGroupe} ({AnneeUniversitaire}) - " +
                   $"{NombreEtudiants} étudiants - " +
                   $"{PourcentageAdmis}% admis - " +
                   $"Généré le {DateArchivage:dd/MM/yyyy HH:mm}";
        }

        public override string ToString()
        {
            return $"{NomFichier} - {ClasseGroupe} - {DateArchivage:dd/MM/yyyy}";
        }
    }
}