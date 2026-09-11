using System;
using System.Collections.Generic;
using System.Data;
using DesktopApp.Models;

namespace DesktopApp.Services
{
    /// <summary>
    /// Service de gestion des classes (logique métier)
    /// Propose les méthodes CRUD de base : AjouterClasse(), ListerClasses(), ObtenirClasseParId(), ModifierClasse(), SupprimerClasse().
    /// </summary>
    public class ClasseService
    {
        private readonly DatabaseHelper _dbHelper;

        public ClasseService()
        {
            _dbHelper = new DatabaseHelper();
            InitialiserTable();
        }

        /// <summary>
        /// S'assure que la table 'classe' existe dans la base de données (compatible avec les 2 schémas)
        /// </summary>
        private void InitialiserTable()
        {
            try
            {
                string createTableQuery = @"
                    CREATE TABLE IF NOT EXISTS `classe` (
                        `id` INT AUTO_INCREMENT PRIMARY KEY,
                        `nom` VARCHAR(100) NOT NULL DEFAULT '',
                        `niveau` VARCHAR(50) DEFAULT '',
                        `annee_universitaire` VARCHAR(20) DEFAULT ''
                    ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;";

                _dbHelper.ExecuteNonQuery(createTableQuery);

                // Permettre la coexistence des colonnes nom / nom_classe et id / id_classe
                try { _dbHelper.ExecuteNonQuery("ALTER TABLE `classe` ADD COLUMN `nom_classe` VARCHAR(100) DEFAULT NULL;"); } catch { }
                try { _dbHelper.ExecuteNonQuery("ALTER TABLE `classe` ADD COLUMN `filiere` VARCHAR(100) DEFAULT '';"); } catch { }
                try { _dbHelper.ExecuteNonQuery("ALTER TABLE `classe` ADD COLUMN `description` TEXT;"); } catch { }
                try { _dbHelper.ExecuteNonQuery("ALTER TABLE `classe` ADD COLUMN `date_creation` DATETIME DEFAULT CURRENT_TIMESTAMP;"); } catch { }

                // Synchroniser les valeurs entre 'nom' et 'nom_classe' si l'une des colonnes est renseignée
                try { _dbHelper.ExecuteNonQuery("UPDATE `classe` SET `nom_classe` = `nom` WHERE (`nom_classe` IS NULL OR `nom_classe` = '') AND `nom` IS NOT NULL AND `nom` != '';"); } catch { }
                try { _dbHelper.ExecuteNonQuery("UPDATE `classe` SET `nom` = `nom_classe` WHERE (`nom` IS NULL OR `nom` = '') AND `nom_classe` IS NOT NULL AND `nom_classe` != '';"); } catch { }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ClasseService] Erreur lors de l'initialisation de la table: {ex.Message}");
            }
        }

        /// <summary>
        /// Dernier message d'erreur d'exécution métier
        /// </summary>
        public string DerniereErreur { get; private set; }

