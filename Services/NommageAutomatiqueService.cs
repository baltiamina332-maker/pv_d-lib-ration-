using System;
using System.IO;
using System.Text.RegularExpressions;
using DesktopApp.Models;

namespace DesktopApp.Services
{
    /// <summary>
    /// Service pour gérer le nommage automatique des fichiers conformément au CDC Section 5.3
    /// </summary>
    public class NommageAutomatiqueService
    {
        /// <summary>
        /// Générer un nom de fichier PV conforme au CDC
        /// Format: PV_Classe_Date.docx (ex: PV_L3-INFO-A_20260830.docx)
        /// </summary>
        public string GenererNomPV(string classeGroupe, DateTime? dateDeliberation = null)
        {
            try
            {
                // Utiliser la date de délibération ou la date courante
                DateTime date = dateDeliberation ?? DateTime.Now;
                
                // Nettoyer le nom de classe/groupe (enlever caractères spéciaux)
                string classeNettoyee = NettoyerNomClasse(classeGroupe);
                
                // Format CDC: PV_Classe_Date.docx
                string nomFichier = $"PV_{classeNettoyee}_{date:yyyyMMdd}.docx";
                
                return nomFichier;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[NOMMAGE] Erreur génération nom PV: {ex.Message}");
                // Nom de fallback
                return $"PV_{DateTime.Now:yyyyMMdd_HHmmss}.docx";
            }
        }

        /// <summary>
        /// Générer un nom de fichier Export Excel conforme
        /// Format: Export_Classe_Date.xlsx
        /// </summary>
        public string GenererNomExport(string classeGroupe, string typeExport = "Results", DateTime? date = null)
        {
            try
            {
                DateTime dateUtilisee = date ?? DateTime.Now;
                string classeNettoyee = NettoyerNomClasse(classeGroupe);
                
                string nomFichier = $"Export_{typeExport}_{classeNettoyee}_{dateUtilisee:yyyyMMdd}.xlsx";
                
                return nomFichier;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[NOMMAGE] Erreur génération nom export: {ex.Message}");
                return $"Export_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            }
        }

        /// <summary>
        /// Générer un nom de fichier d'archive conforme
        /// </summary>
        public string GenererNomArchive(string classeGroupe, string typeDocument = "PV", DateTime? date = null)
        {
            try
            {
                DateTime dateUtilisee = date ?? DateTime.Now;
                string classeNettoyee = NettoyerNomClasse(classeGroupe);
                
                string extension = typeDocument.ToUpper() == "PV" ? "docx" : "xlsx";
                string nomFichier = $"Archive_{typeDocument}_{classeNettoyee}_{dateUtilisee:yyyyMMdd_HHmmss}.{extension}";
                
                return nomFichier;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[NOMMAGE] Erreur génération nom archive: {ex.Message}");
                return $"Archive_{DateTime.Now:yyyyMMdd_HHmmss}.docx";
            }
        }

        /// <summary>
        /// Nettoyer le nom de classe/groupe pour un nom de fichier valide
        /// </summary>
        private string NettoyerNomClasse(string classeGroupe)
        {
            if (string.IsNullOrWhiteSpace(classeGroupe))
            {
                return "CLASSE_INCONNUE";
            }

            // Enlever les caractères non autorisés dans les noms de fichiers
            string nettoyee = classeGroupe.Trim();
            
            // Remplacer les caractères spéciaux par des tirets
            nettoyee = Regex.Replace(nettoyee, @"[<>:""/\\|?*]", "-");
            
            // Remplacer les espaces par des tirets
            nettoyee = nettoyee.Replace(" ", "-");
            
            // Enlever les tirets multiples
            nettoyee = Regex.Replace(nettoyee, @"-+", "-");
            
            // Enlever les tirets en début/fin
            nettoyee = nettoyee.Trim('-');
            
            // Limiter la longueur pour éviter les noms trop longs
            if (nettoyee.Length > 20)
            {
                nettoyee = nettoyee.Substring(0, 20);
            }
            
            return nettoyee;
        }

        /// <summary>
        /// Vérifier qu'un nom de fichier est unique dans le répertoire
        /// Ajouter un suffixe numérique si nécessaire
        /// </summary>
        public string RendreNomUnique(string cheminComplet)
        {
            try
            {
                if (!File.Exists(cheminComplet))
                {
                    return cheminComplet;
                }

                string repertoire = Path.GetDirectoryName(cheminComplet);
                string nomSansExtension = Path.GetFileNameWithoutExtension(cheminComplet);
                string extension = Path.GetExtension(cheminComplet);

                int compteur = 1;
                string nouveauNom;
                do
                {
                    nouveauNom = Path.Combine(repertoire, $"{nomSansExtension}_{compteur:00}{extension}");
                    compteur++;
                } 
                while (File.Exists(nouveauNom) && compteur <= 99);

                return nouveauNom;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[NOMMAGE] Erreur génération nom unique: {ex.Message}");
                return cheminComplet;
            }
        }

        /// <summary>
        /// Extraire les informations depuis un nom de fichier PV
        /// </summary>
        public InfosFichierPV ExtraireInfosDepuisNom(string nomFichier)
        {
            var infos = new InfosFichierPV();
            
            try
            {
                // Pattern: PV_Classe_Date.docx
                var match = Regex.Match(nomFichier, @"PV_(.+)_(\d{8})\.docx", RegexOptions.IgnoreCase);
                
                if (match.Success)
                {
                    infos.Classe = match.Groups[1].Value.Replace("-", " ");
                    
                    if (DateTime.TryParseExact(match.Groups[2].Value, "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out DateTime date))
                    {
                        infos.DateDeliberation = date;
                    }
                    
                    infos.EstValide = true;
                }
                else
                {
                    infos.EstValide = false;
                    infos.MessageErreur = "Format de nom de fichier non conforme au CDC";
                }
            }
            catch (Exception ex)
            {
                infos.EstValide = false;
                infos.MessageErreur = $"Erreur d'analyse: {ex.Message}";
            }
            
            return infos;
        }
    }

    /// <summary>
    /// Informations extraites d'un nom de fichier PV
    /// </summary>
    public class InfosFichierPV
    {
        public string Classe { get; set; } = "";
        public DateTime? DateDeliberation { get; set; }
        public bool EstValide { get; set; } = false;
        public string MessageErreur { get; set; } = "";
    }
}