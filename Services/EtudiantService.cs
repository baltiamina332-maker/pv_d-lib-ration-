using System;
using System.Collections.Generic;
using System.Data;
using DesktopApp.Models;

namespace DesktopApp.Services
{
    /// <summary>
    /// Service de gestion des étudiants (logique métier)
    /// Contient les méthodes CRUD de base : AjouterEtudiant(), ListerEtudiantsParClasse(), ListerEtudiants(), Génération automatique du matricule, etc.
    /// </summary>
    public class EtudiantService
    {
        private readonly DatabaseHelper _dbHelper;

        public EtudiantService()
        {
            _dbHelper = new DatabaseHelper();
            InitialiserTable();
        }

        /// <summary>
        /// S'assure que la table 'etudiant' existe dans la base de données
        /// </summary>
        private void InitialiserTable()
        {
            try
            {
                string createTableQuery = @"
                    CREATE TABLE IF NOT EXISTS `etudiant` (
                        `id_etudiant` INT AUTO_INCREMENT PRIMARY KEY,
                        `num_ordre` INT DEFAULT 1,
                        `nom` VARCHAR(100),
                        `prenom` VARCHAR(100),
                        `nom_prenom` VARCHAR(200),
                        `matricule` VARCHAR(50),
                        `classe_groupe` VARCHAR(50),
                        `filiere` VARCHAR(100) DEFAULT '',
                        `statut` VARCHAR(50) DEFAULT 'Nouveau',
                        `moyenne_generale` DECIMAL(5,3) DEFAULT 0.000,
                        `ects_valides` INT DEFAULT 30,
                        `decision` VARCHAR(100) DEFAULT '',
                        `mention` VARCHAR(100) DEFAULT '',
                        `validation` VARCHAR(50) DEFAULT '',
                        `observation` TEXT,
                        `date_creation` DATETIME DEFAULT CURRENT_TIMESTAMP
                    ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;";

                _dbHelper.ExecuteNonQuery(createTableQuery);

                try { _dbHelper.ExecuteNonQuery("ALTER TABLE `etudiant` ADD COLUMN `num_ordre` INT DEFAULT 1;"); } catch { }
                try { _dbHelper.ExecuteNonQuery("ALTER TABLE `etudiant` ADD COLUMN `nom` VARCHAR(100);"); } catch { }
                try { _dbHelper.ExecuteNonQuery("ALTER TABLE `etudiant` ADD COLUMN `prenom` VARCHAR(100);"); } catch { }
                try { _dbHelper.ExecuteNonQuery("ALTER TABLE `etudiant` ADD COLUMN `nom_prenom` VARCHAR(200);"); } catch { }
                try { _dbHelper.ExecuteNonQuery("ALTER TABLE `etudiant` ADD COLUMN `matricule` VARCHAR(50);"); } catch { }
                try { _dbHelper.ExecuteNonQuery("ALTER TABLE `etudiant` ADD COLUMN `classe_groupe` VARCHAR(50);"); } catch { }
                try { _dbHelper.ExecuteNonQuery("ALTER TABLE `etudiant` ADD COLUMN `filiere` VARCHAR(100) DEFAULT '';"); } catch { }
                try { _dbHelper.ExecuteNonQuery("ALTER TABLE `etudiant` ADD COLUMN `statut` VARCHAR(50) DEFAULT 'Nouveau';"); } catch { }
                try { _dbHelper.ExecuteNonQuery("ALTER TABLE `etudiant` ADD COLUMN `moyenne_generale` DECIMAL(5,3) DEFAULT 0.000;"); } catch { }
                try { _dbHelper.ExecuteNonQuery("ALTER TABLE `etudiant` ADD COLUMN `ects_valides` INT DEFAULT 30;"); } catch { }
                try { _dbHelper.ExecuteNonQuery("ALTER TABLE `etudiant` ADD COLUMN `decision` VARCHAR(100) DEFAULT '';"); } catch { }
                try { _dbHelper.ExecuteNonQuery("ALTER TABLE `etudiant` ADD COLUMN `mention` VARCHAR(100) DEFAULT '';"); } catch { }
                try { _dbHelper.ExecuteNonQuery("ALTER TABLE `etudiant` ADD COLUMN `validation` VARCHAR(50) DEFAULT '';"); } catch { }
                try { _dbHelper.ExecuteNonQuery("ALTER TABLE `etudiant` ADD COLUMN `observation` TEXT;"); } catch { }
                try { _dbHelper.ExecuteNonQuery("ALTER TABLE `etudiant` ADD COLUMN `date_creation` DATETIME DEFAULT CURRENT_TIMESTAMP;"); } catch { }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EtudiantService] Erreur lors de l'initialisation de la table: {ex.Message}");
            }
        }

