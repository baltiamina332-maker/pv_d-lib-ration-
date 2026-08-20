using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DesktopApp.Models;

namespace DesktopApp.Services
{
    /// <summary>
    /// Service central pour générer tous types de documents (Word PV, Excel Export, Archive)
    /// Unifie la logique de génération et d'archivage
    /// </summary>
    public class DocumentGenerator
    {
        private readonly WordGenerationService _wordService;
        private readonly ExcelExportService _excelService;
        private readonly ArchiveService _archiveService;
        private readonly StatisticsCalculator _statsCalculator;

        public DocumentGenerator()
        {
            _wordService = new WordGenerationService();
            _excelService = new ExcelExportService();
            _archiveService = new ArchiveService();
            _statsCalculator = new StatisticsCalculator();
        }

        /// <summary>
        /// Générer un PV Word complet avec archivage automatique
        /// </summary>
        public DocumentGenerationResult GenererPVWord(List<Etudiant> etudiants, string classeGroupe, string anneeUniversitaire)
        {
            var result = new DocumentGenerationResult();

            try
            {
                if (etudiants == null || etudiants.Count == 0)
                {
                    result.Succes = false;
                    result.MessageErreur = "Aucun étudiant à traiter pour le PV";
                    return result;
                }

                // 1. Créer le dossier d'archive si nécessaire
                string cheminArchive = CreerDossierArchive();
                
                // 2. Générer le nom de fichier avec timestamp
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HHmmss");
                string nomFichier = $"PV_{classeGroupe}_{timestamp}.docx";
                string cheminComplet = Path.Combine(cheminArchive, nomFichier);

                // 3. Calculer les statistiques
                var stats = _statsCalculator.CalculerStatistiques(etudiants);

                // 4. Générer le document Word
                bool successWord = _wordService.GenererPV(etudiants, cheminArchive, nomFichier, classeGroupe);
                
                if (!successWord)
                {
                    result.Succes = false;
                    result.MessageErreur = "Erreur lors de la génération du document Word";
                    return result;
                }

                // 5. Créer l'enregistrement d'archive
                var archiveRecord = CreerEnregistrementArchive(etudiants, cheminComplet, nomFichier, "PV_Word", classeGroupe, anneeUniversitaire, stats);

                // 6. Enregistrer dans la base de données d'archive
                bool successArchive = _archiveService.ArchiverDocument(archiveRecord);

                // 7. Préparer le résultat
                result.Succes = true;
                result.CheminFichier = cheminComplet;
                result.NomFichier = nomFichier;
                result.TypeDocument = "PV_Word";
                result.Statistiques = stats;
                result.MessageSucces = $"PV Word généré avec succès: {nomFichier}";
                
                if (!successArchive)
                {
                    result.Avertissements.Add("Document généré mais erreur lors de l'archivage en base de données");
                }

                Console.WriteLine($"[DOC] ✓ PV Word généré: {cheminComplet}");
                return result;
            }
            catch (Exception ex)
            {
                result.Succes = false;
                result.MessageErreur = $"Erreur lors de la génération du PV Word: {ex.Message}";
                Console.WriteLine($"[DOC] ❌ Erreur génération PV: {ex.Message}");
                return result;
            }
        }

        /// <summary>
        /// Générer un export Excel avec décisions et archivage
        /// </summary>
        public DocumentGenerationResult GenererExcelExport(List<Etudiant> etudiants, string classeGroupe, string anneeUniversitaire)
        {
            var result = new DocumentGenerationResult();

            try
            {
                if (etudiants == null || etudiants.Count == 0)
                {
                    result.Succes = false;
                    result.MessageErreur = "Aucun étudiant à exporter";
                    return result;
                }

                // 1. Créer le dossier d'archive
                string cheminArchive = CreerDossierArchive();

                // 2. Générer le nom de fichier
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HHmmss");
                string nomFichier = $"Export_Results_{classeGroupe}_{timestamp}.xlsx";
                string cheminComplet = Path.Combine(cheminArchive, nomFichier);

                // 3. Exporter vers Excel
                bool successExcel = _excelService.ExporterVersExcel(etudiants, cheminComplet);

                if (!successExcel)
                {
                    result.Succes = false;
                    result.MessageErreur = "Erreur lors de l'export Excel";
                    return result;
                }

                // 4. Calculer les statistiques et archiver
                var stats = _statsCalculator.CalculerStatistiques(etudiants);
                var archiveRecord = CreerEnregistrementArchive(etudiants, cheminComplet, nomFichier, "Export_Excel", classeGroupe, anneeUniversitaire, stats);
                _archiveService.ArchiverDocument(archiveRecord);

                // 5. Préparer le résultat
                result.Succes = true;
                result.CheminFichier = cheminComplet;
                result.NomFichier = nomFichier;
                result.TypeDocument = "Export_Excel";
                result.Statistiques = stats;
                result.MessageSucces = $"Export Excel généré avec succès: {nomFichier}";

                Console.WriteLine($"[DOC] ✓ Export Excel généré: {cheminComplet}");
                return result;
            }
            catch (Exception ex)
            {
                result.Succes = false;
                result.MessageErreur = $"Erreur lors de l'export Excel: {ex.Message}";
                Console.WriteLine($"[DOC] ❌ Erreur export Excel: {ex.Message}");
                return result;
            }
        }

