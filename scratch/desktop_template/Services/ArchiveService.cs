using System;
using System.IO;
using System.Collections.Generic;
using DesktopApp.Models;
using MySql.Data.MySqlClient;

namespace DesktopApp.Services
{
    /// <summary>
    /// Service pour archiver et gérer les PV générés
    /// </summary>
    public class ArchiveService
    {
        private DatabaseConnection _dbConnection;
        private string _archivePath;

        public ArchiveService()
        {
            _dbConnection = new DatabaseConnection();
            // Dossier d'archivage: Documents\PV_Archives\
            _archivePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "PV_Archives"
            );

            // Créer le dossier s'il n'existe pas
            if (!Directory.Exists(_archivePath))
                Directory.CreateDirectory(_archivePath);
        }

        /// <summary>
        /// Archiver un document généré (nouvelle méthode pour DocumentGenerator)
        /// </summary>
        public bool ArchiverDocument(ArchiveRecord record)
        {
            try
            {
                if (record == null)
                {
                    Console.WriteLine("[ARCHIVE] ❌ Erreur: Enregistrement d'archive null");
                    return false;
                }

                // Enregistrer dans la base de données
                return EnregistrerArchiveRecord(record);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ARCHIVE] ❌ Erreur lors de l'archivage: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Obtenir les archives récentes
        /// </summary>
        public List<ArchiveRecord> ObtenirArchivesRecentes(int limite = 10)
        {
            var archives = new List<ArchiveRecord>();
            
            try
            {
                if (_dbConnection.OpenConnection())
                {
                    string query = @"
                        SELECT * FROM archive_records 
                        ORDER BY date_archivage DESC 
                        LIMIT @limite
                    ";

                    MySqlCommand cmd = new MySqlCommand(query, _dbConnection.GetConnection());
                    cmd.Parameters.AddWithValue("@limite", limite);
                    
                    MySqlDataReader reader = cmd.ExecuteReader();
                    
                    while (reader.Read())
                    {
                        var record = new ArchiveRecord
                        {
                            Id = reader.GetInt32("id"),
                            NomFichier = reader.GetString("nom_fichier"),
                            CheminArchive = reader.GetString("chemin_archive"),
                            TypeDocument = reader.GetString("type_document"),
                            ClasseGroupe = reader.GetString("classe_groupe"),
                            AnneeUniversitaire = reader.GetString("annee_universitaire"),
                            DateArchivage = reader.GetDateTime("date_archivage"),
                            GenerePar = reader.GetString("genere_par"),
                            NombreEtudiants = reader.GetInt32("nombre_etudiants"),
                            NombreAdmis = reader.GetInt32("nombre_admis"),
                            NombreConseilEcole = reader.GetInt32("nombre_conseil_ecole"),
                            MoyenneGeneraleGlobale = reader.GetDecimal("moyenne_generale_globale")
                        };
                        archives.Add(record);
                    }
                    
                    reader.Close();
                    _dbConnection.CloseConnection();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ARCHIVE] ❌ Erreur récupération archives: {ex.Message}");
            }
            
            return archives;
        }

        /// <summary>
        /// Enregistrer un ArchiveRecord en base de données
        /// </summary>
        private bool EnregistrerArchiveRecord(ArchiveRecord record)
        {
            try
            {
                if (_dbConnection.OpenConnection())
                {
                    string query = @"
                        INSERT INTO archive_records (
                            nom_fichier, chemin_archive, type_document, classe_groupe, annee_universitaire,
                            date_deliberation, genere_par, date_archivage, nombre_etudiants, nombre_admis,
                            nombre_admis_avec_moderation, nombre_conseil_ecole, nombre_decision_conseil, nombre_redouble_exclu,
                            moyenne_generale_globale, moyenne_admis, moyenne_conseil,
                            nombre_tres_bien, nombre_bien, nombre_assez_bien, nombre_passable,
                            moyenne_ects_valides, nombre_etudiants_15_ects_ou_moins, nombre_etudiants_16a22_ects, nombre_etudiants_plus22_ects,
                            nombre_rachat_ancien, nombre_rachat_nouveau, nombre_rachat_ue,
                            taille_fichier_octets, commentaire_archive
                        ) VALUES (
                            @nomFichier, @cheminArchive, @typeDocument, @classeGroupe, @anneeUniversitaire,
                            @dateDeliberation, @generePar, @dateArchivage, @nombreEtudiants, @nombreAdmis,
                            @nombreAdmisAvecModeration, @nombreConseilEcole, @nombreDecisionConseil, @nombreRedoubleExclu,
                            @moyenneGeneraleGlobale, @moyenneAdmis, @moyenneConseil,
                            @nombreTresBien, @nombreBien, @nombreAssezBien, @nombrePassable,
                            @moyenneEctsValides, @nombreEtudiants15EctsOuMoins, @nombreEtudiants16A22Ects, @nombreEtudiantsPlus22Ects,
                            @nombreRachatAncien, @nombreRachatNouveau, @nombreRachatUE,
                            @tailleFichierOctets, @commentaireArchive
                        )
                    ";

                    MySqlCommand cmd = new MySqlCommand(query, _dbConnection.GetConnection());
                    
                    // Ajouter les paramètres
                    cmd.Parameters.AddWithValue("@nomFichier", record.NomFichier);
                    cmd.Parameters.AddWithValue("@cheminArchive", record.CheminArchive);
                    cmd.Parameters.AddWithValue("@typeDocument", record.TypeDocument);
                    cmd.Parameters.AddWithValue("@classeGroupe", record.ClasseGroupe);
                    cmd.Parameters.AddWithValue("@anneeUniversitaire", record.AnneeUniversitaire);
                    cmd.Parameters.AddWithValue("@dateDeliberation", record.DateDeliberation);
                    cmd.Parameters.AddWithValue("@generePar", record.GenerePar);
                    cmd.Parameters.AddWithValue("@dateArchivage", record.DateArchivage);
                    cmd.Parameters.AddWithValue("@nombreEtudiants", record.NombreEtudiants);
                    cmd.Parameters.AddWithValue("@nombreAdmis", record.NombreAdmis);
                    cmd.Parameters.AddWithValue("@nombreAdmisAvecModeration", record.NombreAdmisAvecModeration);
                    cmd.Parameters.AddWithValue("@nombreConseilEcole", record.NombreConseilEcole);
                    cmd.Parameters.AddWithValue("@nombreDecisionConseil", record.NombreDecisionConseil);
                    cmd.Parameters.AddWithValue("@nombreRedoubleExclu", record.NombreRedoubleExclu);
                    cmd.Parameters.AddWithValue("@moyenneGeneraleGlobale", record.MoyenneGeneraleGlobale);
                    cmd.Parameters.AddWithValue("@moyenneAdmis", record.MoyenneAdmis);
                    cmd.Parameters.AddWithValue("@moyenneConseil", record.MoyenneConseil);
                    cmd.Parameters.AddWithValue("@nombreTresBien", record.NombreTresBien);
                    cmd.Parameters.AddWithValue("@nombreBien", record.NombreBien);
                    cmd.Parameters.AddWithValue("@nombreAssezBien", record.NombreAssezBien);
                    cmd.Parameters.AddWithValue("@nombrePassable", record.NombrePassable);
                    cmd.Parameters.AddWithValue("@moyenneEctsValides", record.MoyenneEctsValides);
                    cmd.Parameters.AddWithValue("@nombreEtudiants15EctsOuMoins", record.NombreEtudiants15EctsOuMoins);
                    cmd.Parameters.AddWithValue("@nombreEtudiants16A22Ects", record.NombreEtudiants16A22Ects);
                    cmd.Parameters.AddWithValue("@nombreEtudiantsPlus22Ects", record.NombreEtudiantsPlus22Ects);
                    cmd.Parameters.AddWithValue("@nombreRachatAncien", record.NombreRachatAncien);
                    cmd.Parameters.AddWithValue("@nombreRachatNouveau", record.NombreRachatNouveau);
                    cmd.Parameters.AddWithValue("@nombreRachatUE", record.NombreRachatUE);
                    cmd.Parameters.AddWithValue("@tailleFichierOctets", record.TailleFichierOctets);
                    cmd.Parameters.AddWithValue("@commentaireArchive", record.CommentaireArchive);

                    cmd.ExecuteNonQuery();
                    _dbConnection.CloseConnection();

                    Console.WriteLine("[ARCHIVE] ✓ Enregistrement d'archive sauvegardé en base de données");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ARCHIVE] ❌ Erreur enregistrement BD: {ex.Message}");
                return false;
            }
            
            return false;
        }

        /// <summary>
        /// Archiver un PV généré
        /// </summary>
        public bool ArchiverPV(string cheminFichier, string classeGroupe, List<Etudiant> etudiants, string session)
        {
            try
            {
                if (!File.Exists(cheminFichier))
                    return false;

                // Créer le nom du fichier archivé
                string dateArchive = DateTime.Now.ToString("yyyy-MM-dd_HHmmss");
                string nomFichierArchive = $"PV_{classeGroupe}_{dateArchive}.docx";
                string cheminArchive = Path.Combine(_archivePath, nomFichierArchive);

                // Copier le fichier
                File.Copy(cheminFichier, cheminArchive, true);

                // Enregistrer dans la base de données
                EnregistrerDansBaseDeDonnees(cheminArchive, classeGroupe, etudiants, session);

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de l'archivage: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Enregistrer le PV dans la base de données
        /// </summary>
        private void EnregistrerDansBaseDeDonnees(string cheminFichier, string classeGroupe, List<Etudiant> etudiants, string session)
        {
            try
            {
                if (_dbConnection.OpenConnection())
                {
                    var admis = etudiants.FindAll(e => e.Decision.Contains("Admis")).Count;
                    var ajournes = etudiants.FindAll(e => e.Decision.Contains("Ajourné") || e.Decision.Contains("Redouble")).Count;

                    string query = $@"
                        INSERT INTO deliberations 
                        (date_deliberation, nb_etudiants, nb_admis, nb_ajournes, fichier_pv, utilisateur_id) 
                        VALUES 
                        (NOW(), {etudiants.Count}, {admis}, {ajournes}, '{cheminFichier}', {(AuthenticationService.CurrentUser?.Id ?? 1)})
                    ";

                    MySqlCommand cmd = new MySqlCommand(query, _dbConnection.GetConnection());
                    cmd.ExecuteNonQuery();
                    _dbConnection.CloseConnection();

                    Console.WriteLine("✅ PV enregistré dans la base de données");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de l'enregistrement en BD: {ex.Message}");
            }
        }

        /// <summary>
        /// Récupérer tous les PV archivés
        /// </summary>
        public List<string> ObtenirPVArchives()
        {
            var fichiers = new List<string>();

            try
            {
                if (Directory.Exists(_archivePath))
                {
                    var files = Directory.GetFiles(_archivePath, "*.docx");
                    fichiers.AddRange(files);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de la récupération des archives: {ex.Message}");
            }

            return fichiers;
        }

        /// <summary>
        /// Ouvrir un PV archivé
        /// </summary>
        public bool OuvrirPVArchive(string cheminFichier)
        {
            try
            {
                if (File.Exists(cheminFichier))
                {
                    System.Diagnostics.Process.Start(cheminFichier);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de l'ouverture du fichier: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Supprimer un PV archivé
        /// </summary>
        public bool SupprimerPVArchive(string cheminFichier)
        {
            try
            {
                if (File.Exists(cheminFichier))
                {
                    File.Delete(cheminFichier);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de la suppression: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Obtenir le chemin d'archivage
        /// </summary>
        public string ObtenirCheminArchivage()
        {
            return _archivePath;
        }

        /// <summary>
        /// Exporter un PV vers un dossier personnalisé
        /// </summary>
        public bool ExporterPV(string cheminSource, string cheminExport)
        {
            try
            {
                if (!File.Exists(cheminSource))
                    return false;

                // Créer le dossier si nécessaire
                if (!Directory.Exists(cheminExport))
                    Directory.CreateDirectory(cheminExport);

                string nomFichier = Path.GetFileName(cheminSource);
                string cheminDestination = Path.Combine(cheminExport, nomFichier);

                File.Copy(cheminSource, cheminDestination, true);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de l'export: {ex.Message}");
                return false;
            }
        }
    }
}
