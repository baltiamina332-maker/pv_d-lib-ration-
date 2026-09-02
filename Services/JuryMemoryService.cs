using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace DesktopApp.Services
{
    /// <summary>
    /// Modèle d'entrée pour un membre de jury enregistré
    /// </summary>
    public class JuryMemberEntry
    {
        public string NomPrenom { get; set; } = "";
        public string Role { get; set; } = ""; // "President", "Secretaire", "Membre"
        public int Frequency { get; set; } = 1;
        public DateTime LastUsed { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// Service d'aide à la saisie et de mémorisation intelligente des membres du jury
    /// </summary>
    public class JuryMemoryService
    {
        private readonly string _filePath;
        private List<JuryMemberEntry> _entries;

        public JuryMemoryService()
        {
            try
            {
                string appDataFolder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "PVDeliberation"
                );
                if (!Directory.Exists(appDataFolder))
                {
                    Directory.CreateDirectory(appDataFolder);
                }
                _filePath = Path.Combine(appDataFolder, "jury_history.json");
            }
            catch
            {
                _filePath = "jury_history.json";
            }

            _entries = LoadHistory();
            EnsureDefaultEntries();
        }

        private List<JuryMemberEntry> LoadHistory()
        {
            try
            {
                if (File.Exists(_filePath))
                {
                    string json = File.ReadAllText(_filePath);
                    var data = JsonConvert.DeserializeObject<List<JuryMemberEntry>>(json);
                    if (data != null) return data;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[JURY MEMORY] Erreur chargement: {ex.Message}");
            }
            return new List<JuryMemberEntry>();
        }

        public void SaveHistory()
        {
            try
            {
                string json = JsonConvert.SerializeObject(_entries, Formatting.Indented);
                File.WriteAllText(_filePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[JURY MEMORY] Erreur sauvegarde: {ex.Message}");
            }
        }

        private void EnsureDefaultEntries()
        {
            if (_entries.Count == 0)
            {
                // Entrées de démonstration / standards fréquents
                AddOrUpdateMember("Prof. Mohamed Ben Ali", "President");
                AddOrUpdateMember("Dr. Amina Balti", "President");
                AddOrUpdateMember("M. Karim Trabelsi", "Secretaire");
                AddOrUpdateMember("Mme. Yasmine Sassi", "Secretaire");
                AddOrUpdateMember("Dr. Ahmed Nouri", "Membre");
                AddOrUpdateMember("Prof. Fatma Gharbi", "Membre");
            }
        }

        /// <summary>
        /// Mémoriser ou mettre à jour un membre de jury après une délibération
        /// </summary>
        public void AddOrUpdateMember(string nomPrenom, string role)
        {
            if (string.IsNullOrWhiteSpace(nomPrenom) || nomPrenom.Trim().Length < 2) return;

            string cleanedName = nomPrenom.Trim();
            var existing = _entries.FirstOrDefault(e => 
                e.NomPrenom.Equals(cleanedName, StringComparison.OrdinalIgnoreCase));

            if (existing != null)
            {
                existing.Frequency++;
                existing.LastUsed = DateTime.Now;
                if (!string.IsNullOrEmpty(role)) existing.Role = role;
            }
            else
            {
                _entries.Add(new JuryMemberEntry
                {
                    NomPrenom = cleanedName,
                    Role = role ?? "Membre",
                    Frequency = 1,
                    LastUsed = DateTime.Now
                });
            }

            SaveHistory();
        }

        /// <summary>
        /// Obtenir des suggestions autocomplétées basées sur ce que l'utilisateur tape
        /// </summary>
        public List<string> GetSuggestions(string query, string role = null, int maxResults = 5)
        {
            if (_entries == null || !_entries.Any()) return new List<string>();

            var filtered = _entries.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(role))
            {
                filtered = filtered.Where(e => string.Equals(e.Role, role, StringComparison.OrdinalIgnoreCase) || e.Role == "Membre");
            }

            if (!string.IsNullOrWhiteSpace(query))
            {
                string q = query.Trim().ToLowerInvariant();
                filtered = filtered.Where(e => e.NomPrenom.ToLowerInvariant().Contains(q));
            }

            return filtered
                .OrderByDescending(e => e.Frequency)
                .ThenByDescending(e => e.LastUsed)
                .Select(e => e.NomPrenom)
                .Distinct()
                .Take(maxResults)
                .ToList();
        }
    }
}
