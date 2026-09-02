using System;
using System.Collections.Generic;
using System.Linq;
using DesktopApp.Models;
using MySql.Data.MySqlClient;
using System.Configuration;

namespace DesktopApp.Services
{
    /// <summary>
    /// Service de gestion des étudiants pour l'application desktop
    /// Gère l'import Excel, la validation des décisions et les CRUD étudiants
    /// </summary>
    public class StudentManagementService
    {
        private readonly DatabaseHelper _dbHelper;
        private readonly ExcelImportService _excelService;
        private readonly HistoriqueService _historiqueService;

        public StudentManagementService()
        {
            _dbHelper = new DatabaseHelper();
            _excelService = new ExcelImportService();
            _historiqueService = new HistoriqueService();
        }

        /// <summary>
        /// Import Excel et parsing des données étudiants
        /// </summary>
        /// <param name="filePath">Chemin vers le fichier Excel</param>
        /// <returns>Résultat d'import avec statistiques</returns>
        public ImportResult ImportEtudiantsFromExcel(string filePath)
        {
            try
            {
                // Utiliser le service Excel existant
                var result = _excelService.ImporterDonneesExcel(filePath);

                if (result == null || !result.Succes)
                {
                    return new ImportResult
                    {
                        Succes = false,
                        MessageErreur = result?.MessageErreur ?? "Erreur inconnue lors de l'import"
                    };
                }

                // Enregistrer dans l'historique
                try
                {
                    _historiqueService?.AddHistorique(new Historique
                    {
                        DateDeliberation = DateTime.Now,
                        Classe = result.Etudiants?.FirstOrDefault()?.ClasseGroupe ?? "Import",
                        Session = DateTime.Now.Year.ToString(),
                        NomFichier = System.IO.Path.GetFileName(filePath),
                        CheminFichier = filePath,
                        NbEtudiants = result.Etudiants?.Count ?? 0,
                        UtilisateurId = 1
                    });
                }
                catch { /* Ignorer erreurs historique */ }

                return result;
            }
            catch (Exception ex)
            {
                return new ImportResult
                {
                    Succes = false,
                    MessageErreur = $"Erreur lors de l'import Excel: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Applique les règles de décision sur une liste d'étudiants
        /// </summary>
        /// <param name="etudiants">Liste des étudiants</param>
        /// <returns>Résultat de validation avec statistiques</returns>
        public ImportResult ApplyDecisionRules(List<Etudiant> etudiants)
        {
            try
            {
                if (etudiants == null || !etudiants.Any())
                {
                    return new ImportResult
                    {
                        Succes = false,
                        MessageErreur = "Aucun étudiant fourni pour validation"
                    };
                }

                // Appliquer les règles de décision à chaque étudiant
                var decisionService = new DecisionCalculatorService();
                var etudiantsTraites = decisionService.TraiterEtudiants(etudiants);

                DeliberationStatistics statistiques = null;
                // Enregistrer dans l'historique
                try
                {
                    statistiques = new StatisticsCalculator().CalculerStatistiques(etudiantsTraites);
                    _historiqueService?.AddHistorique(new Historique
                    {
                        DateDeliberation = DateTime.Now,
                        Classe = "Validation",
                        Session = DateTime.Now.Year.ToString(),
                        NomFichier = "Validation_Auto",
                        NbEtudiants = etudiantsTraites.Count,
                        NbAdmis = statistiques.NombreTotalAdmis,
                        NbAjournes = statistiques.NombreRedoubleExclu,
                        UtilisateurId = 1
                    });
                }
                catch { /* Ignorer erreurs historique */ }

                return new ImportResult
                {
                    Succes = true,
                    MessageSucces = $"Validation terminée pour {etudiantsTraites.Count} étudiants",
                    Etudiants = etudiantsTraites,
                    Statistics = statistiques
                };
            }
            catch (Exception ex)
            {
                return new ImportResult
                {
                    Succes = false,
                    MessageErreur = $"Erreur lors de la validation: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Récupère les étudiants d'une classe depuis la base de données
        /// </summary>
        /// <param name="classe">Code de la classe (optionnel)</param>
        /// <param name="session">ID de session (optionnel)</param>
        /// <returns>Liste des étudiants</returns>
        public List<Etudiant> GetEtudiants(string classe = null, int? session = null)
        {
            var etudiants = new List<Etudiant>();

            try
            {
                // Construire la requête
                var whereConditions = new List<string>();
                if (!string.IsNullOrEmpty(classe))
                {
                    var cleanClasse = classe.Trim().Replace("'", "''");
                    whereConditions.Add($"(LOWER(TRIM(classe_groupe)) = LOWER(TRIM('{cleanClasse}')) OR classe_groupe LIKE '%{cleanClasse}%')");
                }
                if (session.HasValue)
                    whereConditions.Add($"id_session = {session.Value}");

                var whereClause = whereConditions.Any() 
                    ? $"WHERE {string.Join(" AND ", whereConditions)}" 
                    : "";

                var query = $@"
                    SELECT id, numero_ordre, nom, prenom, nom_prenom, matricule, 
                           classe_groupe, moyenne_generale, ects_valides, 
                           est_ancien_etudiant, moyenne_ue, decision, mention, 
                           observation, id_session, annee_universitaire, statut
                    FROM etudiants 
                    {whereClause}
                    ORDER BY numero_ordre";

                var result = _dbHelper.ExecuteSelectQuery(query);
                
                foreach (System.Data.DataRow row in result.Rows)
                {
                    var etudiant = new Etudiant
                    {
                        Id = Convert.ToInt32(row["id"]),
                        NumeroOrdre = Convert.ToInt32(row["numero_ordre"]),
                        Nom = row["nom"]?.ToString() ?? "",
                        Prenom = row["prenom"]?.ToString() ?? "",
                        NomPrenom = row["nom_prenom"]?.ToString() ?? "",
                        Matricule = row["matricule"]?.ToString() ?? "",
                        ClasseGroupe = row["classe_groupe"]?.ToString() ?? "",
                        MoyenneGenerale = Convert.ToDecimal(row["moyenne_generale"]),
                        EctsValides = Convert.ToInt32(row["ects_valides"]),
                        EstAncienEtudiant = Convert.ToBoolean(row["est_ancien_etudiant"]),
                        MoyenneUE = Convert.ToDecimal(row["moyenne_ue"]),
                        Decision = row["decision"]?.ToString() ?? "",
                        Mention = row["mention"]?.ToString() ?? "",
                        Observation = row["observation"]?.ToString() ?? "",
                        IdSession = Convert.ToInt32(row["id_session"]),
                        AnneeUniversitaire = row["annee_universitaire"]?.ToString() ?? "",
                        Statut = row["statut"]?.ToString() ?? ""
                    };
                    
                    etudiant.CalculerDecisionEtMention();
                    etudiants.Add(etudiant);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Erreur lors de la récupération des étudiants: {ex.Message}", ex);
            }

            return etudiants;
        }

        /// <summary>
        /// Met à jour un étudiant dans la base de données
        /// </summary>
        /// <param name="etudiantId">ID de l'étudiant</param>
        /// <param name="nouvelleDecision">Nouvelle décision (optionnel)</param>
        /// <param name="nouvelleMention">Nouvelle mention (optionnel)</param>
        /// <param name="nouvelleObservation">Nouvelle observation (optionnel)</param>
        /// <param name="nouvelleMoyenne">Nouvelle moyenne (optionnel)</param>
        /// <returns>True si succès</returns>
        public bool UpdateEtudiant(int etudiantId, string nouvelleDecision = null, 
            string nouvelleMention = null, string nouvelleObservation = null, decimal? nouvelleMoyenne = null)
        {
            try
            {
                // Construire la requête de mise à jour
                var updateFields = new List<string>();
                
                if (!string.IsNullOrEmpty(nouvelleDecision))
                    updateFields.Add($"decision = '{nouvelleDecision.Replace("'", "''")}'");
                if (!string.IsNullOrEmpty(nouvelleMention))
                    updateFields.Add($"mention = '{nouvelleMention.Replace("'", "''")}'");
                if (!string.IsNullOrEmpty(nouvelleObservation))
                    updateFields.Add($"observation = '{nouvelleObservation.Replace("'", "''")}'");
                if (nouvelleMoyenne.HasValue)
                    updateFields.Add($"moyenne_generale = {nouvelleMoyenne.Value}");
                
                if (!updateFields.Any())
                    return false;
                
                var updateQuery = $@"
                    UPDATE etudiants 
                    SET {string.Join(", ", updateFields)}
                    WHERE id = {etudiantId}";
                
                var rowsAffected = _dbHelper.ExecuteNonQuery(updateQuery);
                
                if (rowsAffected > 0)
                {
                    // Enregistrer dans l'historique
                    try
                    {
                        var modifications = string.Join(", ", updateFields);
                        _historiqueService?.AddHistorique(new Historique
                        {
                            DateDeliberation = DateTime.Now,
                            Classe = "Correction",
                            Session = DateTime.Now.Year.ToString(),
                            NomFichier = $"Correction_Etudiant_{etudiantId}",
                            NbEtudiants = 1,
                            UtilisateurId = 1
                        });
                    }
                    catch { /* Ignorer erreurs historique */ }
                    
                    return true;
                }
                
                return false;
            }
            catch (Exception ex)
            {
                throw new Exception($"Erreur lors de la mise à jour de l'étudiant: {ex.Message}", ex);
            }
        }
    }
}