        /// <summary>
        /// Créer le dossier d'archive avec structure par année/mois
        /// </summary>
        private string CreerDossierArchive()
        {
            string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            DateTime now = DateTime.Now;
            
            string cheminArchive = Path.Combine(documentsPath, "PV_Archives", now.Year.ToString(), now.Month.ToString("00"));
            
            if (!Directory.Exists(cheminArchive))
            {
                Directory.CreateDirectory(cheminArchive);
                Console.WriteLine($"[DOC] Dossier d'archive créé: {cheminArchive}");
            }

            return cheminArchive;
        }

        /// <summary>
        /// Créer un enregistrement d'archive complet
        /// </summary>
        private ArchiveRecord CreerEnregistrementArchive(List<Etudiant> etudiants, string cheminFichier, string nomFichier, string typeDocument, string classeGroupe, string anneeUniversitaire, DeliberationStatistics stats)
        {
            var record = new ArchiveRecord
            {
                NomFichier = nomFichier,
                CheminArchive = cheminFichier,
                TypeDocument = typeDocument,
                ClasseGroupe = classeGroupe,
                AnneeUniversitaire = anneeUniversitaire,
                DateDeliberation = DateTime.Now,
                GenerePar = Environment.UserName,
                DateArchivage = DateTime.Now,

                // Statistiques des étudiants
                NombreEtudiants = etudiants.Count,
                NombreAdmis = stats.NombreAdmis,
                NombreAdmisAvecModeration = stats.NombreAdmisAvecModeration,
                NombreConseilEcole = stats.NombreConseilEcole,
                NombreDecisionConseil = stats.NombreDecisionConseil,
                NombreRedoubleExclu = stats.NombreRedoubleExclu,

                // Statistiques des moyennes
                MoyenneGeneraleGlobale = stats.MoyenneGeneraleGlobale,
                MoyenneAdmis = stats.MoyenneAdmis,
                MoyenneConseil = stats.MoyenneConseil,

                // Statistiques des mentions
                NombreTresBien = stats.NombreTresBien,
                NombreBien = stats.NombreBien,
                NombreAssezBien = stats.NombreAssezBien,
                NombrePassable = stats.NombrePassable,

                // Statistiques des ECTS
                MoyenneEctsValides = stats.MoyenneEcts,
                NombreEtudiants15EctsOuMoins = stats.NombreEcts0A15,
                NombreEtudiants16A22Ects = stats.NombreEcts16A22,
                NombreEtudiantsPlus22Ects = stats.NombreEctsPlus22,

                // Statistiques des rachats
                NombreRachatAncien = stats.NombreRachatAncien,
                NombreRachatNouveau = stats.NombreRachatNouveau,
                NombreRachatUE = stats.NombreRachatUE,

                // Métadonnées du fichier
                TailleFichierOctets = File.Exists(cheminFichier) ? new FileInfo(cheminFichier).Length : 0,
                CommentaireArchive = $"Délibération {classeGroupe} - {stats.NombreTotal} étudiants - {stats.PourcentageAdmis}% admis"
            };

            return record;
        }

        /// <summary>
        /// Ouvrir le dossier d'archive dans l'explorateur
        /// </summary>
        public bool OuvrirDossierArchive()
        {
            try
            {
                string cheminArchive = CreerDossierArchive();
                System.Diagnostics.Process.Start("explorer.exe", cheminArchive);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DOC] ❌ Erreur ouverture dossier: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Obtenir la liste des archives récentes
        /// </summary>
        public List<ArchiveRecord> ObtenirArchivesRecentes(int nombreMax = 10)
        {
            try
            {
                return _archiveService.ObtenirArchivesRecentes(nombreMax);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DOC] ❌ Erreur récupération archives: {ex.Message}");
                return new List<ArchiveRecord>();
            }
        }

        /// <summary>
        /// Générer un rapport de statistiques en texte
        /// </summary>
        public string GenererRapportStatistiques(List<Etudiant> etudiants)
        {
            var stats = _statsCalculator.CalculerStatistiques(etudiants);
            return _statsCalculator.GenererResume(stats);
        }
    }

    /// <summary>
    /// Résultat d'une opération de génération de document
    /// </summary>
    public class DocumentGenerationResult
    {
        public bool Succes { get; set; }
        public string CheminFichier { get; set; }
        public string NomFichier { get; set; }
        public string TypeDocument { get; set; }
        public string MessageSucces { get; set; }
        public string MessageErreur { get; set; }
        public List<string> Avertissements { get; set; }
        public DeliberationStatistics Statistiques { get; set; }

        public DocumentGenerationResult()
        {
            CheminFichier = string.Empty;
            NomFichier = string.Empty;
            TypeDocument = string.Empty;
            MessageSucces = string.Empty;
            MessageErreur = string.Empty;
            Avertissements = new List<string>();
        }
    }
}