        /// <summary>
        /// Ajouter une nouvelle classe dans la base de données (AjouterClasse)
        /// </summary>
        public bool AjouterClasse(Classe classe)
        {
            DerniereErreur = null;
            if (classe == null || string.IsNullOrWhiteSpace(classe.NomClasse))
            {
                DerniereErreur = "Le nom de la classe est obligatoire.";
                return false;
            }

            try
            {
                string nomNettoye = classe.NomClasse.Trim();

                // Vérifier si la classe existe déjà dans la base
                var existante = ObtenirClasseParNom(nomNettoye);
                if (existante != null)
                {
                    DerniereErreur = $"La classe '{nomNettoye}' existe déjà dans la base de données.";
                    return false;
                }

                // Essayer l'insertion compatible avec le schéma phpMyAdmin (`nom`, `niveau`, `annee_universitaire`)
                var valuesNom = new Dictionary<string, object>
                {
                    { "nom", nomNettoye },
                    { "niveau", classe.Niveau ?? "" },
                    { "annee_universitaire", classe.AnneeUniversitaire ?? "" }
                };

                bool succes = _dbHelper.InsertRecord("classe", valuesNom);
                if (succes)
                {
                    // Tenter de maintenir 'nom_classe' synchronisé si la colonne existe
                    try { _dbHelper.ExecuteNonQuery($"UPDATE `classe` SET `nom_classe` = '{nomNettoye.Replace("'", "''")}' WHERE `nom` = '{nomNettoye.Replace("'", "''")}';"); } catch { }
                    return true;
                }

                // Fallback schéma `nom_classe`
                var valuesNomClasse = new Dictionary<string, object>
                {
                    { "nom_classe", nomNettoye },
                    { "niveau", classe.Niveau ?? "" },
                    { "filiere", classe.Filiere ?? "" },
                    { "annee_universitaire", classe.AnneeUniversitaire ?? "" },
                    { "description", classe.Description ?? "" }
                };

                succes = _dbHelper.InsertRecord("classe", valuesNomClasse);
                if (!succes)
                {
                    DerniereErreur = _dbHelper.LastError ?? "Échec de l'insertion dans la base de données MySQL.";
                }
                return succes;
            }
            catch (Exception ex)
            {
                DerniereErreur = ex.Message;
                Console.WriteLine($"[ClasseService] Erreur AjouterClasse: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Obtenir une classe par son nom exact (insensible à la casse, compatible avec tous les schémas)
        /// </summary>
        public Classe ObtenirClasseParNom(string nomClasse)
        {
            if (string.IsNullOrWhiteSpace(nomClasse)) return null;
            try
            {
                string nomEscaped = nomClasse.Trim().Replace("'", "''");
                string query = $"SELECT * FROM `classe` WHERE (LOWER(`nom`) = LOWER('{nomEscaped}') OR LOWER(`nom_classe`) = LOWER('{nomEscaped}')) LIMIT 1";
                DataTable dt = _dbHelper.ExecuteSelectQuery(query);

                if (dt != null && dt.Rows.Count > 0)
                {
                    return ExtraireClasse(dt.Rows[0]);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ClasseService] Erreur ObtenirClasseParNom: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Surcharge pour ajouter une classe directement par ses attributs
        /// </summary>
        public bool AjouterClasse(string nomClasse, string niveau = "", string filiere = "", string anneeUniversitaire = "", string description = "")
        {
            if (string.IsNullOrWhiteSpace(nomClasse))
                return false;

            var classe = new Classe
            {
                NomClasse = nomClasse.Trim(),
                Niveau = niveau,
                Filiere = filiere,
                AnneeUniversitaire = anneeUniversitaire,
                Description = description,
                DateCreation = DateTime.Now
            };

            return AjouterClasse(classe);
        }

        /// <summary>
        /// Lister toutes les classes enregistrées (ListerClasses - compatible multi-schémas)
        /// </summary>
        public List<Classe> ListerClasses()
        {
            var classes = new List<Classe>();

            try
            {
                string query = "SELECT * FROM `classe` ORDER BY 1";
                DataTable dt = _dbHelper.ExecuteSelectQuery(query);

                if (dt != null && dt.Rows.Count > 0)
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        var c = ExtraireClasse(row);
                        if (c != null && !string.IsNullOrWhiteSpace(c.NomClasse))
                        {
                            classes.Add(c);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ClasseService] Erreur ListerClasses: {ex.Message}");
            }

            return classes;
        }

        /// <summary>
        /// Obtenir une classe par son ID
        /// </summary>
        public Classe ObtenirClasseParId(int id)
        {
            try
            {
                string query = $"SELECT * FROM `classe` WHERE `id` = {id} OR `id_classe` = {id} LIMIT 1";
                DataTable dt = _dbHelper.ExecuteSelectQuery(query);

                if (dt != null && dt.Rows.Count > 0)
                {
                    return ExtraireClasse(dt.Rows[0]);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ClasseService] Erreur ObtenirClasseParId: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Extraire un objet Classe depuis un DataRow MySQL (supporte id/id_classe et nom/nom_classe)
        /// </summary>
        private Classe ExtraireClasse(DataRow row)
        {
            if (row == null) return null;

            int id = 0;
            if (row.Table.Columns.Contains("id") && row["id"] != DBNull.Value) id = Convert.ToInt32(row["id"]);
            else if (row.Table.Columns.Contains("id_classe") && row["id_classe"] != DBNull.Value) id = Convert.ToInt32(row["id_classe"]);

            string nom = "";
            if (row.Table.Columns.Contains("nom") && row["nom"] != DBNull.Value && !string.IsNullOrWhiteSpace(row["nom"].ToString())) nom = row["nom"].ToString().Trim();
            else if (row.Table.Columns.Contains("nom_classe") && row["nom_classe"] != DBNull.Value && !string.IsNullOrWhiteSpace(row["nom_classe"].ToString())) nom = row["nom_classe"].ToString().Trim();

            string niveau = row.Table.Columns.Contains("niveau") && row["niveau"] != DBNull.Value ? row["niveau"].ToString() : "";
            string filiere = row.Table.Columns.Contains("filiere") && row["filiere"] != DBNull.Value ? row["filiere"].ToString() : "";
            string anneeUniv = row.Table.Columns.Contains("annee_universitaire") && row["annee_universitaire"] != DBNull.Value ? row["annee_universitaire"].ToString() : "";
            string desc = row.Table.Columns.Contains("description") && row["description"] != DBNull.Value ? row["description"].ToString() : "";
            DateTime dateCrea = row.Table.Columns.Contains("date_creation") && row["date_creation"] != DBNull.Value ? Convert.ToDateTime(row["date_creation"]) : DateTime.Now;

            return new Classe
            {
                Id = id,
                NomClasse = nom,
                Niveau = niveau,
                Filiere = filiere,
                AnneeUniversitaire = anneeUniv,
                Description = desc,
                DateCreation = dateCrea
            };
        }

        /// <summary>
        /// Modifier une classe existante dans la base de données
        /// </summary>
        public bool ModifierClasse(Classe classe)
        {
            if (classe == null || classe.Id <= 0)
                return false;

            try
            {
                string nomNettoye = (classe.NomClasse ?? "").Trim();
                try
                {
                    _dbHelper.ExecuteNonQuery($"UPDATE `classe` SET `nom` = '{nomNettoye.Replace("'", "''")}', `niveau` = '{(classe.Niveau ?? "").Replace("'", "''")}', `annee_universitaire` = '{(classe.AnneeUniversitaire ?? "").Replace("'", "''")}' WHERE `id` = {classe.Id} OR `id_classe` = {classe.Id};");
                }
                catch { }

                try
                {
                    _dbHelper.ExecuteNonQuery($"UPDATE `classe` SET `nom_classe` = '{nomNettoye.Replace("'", "''")}', `filiere` = '{(classe.Filiere ?? "").Replace("'", "''")}', `description` = '{(classe.Description ?? "").Replace("'", "''")}' WHERE `id` = {classe.Id} OR `id_classe` = {classe.Id};");
                }
                catch { }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ClasseService] Erreur ModifierClasse: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Supprimer une classe par son ID
        /// </summary>
        public bool SupprimerClasse(int id)
        {
            try
            {
                return _dbHelper.ExecuteNonQuery($"DELETE FROM `classe` WHERE `id` = {id} OR `id_classe` = {id};") > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ClasseService] Erreur SupprimerClasse: {ex.Message}");
                return false;
            }
        }
    }
}