        /// <summary>
        /// Génère un matricule automatique au format : Année (4 chiffres) + Numéro séquentiel (4 chiffres)
        /// Exemple : 20260001, 20260002, 20260003...
        /// </summary>
        /// <param name="annee">Année de référence (optionnel, par défaut l'année en cours)</param>
        /// <returns>Chaine représentant le matricule généré</returns>
        public string GenererMatriculeAutomatique(string annee = null)
        {
            string prefixeAnnee = string.IsNullOrWhiteSpace(annee) ? DateTime.Now.Year.ToString() : annee.Trim();
            if (prefixeAnnee.Length > 4)
            {
                prefixeAnnee = prefixeAnnee.Substring(0, 4);
            }

            int dernierNumero = 0;

            try
            {
                // Chercher dans la table etudiant (ou etudiants) les matricules commençant par prefixeAnnee
                string query = $"SELECT matricule FROM etudiant WHERE matricule LIKE '{prefixeAnnee}%'";
                DataTable dt = _dbHelper.ExecuteSelectQuery(query);

                if (dt == null || dt.Rows.Count == 0)
                {
                    // Fallback si la table s'appelle 'etudiants'
                    query = $"SELECT matricule FROM etudiants WHERE matricule LIKE '{prefixeAnnee}%'";
                    dt = _dbHelper.ExecuteSelectQuery(query);
                }

                if (dt != null && dt.Rows.Count > 0)
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        string mat = row["matricule"]?.ToString();
                        if (!string.IsNullOrEmpty(mat) && mat.StartsWith(prefixeAnnee))
                        {
                            string reste = mat.Substring(prefixeAnnee.Length);
                            if (int.TryParse(reste, out int numSeq))
                            {
                                if (numSeq > dernierNumero)
                                {
                                    dernierNumero = numSeq;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EtudiantService] Erreur lors de la recherche du dernier matricule: {ex.Message}");
            }

            int nouveauNumero = dernierNumero + 1;
            return $"{prefixeAnnee}{nouveauNumero:D4}";
        }

        /// <summary>
        /// Ajouter un étudiant (AjouterEtudiant).
        /// Génère le matricule automatiquement si celui-ci n'est pas renseigné (ex: année + numéro incrémental).
        /// </summary>
        /// <param name="etudiant">Instance de l'étudiant à ajouter</param>
        /// <returns>True si l'ajout a réussi, sinon False</returns>
        public bool AjouterEtudiant(Etudiant etudiant)
        {
            if (etudiant == null)
                return false;

            try
            {
                // Génération automatique du matricule si vide ou non renseigné (année + numéro incrémental)
                if (string.IsNullOrWhiteSpace(etudiant.Matricule))
                {
                    etudiant.Matricule = GenererMatriculeAutomatique(
                        !string.IsNullOrWhiteSpace(etudiant.AnneeUniversitaire) ? etudiant.AnneeUniversitaire : null
                    );
                }

                // S'assurer que NomPrenom est renseigné
                if (string.IsNullOrWhiteSpace(etudiant.NomPrenom))
                {
                    etudiant.NomPrenom = $"{etudiant.Nom} {etudiant.Prenom}".Trim();
                }

                // Calculer automatiquement la décision si non renseignée
                if (string.IsNullOrWhiteSpace(etudiant.Decision))
                {
                    etudiant.CalculerDecisionEtMention();
                }

                var values = new Dictionary<string, object>
                {
                    { "num_ordre", etudiant.NumeroOrdre > 0 ? etudiant.NumeroOrdre : 1 },
                    { "nom", etudiant.Nom ?? "" },
                    { "prenom", etudiant.Prenom ?? "" },
                    { "nom_prenom", etudiant.NomPrenom ?? "" },
                    { "matricule", etudiant.Matricule },
                    { "classe_groupe", etudiant.ClasseGroupe ?? "" },
                    { "filiere", etudiant.Filiere ?? "" },
                    { "statut", string.IsNullOrWhiteSpace(etudiant.Statut) ? "Nouveau" : etudiant.Statut },
                    { "moyenne_generale", etudiant.MoyenneGenerale },
                    { "ects_valides", etudiant.EctsValides },
                    { "decision", etudiant.Decision ?? "" },
                    { "mention", etudiant.Mention ?? "" },
                    { "validation", etudiant.Validation ?? "" },
                    { "observation", etudiant.Observation ?? "" },
                    { "date_creation", etudiant.DateCreation == default ? DateTime.Now : etudiant.DateCreation }
                };

                return _dbHelper.InsertRecord("etudiant", values);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EtudiantService] Erreur AjouterEtudiant: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Lister les étudiants filtrés par classe (ListerEtudiantsParClasse)
        /// </summary>
        /// <param name="classeGroupe">Nom / Code de la classe</param>
        /// <returns>Liste des étudiants de la classe spécifiée</returns>
        public List<Etudiant> ListerEtudiantsParClasse(string classeGroupe)
        {
            var etudiants = new List<Etudiant>();

            if (string.IsNullOrWhiteSpace(classeGroupe))
                return ListerEtudiants();

            try
            {
                string cleanClasse = classeGroupe.Trim().Replace("'", "''");
                string query = $@"
                    SELECT id_etudiant, num_ordre, nom, prenom, nom_prenom, matricule, 
                           classe_groupe, filiere, statut, moyenne_generale, ects_valides, 
                           decision, mention, validation, observation, date_creation
                    FROM etudiant
                    WHERE LOWER(TRIM(classe_groupe)) = LOWER(TRIM('{cleanClasse}'))
                    ORDER BY num_ordre, nom, prenom";

                DataTable dt = _dbHelper.ExecuteSelectQuery(query);

                // Fallback table 'etudiants' au besoin
                if (dt == null || dt.Rows.Count == 0)
                {
                    query = $@"
                        SELECT id as id_etudiant, numero_ordre as num_ordre, nom, prenom, nom_prenom, matricule, 
                               classe_groupe, filiere, statut, moyenne_generale, ects_valides, 
                               decision, mention, validation, observation, date_creation
                        FROM etudiants
                        WHERE LOWER(TRIM(classe_groupe)) = LOWER(TRIM('{cleanClasse}'))
                        ORDER BY numero_ordre, nom, prenom";

                    dt = _dbHelper.ExecuteSelectQuery(query);
                }

                if (dt != null && dt.Rows.Count > 0)
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        var e = MapRowToEtudiant(row);
                        etudiants.Add(e);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EtudiantService] Erreur ListerEtudiantsParClasse: {ex.Message}");
            }

            return etudiants;
        }

        /// <summary>
        /// Lister tous les étudiants de la base de données (ListerEtudiants)
        /// </summary>
        /// <returns>Liste complète des étudiants</returns>
        public List<Etudiant> ListerEtudiants()
        {
            var etudiants = new List<Etudiant>();

            try
            {
                string query = @"
                    SELECT id_etudiant, num_ordre, nom, prenom, nom_prenom, matricule, 
                           classe_groupe, filiere, statut, moyenne_generale, ects_valides, 
                           decision, mention, validation, observation, date_creation
                    FROM etudiant
                    ORDER BY classe_groupe, num_ordre, nom, prenom";

                DataTable dt = _dbHelper.ExecuteSelectQuery(query);

                if (dt == null || dt.Rows.Count == 0)
                {
                    query = @"
                        SELECT id as id_etudiant, numero_ordre as num_ordre, nom, prenom, nom_prenom, matricule, 
                               classe_groupe, filiere, statut, moyenne_generale, ects_valides, 
                               decision, mention, validation, observation, date_creation
                        FROM etudiants
                        ORDER BY classe_groupe, numero_ordre, nom, prenom";

                    dt = _dbHelper.ExecuteSelectQuery(query);
                }

                if (dt != null && dt.Rows.Count > 0)
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        var e = MapRowToEtudiant(row);
                        etudiants.Add(e);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EtudiantService] Erreur ListerEtudiants: {ex.Message}");
            }

            return etudiants;
        }

        /// <summary>
        /// Modifier les informations d'un étudiant
        /// </summary>
        public bool ModifierEtudiant(Etudiant etudiant)
        {
            if (etudiant == null || etudiant.Id <= 0)
                return false;

            try
            {
                var values = new Dictionary<string, object>
                {
                    { "num_ordre", etudiant.NumeroOrdre },
                    { "nom", etudiant.Nom ?? "" },
                    { "prenom", etudiant.Prenom ?? "" },
                    { "nom_prenom", etudiant.NomPrenom ?? "" },
                    { "matricule", etudiant.Matricule ?? "" },
                    { "classe_groupe", etudiant.ClasseGroupe ?? "" },
                    { "filiere", etudiant.Filiere ?? "" },
                    { "statut", etudiant.Statut ?? "Actif" },
                    { "moyenne_generale", etudiant.MoyenneGenerale },
                    { "ects_valides", etudiant.EctsValides },
                    { "decision", etudiant.Decision ?? "" },
                    { "mention", etudiant.Mention ?? "" },
                    { "validation", etudiant.Validation ?? "" },
                    { "observation", etudiant.Observation ?? "" }
                };

                return _dbHelper.UpdateRecord("etudiant", values, $"id_etudiant = {etudiant.Id}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EtudiantService] Erreur ModifierEtudiant: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Supprimer un étudiant de la base de données
        /// </summary>
        public bool SupprimerEtudiant(int id)
        {
            try
            {
                return _dbHelper.DeleteRecord("etudiant", $"id_etudiant = {id}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EtudiantService] Erreur SupprimerEtudiant: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Helper pour mapper une DataRow MySQL vers un objet Etudiant
        /// </summary>
        private Etudiant MapRowToEtudiant(DataRow row)
        {
            var e = new Etudiant
            {
                Id = row["id_etudiant"] != DBNull.Value ? Convert.ToInt32(row["id_etudiant"]) : 0,
                NumeroOrdre = row.Table.Columns.Contains("num_ordre") && row["num_ordre"] != DBNull.Value ? Convert.ToInt32(row["num_ordre"]) : 1,
                Nom = row.Table.Columns.Contains("nom") ? row["nom"]?.ToString() ?? "" : "",
                Prenom = row.Table.Columns.Contains("prenom") ? row["prenom"]?.ToString() ?? "" : "",
                NomPrenom = row.Table.Columns.Contains("nom_prenom") ? row["nom_prenom"]?.ToString() ?? "" : "",
                Matricule = row.Table.Columns.Contains("matricule") ? row["matricule"]?.ToString() ?? "" : "",
                ClasseGroupe = row.Table.Columns.Contains("classe_groupe") ? row["classe_groupe"]?.ToString() ?? "" : "",
                Filiere = row.Table.Columns.Contains("filiere") ? row["filiere"]?.ToString() ?? "" : "",
                Statut = row.Table.Columns.Contains("statut") ? row["statut"]?.ToString() ?? "Actif" : "Actif",
                MoyenneGenerale = row.Table.Columns.Contains("moyenne_generale") && row["moyenne_generale"] != DBNull.Value ? Convert.ToDecimal(row["moyenne_generale"]) : 0m,
                EctsValides = row.Table.Columns.Contains("ects_valides") && row["ects_valides"] != DBNull.Value ? Convert.ToInt32(row["ects_valides"]) : 30,
                Decision = row.Table.Columns.Contains("decision") ? row["decision"]?.ToString() ?? "" : "",
                Mention = row.Table.Columns.Contains("mention") ? row["mention"]?.ToString() ?? "" : "",
                Validation = row.Table.Columns.Contains("validation") ? row["validation"]?.ToString() ?? "" : "",
                Observation = row.Table.Columns.Contains("observation") ? row["observation"]?.ToString() ?? "" : "",
                DateCreation = row.Table.Columns.Contains("date_creation") && row["date_creation"] != DBNull.Value ? Convert.ToDateTime(row["date_creation"]) : DateTime.Now
            };

            return e;
        }
    }
}
