using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using DesktopApp.Models;

namespace DesktopApp.Services
{
    /// <summary>
    /// Service de gestion des affectations (logique métier)
    /// Gère les opérations CRUD pour la table 'affectation' et 'affectations'
    /// </summary>
    public class AffectationService
    {
        private readonly DatabaseHelper _dbHelper;

        public string DerniereErreur { get; private set; }

        public AffectationService()
        {
            _dbHelper = new DatabaseHelper();
            InitialiserTable();
        }

        /// <summary>
        /// S'assure que les tables 'affectation' et 'affectations' existent dans MySQL avec toutes les colonnes
        /// </summary>
        private void InitialiserTable()
        {
            try
            {
                string createTableQuery = @"
                    CREATE TABLE IF NOT EXISTS `affectation` (
                        `id_affectation` INT AUTO_INCREMENT PRIMARY KEY,
                        `enseignant` VARCHAR(150) NOT NULL DEFAULT '',
                        `matiere` VARCHAR(150) NOT NULL DEFAULT '',
                        `nom_classe` VARCHAR(50) NOT NULL DEFAULT '',
                        `id_classe` INT DEFAULT 0,
                        `id_etudiant` INT DEFAULT 0,
                        `annee_universitaire` VARCHAR(20) DEFAULT '2025-2026',
                        `statut` VARCHAR(50) DEFAULT 'Actif',
                        `date_affectation` DATETIME DEFAULT CURRENT_TIMESTAMP
                    ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;";

                _dbHelper.ExecuteNonQuery(createTableQuery);

                try { _dbHelper.ExecuteNonQuery("ALTER TABLE `affectation` ADD COLUMN `enseignant` VARCHAR(150) NOT NULL DEFAULT '';"); } catch { }
                try { _dbHelper.ExecuteNonQuery("ALTER TABLE `affectation` ADD COLUMN `matiere` VARCHAR(150) NOT NULL DEFAULT '';"); } catch { }
                try { _dbHelper.ExecuteNonQuery("ALTER TABLE `affectation` ADD COLUMN `nom_classe` VARCHAR(50) NOT NULL DEFAULT '';"); } catch { }
                try { _dbHelper.ExecuteNonQuery("ALTER TABLE `affectation` ADD COLUMN `id_classe` INT DEFAULT 0;"); } catch { }
                try { _dbHelper.ExecuteNonQuery("ALTER TABLE `affectation` ADD COLUMN `id_etudiant` INT DEFAULT 0;"); } catch { }
                try { _dbHelper.ExecuteNonQuery("ALTER TABLE `affectation` ADD COLUMN `annee_universitaire` VARCHAR(20) DEFAULT '2025-2026';"); } catch { }
                try { _dbHelper.ExecuteNonQuery("ALTER TABLE `affectation` ADD COLUMN `statut` VARCHAR(50) DEFAULT 'Actif';"); } catch { }
                try { _dbHelper.ExecuteNonQuery("ALTER TABLE `affectation` ADD COLUMN `date_affectation` DATETIME DEFAULT CURRENT_TIMESTAMP;"); } catch { }

                // Créer également la table 'affectations' si besoin
                string createTablePluralQuery = @"
                    CREATE TABLE IF NOT EXISTS `affectations` (
                        `id` INT AUTO_INCREMENT PRIMARY KEY,
                        `enseignant` VARCHAR(150) NOT NULL DEFAULT '',
                        `matiere` VARCHAR(150) NOT NULL DEFAULT '',
                        `nom_classe` VARCHAR(50) NOT NULL DEFAULT '',
                        `id_classe` INT DEFAULT 0,
                        `id_etudiant` INT DEFAULT 0,
                        `annee_universitaire` VARCHAR(20) DEFAULT '2025-2026',
                        `statut` VARCHAR(50) DEFAULT 'Actif',
                        `date_affectation` DATETIME DEFAULT CURRENT_TIMESTAMP
                    ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;";

                _dbHelper.ExecuteNonQuery(createTablePluralQuery);
                try { _dbHelper.ExecuteNonQuery("ALTER TABLE `affectations` ADD COLUMN `enseignant` VARCHAR(150) NOT NULL DEFAULT '';"); } catch { }
                try { _dbHelper.ExecuteNonQuery("ALTER TABLE `affectations` ADD COLUMN `matiere` VARCHAR(150) NOT NULL DEFAULT '';"); } catch { }
                try { _dbHelper.ExecuteNonQuery("ALTER TABLE `affectations` ADD COLUMN `nom_classe` VARCHAR(50) NOT NULL DEFAULT '';"); } catch { }
                try { _dbHelper.ExecuteNonQuery("ALTER TABLE `affectations` ADD COLUMN `id_classe` INT DEFAULT 0;"); } catch { }
                try { _dbHelper.ExecuteNonQuery("ALTER TABLE `affectations` ADD COLUMN `id_etudiant` INT DEFAULT 0;"); } catch { }
                try { _dbHelper.ExecuteNonQuery("ALTER TABLE `affectations` ADD COLUMN `annee_universitaire` VARCHAR(20) DEFAULT '2025-2026';"); } catch { }
                try { _dbHelper.ExecuteNonQuery("ALTER TABLE `affectations` ADD COLUMN `statut` VARCHAR(50) DEFAULT 'Actif';"); } catch { }
                try { _dbHelper.ExecuteNonQuery("ALTER TABLE `affectations` ADD COLUMN `date_affectation` DATETIME DEFAULT CURRENT_TIMESTAMP;"); } catch { }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AffectationService] Erreur lors de l'initialisation de la table: {ex.Message}");
            }
        }

        /// <summary>
        /// Ajouter une nouvelle affectation dans la base de données (AjouterAffectation)
        /// </summary>
        /// <param name="affectation">Instance de l'affectation à créer</param>
        /// <returns>True si l'insertion a réussi, sinon False</returns>
        public bool AjouterAffectation(Affectation affectation)
        {
            DerniereErreur = null;
            if (affectation == null)
            {
                DerniereErreur = "L'affectation ne peut pas être vide.";
                return false;
            }

            try
            {
                var values = new Dictionary<string, object>
                {
                    { "enseignant", affectation.Enseignant ?? "" },
                    { "matiere", affectation.Matiere ?? "" },
                    { "nom_classe", affectation.NomClasse ?? "" },
                    { "id_classe", affectation.IdClasse },
                    { "id_etudiant", affectation.IdEtudiant },
                    { "annee_universitaire", string.IsNullOrWhiteSpace(affectation.AnneeUniversitaire) ? "2025-2026" : affectation.AnneeUniversitaire },
                    { "statut", string.IsNullOrWhiteSpace(affectation.Statut) ? "Actif" : affectation.Statut },
                    { "date_affectation", affectation.DateAffectation == default ? DateTime.Now : affectation.DateAffectation }
                };

                bool succes = _dbHelper.InsertRecord("affectation", values);
                if (!succes)
                {
                    succes = _dbHelper.InsertRecord("affectations", values);
                }

                if (!succes)
                {
                    // Tentative d'insertion avec les champs minimaux obligatoires
                    var valuesMinimaux = new Dictionary<string, object>
                    {
                        { "enseignant", affectation.Enseignant ?? "" },
                        { "matiere", affectation.Matiere ?? "" },
                        { "nom_classe", affectation.NomClasse ?? "" }
                    };

                    succes = _dbHelper.InsertRecord("affectation", valuesMinimaux);
                    if (!succes)
                    {
                        succes = _dbHelper.InsertRecord("affectations", valuesMinimaux);
                    }
                }

                if (!succes)
                {
                    DerniereErreur = _dbHelper.LastError ?? "Impossible d'enregistrer l'affectation dans la base de données.";
                }

                return succes;
            }
            catch (Exception ex)
            {
                DerniereErreur = ex.Message;
                Console.WriteLine($"[AffectationService] Erreur AjouterAffectation: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Lister toutes les affectations enregistrées (ListerAffectations)
        /// </summary>
        /// <returns>Liste de toutes les affectations</returns>
        public List<Affectation> ListerAffectations()
        {
            var list = new List<Affectation>();
            DerniereErreur = null;

            try
            {
                string query = @"
                    SELECT id_affectation, enseignant, matiere, nom_classe, id_classe, id_etudiant, 
                           annee_universitaire, statut, date_affectation 
                    FROM affectation 
                    ORDER BY date_affectation DESC, nom_classe, enseignant";

                DataTable dt = _dbHelper.ExecuteSelectQuery(query);

                // Fallback si la table s'appelle 'affectations' ou si 'id_affectation' n'existe pas
                if (dt == null || dt.Rows.Count == 0)
                {
                    query = @"
                        SELECT id as id_affectation, enseignant, matiere, nom_classe, id_classe, id_etudiant, 
                               annee_universitaire, statut, date_affectation 
                        FROM affectations 
                        ORDER BY date_affectation DESC, nom_classe, enseignant";

                    dt = _dbHelper.ExecuteSelectQuery(query);
                }

                if (dt != null && dt.Rows.Count > 0)
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        var aff = MapRowToAffectation(row);
                        list.Add(aff);
                    }
                }
            }
            catch (Exception ex)
            {
                DerniereErreur = ex.Message;
                Console.WriteLine($"[AffectationService] Erreur ListerAffectations: {ex.Message}");
            }

            return list;
        }

        /// <summary>
        /// Lister les affectations par classe
        /// </summary>
        public List<Affectation> ListerAffectationsParClasse(string nomClasse)
        {
            var list = new List<Affectation>();

            if (string.IsNullOrWhiteSpace(nomClasse))
                return ListerAffectations();

            try
            {
                string cleanClasse = nomClasse.Trim().Replace("'", "''");
                string query = $@"
                    SELECT id_affectation, enseignant, matiere, nom_classe, id_classe, id_etudiant, 
                           annee_universitaire, statut, date_affectation 
                    FROM affectation 
                    WHERE LOWER(TRIM(nom_classe)) = LOWER(TRIM('{cleanClasse}'))
                    ORDER BY date_affectation DESC";

                DataTable dt = _dbHelper.ExecuteSelectQuery(query);

                if (dt == null || dt.Rows.Count == 0)
                {
                    query = $@"
                        SELECT id as id_affectation, enseignant, matiere, nom_classe, id_classe, id_etudiant, 
                               annee_universitaire, statut, date_affectation 
                        FROM affectations 
                        WHERE LOWER(TRIM(nom_classe)) = LOWER(TRIM('{cleanClasse}'))
                        ORDER BY date_affectation DESC";

                    dt = _dbHelper.ExecuteSelectQuery(query);
                }

                if (dt != null && dt.Rows.Count > 0)
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        var aff = MapRowToAffectation(row);
                        list.Add(aff);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AffectationService] Erreur ListerAffectationsParClasse: {ex.Message}");
            }

            return list;
        }

        /// <summary>
        /// Lister les noms de classes uniques affectées à un enseignant spécifique (EF-05)
        /// </summary>
        public List<string> ListerClassesPourEnseignant(string enseignant)
        {
            var classes = new List<string>();
            if (string.IsNullOrWhiteSpace(enseignant)) return classes;

            try
            {
                string cleanEnseignant = enseignant.Trim().Replace("'", "''");
                string query = $@"
                    SELECT DISTINCT nom_classe 
                    FROM affectation 
                    WHERE LOWER(TRIM(enseignant)) LIKE LOWER(TRIM('%{cleanEnseignant}%')) 
                       OR LOWER(TRIM(enseignant)) = LOWER(TRIM('{cleanEnseignant}'))";

                DataTable dt = _dbHelper.ExecuteSelectQuery(query);

                if (dt == null || dt.Rows.Count == 0)
                {
                    query = $@"
                        SELECT DISTINCT nom_classe 
                        FROM affectations 
                        WHERE LOWER(TRIM(enseignant)) LIKE LOWER(TRIM('%{cleanEnseignant}%')) 
                           OR LOWER(TRIM(enseignant)) = LOWER(TRIM('{cleanEnseignant}'))";

                    dt = _dbHelper.ExecuteSelectQuery(query);
                }

                if (dt != null && dt.Rows.Count > 0)
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        string c = row["nom_classe"]?.ToString()?.Trim();
                        if (!string.IsNullOrWhiteSpace(c) && !classes.Any(x => string.Equals(x, c, StringComparison.OrdinalIgnoreCase)))
                        {
                            classes.Add(c);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AffectationService] Erreur ListerClassesPourEnseignant: {ex.Message}");
            }

            return classes;
        }

        /// <summary>
        /// Vérifier si un enseignant est affecté à une classe donnée (EF-05)
        /// </summary>
        public bool EstEnseignantAffecteAClasse(string enseignant, string nomClasse)
        {
            if (string.IsNullOrWhiteSpace(enseignant) || string.IsNullOrWhiteSpace(nomClasse))
                return true;

            var classesAffectees = ListerClassesPourEnseignant(enseignant);
            if (classesAffectees.Count == 0) return true; // Si aucune restriction explicite enregistrée, autoriser

            return classesAffectees.Any(c => string.Equals(c, nomClasse.Trim(), StringComparison.OrdinalIgnoreCase) 
                                          || nomClasse.Trim().IndexOf(c, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        /// <summary>
        /// Modifier une affectation existante
        /// </summary>
        public bool ModifierAffectation(Affectation affectation)
        {
            DerniereErreur = null;
            if (affectation == null || affectation.Id <= 0)
            {
                DerniereErreur = "Affectation invalide pour la modification.";
                return false;
            }

            try
            {
                var values = new Dictionary<string, object>
                {
                    { "enseignant", affectation.Enseignant ?? "" },
                    { "matiere", affectation.Matiere ?? "" },
                    { "nom_classe", affectation.NomClasse ?? "" },
                    { "id_classe", affectation.IdClasse },
                    { "id_etudiant", affectation.IdEtudiant },
                    { "annee_universitaire", affectation.AnneeUniversitaire ?? "2025-2026" },
                    { "statut", affectation.Statut ?? "Actif" }
                };

                bool succes = _dbHelper.UpdateRecord("affectation", values, $"id_affectation = {affectation.Id}");
                if (!succes)
                {
                    succes = _dbHelper.UpdateRecord("affectation", values, $"id = {affectation.Id}");
                }
                if (!succes)
                {
                    succes = _dbHelper.UpdateRecord("affectations", values, $"id = {affectation.Id}");
                }

                if (!succes)
                {
                    DerniereErreur = _dbHelper.LastError ?? "Échec de la mise à jour de l'affectation.";
                }

                return succes;
            }
            catch (Exception ex)
            {
                DerniereErreur = ex.Message;
                Console.WriteLine($"[AffectationService] Erreur ModifierAffectation: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Supprimer une affectation par son ID
        /// </summary>
        public bool SupprimerAffectation(int id)
        {
            DerniereErreur = null;
            try
            {
                bool succes = _dbHelper.DeleteRecord("affectation", $"id_affectation = {id}");
                if (!succes)
                {
                    succes = _dbHelper.DeleteRecord("affectation", $"id = {id}");
                }
                if (!succes)
                {
                    succes = _dbHelper.DeleteRecord("affectations", $"id = {id}");
                }

                if (!succes)
                {
                    DerniereErreur = _dbHelper.LastError ?? "Échec de la suppression de l'affectation.";
                }

                return succes;
            }
            catch (Exception ex)
            {
                DerniereErreur = ex.Message;
                Console.WriteLine($"[AffectationService] Erreur SupprimerAffectation: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Helper pour mapper une DataRow vers un objet Affectation
        /// </summary>
        private Affectation MapRowToAffectation(DataRow row)
        {
            int id = 0;
            if (row.Table.Columns.Contains("id_affectation") && row["id_affectation"] != DBNull.Value)
                id = Convert.ToInt32(row["id_affectation"]);
            else if (row.Table.Columns.Contains("id") && row["id"] != DBNull.Value)
                id = Convert.ToInt32(row["id"]);

            return new Affectation
            {
                Id = id,
                Enseignant = row.Table.Columns.Contains("enseignant") ? row["enseignant"]?.ToString() ?? "" : "",
                Matiere = row.Table.Columns.Contains("matiere") ? row["matiere"]?.ToString() ?? "" : "",
                NomClasse = row.Table.Columns.Contains("nom_classe") ? row["nom_classe"]?.ToString() ?? "" : "",
                IdClasse = row.Table.Columns.Contains("id_classe") && row["id_classe"] != DBNull.Value ? Convert.ToInt32(row["id_classe"]) : 0,
                IdEtudiant = row.Table.Columns.Contains("id_etudiant") && row["id_etudiant"] != DBNull.Value ? Convert.ToInt32(row["id_etudiant"]) : 0,
                AnneeUniversitaire = row.Table.Columns.Contains("annee_universitaire") ? row["annee_universitaire"]?.ToString() ?? "2025-2026" : "2025-2026",
                Statut = row.Table.Columns.Contains("statut") ? row["statut"]?.ToString() ?? "Actif" : "Actif",
                DateAffectation = row.Table.Columns.Contains("date_affectation") && row["date_affectation"] != DBNull.Value ? Convert.ToDateTime(row["date_affectation"]) : DateTime.Now
            };
        }
    }
}
