using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using DesktopApp.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DesktopApp.Services
{
    public enum AiActionType
    {
        None,
        GeneratePVWord,
        ExportExcel,
        FilterClass,
        ClearFilter,
        SwitchTab,
        RefreshData
    }

    public class AiAction
    {
        public AiActionType Type { get; set; } = AiActionType.None;
        public string TargetParameter { get; set; } = "";
        public int TargetIndex { get; set; } = -1;
    }

    public class AiAssistantResponse
    {
        public string ResponseText { get; set; } = "";
        public string Title { get; set; } = "";
        public List<string> StatHighlights { get; set; } = new List<string>();
        public AiAction Action { get; set; } = new AiAction();
        public List<string> SuggestedFollowUps { get; set; } = new List<string>();
    }

    /// <summary>
    /// Service d'Assistant IA / Chatbot conversationnel pour l'application de délibération
    /// Supporte Python + Anthropic Claude API et fallback local intelligent.
    /// </summary>
    public class AiAssistantService
    {
        private readonly StatisticsCalculator _statsCalculator;
        private static readonly HttpClient _httpClient = new HttpClient();

        public AiAssistantService()
        {
            _statsCalculator = new StatisticsCalculator();
        }

        /// <summary>
        /// Traiter une question ou commande en langage naturel de l'utilisateur
        /// </summary>
        public AiAssistantResponse ProcessPrompt(string prompt, List<Etudiant> etudiantsActuels, string currentClassFilter = null)
        {
            if (string.IsNullOrWhiteSpace(prompt))
            {
                var emptyResponse = new AiAssistantResponse();
                emptyResponse.ResponseText = "Bonjour ! Je suis votre Assistant IA. Posez-moi une question comme :\n• \"Combien d'étudiants ont eu une mention Bien ce semestre ?\"\n• \"Génère-moi le PV de la classe 3A40\"\n• \"Quel est le taux de réussite ?\"";
                emptyResponse.SuggestedFollowUps.Add("Combien d'étudiants ont une mention Bien ?");
                emptyResponse.SuggestedFollowUps.Add("Génère-moi le PV de la classe 3A40");
                emptyResponse.SuggestedFollowUps.Add("Quel est le taux de réussite ?");
                return emptyResponse;
            }

            // 1. Tenter d'utiliser le chatbot Python avec Anthropic Claude si disponible
            if (IsPythonChatbotAvailable())
            {
                try
                {
                    var pythonResponse = CallPythonChatbot(prompt, etudiantsActuels);
                    if (pythonResponse.Success)
                    {
                        return ConvertPythonResponse(pythonResponse);
                    }
                    Console.WriteLine($"[AI ASSISTANT] Python chatbot error, falling back to API/local processing");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[AI ASSISTANT] Python Chatbot Error: {ex.Message}");
                }
            }

            // 2. Tenter une connexion directe avec l'API Anthropic Claude si la clé ANTHROPIC_API_KEY est configurée
            string apiKey = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");
            if (!string.IsNullOrWhiteSpace(apiKey))
            {
                try
                {
                    var claudeResponse = CallAnthropicClaudeApi(apiKey, prompt, etudiantsActuels, currentClassFilter);
                    if (claudeResponse != null)
                    {
                        return claudeResponse;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[AI ASSISTANT] Anthropic API Error: {ex.Message}");
                }
            }

            // 3. Moteur local intelligent (Fallback ultrarapide)
            return ProcessPromptLocal(prompt, etudiantsActuels, currentClassFilter);
        }

        /// <summary>
        /// Vérifie si le chatbot Python est disponible
        /// </summary>
        private bool IsPythonChatbotAvailable()
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "python",
                    Arguments = "--version",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };

                using (var process = System.Diagnostics.Process.Start(startInfo))
                {
                    if (process == null) return false;
                    process.WaitForExit(5000);
                    
                    // Vérifier si le script existe
                    string scriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "chatbot_deliberation.py");
                    return File.Exists(scriptPath);
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Appelle le chatbot Python et retourne la réponse
        /// </summary>
        private PythonChatbotResult CallPythonChatbot(string message, List<Etudiant> etudiants)
        {
            try
            {
                // 1. Exporter les données vers CSV si nécessaire
                if (etudiants != null && etudiants.Count > 0)
                {
                    ExportStudentsToCSV(etudiants);
                }

                // 2. Appeler le script Python
                string scriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "chatbot_deliberation.py");
                var startInfo = new ProcessStartInfo
                {
                    FileName = "python",
                    Arguments = $"\"{scriptPath}\" \"{message.Replace("\"", "\\\"")}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8
                };

                using (var process = System.Diagnostics.Process.Start(startInfo))
                {
                    if (process == null)
                    {
                        return new PythonChatbotResult { Success = false, ErrorMessage = "Impossible de démarrer Python" };
                    }

                    process.WaitForExit(30000);

                    if (!process.HasExited)
                    {
                        process.Kill();
                        return new PythonChatbotResult { Success = false, ErrorMessage = "Timeout du chatbot Python" };
                    }

                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();

                    if (process.ExitCode != 0 || !string.IsNullOrEmpty(error))
                    {
                        return new PythonChatbotResult { Success = false, ErrorMessage = error };
                    }

                    // Parser la réponse JSON
                    return ParsePythonResponse(output);
                }
            }
            catch (Exception ex)
            {
                return new PythonChatbotResult { Success = false, ErrorMessage = ex.Message };
            }
        }

        /// <summary>
        /// Exporte les données étudiants vers CSV pour Python
        /// </summary>
        private void ExportStudentsToCSV(List<Etudiant> etudiants)
        {
            try
            {
                string csvPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "donnees_etudiants_import.csv");
                var lines = new List<string>();
                lines.Add("matricule;nom_prenom;classe;moyenne;decision;mention");

                foreach (var etudiant in etudiants)
                {
                    var line = $"{etudiant.Matricule};{etudiant.NomPrenom};{etudiant.ClasseGroupe};{etudiant.MoyenneGenerale:F2};{etudiant.Decision};{etudiant.Mention}";
                    lines.Add(line);
                }

                File.WriteAllLines(csvPath, lines, Encoding.UTF8);
                Console.WriteLine($"[AI ASSISTANT] Données exportées vers CSV pour Python");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AI ASSISTANT] Erreur export CSV: {ex.Message}");
            }
        }

        /// <summary>
        /// Parse la réponse du chatbot Python
        /// </summary>
        private PythonChatbotResult ParsePythonResponse(string jsonOutput)
        {
            try
            {
                var jsonObj = JsonConvert.DeserializeObject<JObject>(jsonOutput);
                
                return new PythonChatbotResult
                {
                    Success = true,
                    ResponseText = jsonObj?["texte"]?.ToString() ?? "",
                    ActionUI = jsonObj?["action_ui"]?.ToString(),
                    Classe = jsonObj?["classe"]?.ToString()
                };
            }
            catch (JsonException)
            {
                // Si ce n'est pas du JSON, traiter comme texte simple
                return new PythonChatbotResult
                {
                    Success = true,
                    ResponseText = jsonOutput.Trim()
                };
            }
            catch (Exception ex)
            {
                return new PythonChatbotResult { Success = false, ErrorMessage = ex.Message };
            }
        }

        /// <summary>
        /// Convertit la réponse Python en AiAssistantResponse
        /// </summary>
        private AiAssistantResponse ConvertPythonResponse(PythonChatbotResult pythonResponse)
        {
            var response = new AiAssistantResponse
            {
                Title = "🐍 Python Chatbot (Anthropic Claude)",
                ResponseText = pythonResponse.ResponseText
            };

            // Traiter les actions UI
            if (!string.IsNullOrEmpty(pythonResponse.ActionUI))
            {
                switch (pythonResponse.ActionUI)
                {
                    case "basculer_onglet_generation_pv":
                        response.Action = new AiAction
                        {
                            Type = AiActionType.GeneratePVWord,
                            TargetParameter = pythonResponse.Classe ?? "3A40",
                            TargetIndex = 2
                        };
                        break;

                    case "exporter_excel":
                        response.Action = new AiAction { Type = AiActionType.ExportExcel };
                        break;

                    case "filtrer_classe":
                        response.Action = new AiAction
                        {
                            Type = AiActionType.FilterClass,
                            TargetParameter = pythonResponse.Classe ?? ""
                        };
                        break;
                }
            }

            // Ajouter des suggestions par défaut
            response.SuggestedFollowUps.Add("Combien d'étudiants ont une mention Bien ?");
            response.SuggestedFollowUps.Add("Génère-moi le PV de la classe 3A40");
            response.SuggestedFollowUps.Add("Quel est le taux de réussite ?");

            return response;
        }

        /// <summary>
        /// Appel direct à l'API Anthropic Claude avec déclaration d'outils (Tool Calling)
        /// </summary>
        private AiAssistantResponse CallAnthropicClaudeApi(string apiKey, string prompt, List<Etudiant> etudiantsActuels, string currentClassFilter)
        {
            var requestPayload = new
            {
                model = "claude-3-5-sonnet-20241022",
                max_tokens = 1024,
                system = "Vous êtes l'assistant IA de délibération universitaire. Vous répondez de manière concise et professionnelle en français. Vous pouvez utiliser les outils (tools) mis à votre disposition pour déclencher des actions dans l'application (générer un PV Word, exporter en Excel, filtrer la classe, ou basculer d'onglet).",
                tools = new object[]
                {
                    new
                    {
                        name = "preparer_generation_pv",
                        description = "Prépare la génération du PV Word pour une classe et bascule sur l'onglet de génération.",
                        input_schema = new
                        {
                            type = "object",
                            properties = new
                            {
                                classe = new { type = "string", description = "Nom de la classe (ex: 3A40)" }
                            },
                            required = new[] { "classe" }
                        }
                    },
                    new
                    {
                        name = "exporter_excel",
                        description = "Exporte la liste des étudiants en fichier Excel.",
                        input_schema = new
                        {
                            type = "object",
                            properties = new { }
                        }
                    },
                    new
                    {
                        name = "filtrer_classe",
                        description = "Filtre le tableau d'affichage des étudiants sur une classe spécifique.",
                        input_schema = new
                        {
                            type = "object",
                            properties = new
                            {
                                classe = new { type = "string", description = "Nom de la classe à filtrer" }
                            },
                            required = new[] { "classe" }
                        }
                    }
                },
                messages = new[]
                {
                    new { role = "user", content = prompt }
                }
            };

            string jsonContent = JsonConvert.SerializeObject(requestPayload);
            var httpRequest = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages");
            httpRequest.Headers.Add("x-api-key", apiKey);
            httpRequest.Headers.Add("anthropic-version", "2023-06-01");
            httpRequest.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var httpResponse = _httpClient.SendAsync(httpRequest).Result;
            if (!httpResponse.IsSuccessStatusCode)
            {
                return null;
            }

            string responseString = httpResponse.Content.ReadAsStringAsync().Result;
            JObject json = JObject.Parse(responseString);

            var aiResponse = new AiAssistantResponse
            {
                Title = "🤖 Anthropic Claude AI"
            };

            string stopReason = json["stop_reason"]?.ToString();
            JArray contentArr = json["content"] as JArray;

            StringBuilder responseTextBuilder = new StringBuilder();

            if (contentArr != null)
            {
                foreach (var block in contentArr)
                {
                    string type = block["type"]?.ToString();
                    if (type == "text")
                    {
                        responseTextBuilder.AppendLine(block["text"]?.ToString());
                    }
                    else if (type == "tool_use")
                    {
                        string toolName = block["name"]?.ToString();
                        JObject toolInput = block["input"] as JObject;

                        if (toolName == "preparer_generation_pv")
                        {
                            string targetClass = toolInput?["classe"]?.ToString() ?? "3A40";
                            aiResponse.Action = new AiAction
                            {
                                Type = AiActionType.GeneratePVWord,
                                TargetParameter = targetClass,
                                TargetIndex = 2
                            };
                            responseTextBuilder.AppendLine($"\n📄 *[Action UI: Bascule vers l'onglet Génération PV pour la classe {targetClass}]*");
                        }
                        else if (toolName == "exporter_excel")
                        {
                            aiResponse.Action = new AiAction { Type = AiActionType.ExportExcel };
                            responseTextBuilder.AppendLine("\n📊 *[Action UI: Démarrage de l'exportation Excel]*");
                        }
                        else if (toolName == "filtrer_classe")
                        {
                            string targetClass = toolInput?["classe"]?.ToString() ?? "";
                            aiResponse.Action = new AiAction
                            {
                                Type = AiActionType.FilterClass,
                                TargetParameter = targetClass
                            };
                            responseTextBuilder.AppendLine($"\n🔎 *[Action UI: Application du filtre sur la classe {targetClass}]*");
                        }
                    }
                }
            }

            aiResponse.ResponseText = responseTextBuilder.ToString().Trim();
            if (string.IsNullOrWhiteSpace(aiResponse.ResponseText))
            {
                aiResponse.ResponseText = "Demande traitée par Anthropic Claude.";
            }

            aiResponse.SuggestedFollowUps.Add("Combien d'étudiants ont une mention Bien ?");
            aiResponse.SuggestedFollowUps.Add("Génère-moi le PV de la classe 3A40");
            aiResponse.SuggestedFollowUps.Add("Exporte les admis en Excel");

            return aiResponse;
        }

        /// <summary>
        /// Traitement local par règles et patterns si l'API Anthropic n'est pas activée
        /// </summary>
        private AiAssistantResponse ProcessPromptLocal(string prompt, List<Etudiant> etudiantsActuels, string currentClassFilter)
        {
            var response = new AiAssistantResponse();

            string rawPrompt = prompt.Trim();
            string normPrompt = RemoveAccents(rawPrompt.ToLowerInvariant());

            // 1. Commande : Génération de PV
            if (normPrompt.Contains("genere") || normPrompt.Contains("generer") || normPrompt.Contains("creer pv") || normPrompt.Contains("editer pv"))
            {
                return HandleGeneratePV(normPrompt, etudiantsActuels, currentClassFilter);
            }

            // 2. Commande : Exportation Excel
            if (normPrompt.Contains("export") || normPrompt.Contains("telecharger excel") || normPrompt.Contains("fichier excel"))
            {
                response.Title = "📊 Exportation Excel";
                response.ResponseText = "J'ai préparé l'exportation des étudiants en fichier Excel. L'action d'exportation est en cours d'exécution...";
                response.Action = new AiAction { Type = AiActionType.ExportExcel };
                response.SuggestedFollowUps.Add("Combien d'étudiants sont admis ?");
                response.SuggestedFollowUps.Add("Combien de mentions Très Bien ?");
                return response;
            }

            // 3. Commande : Navigation d'onglet
            if (normPrompt.Contains("onglet") || normPrompt.Contains("ouvre") || normPrompt.Contains("affiche la page") || normPrompt.Contains("va dans"))
            {
                var navResponse = HandleNavigation(normPrompt);
                if (navResponse != null) return navResponse;
            }

            // 4. Commande : Filtrage
            if (normPrompt.Contains("filtre") || normPrompt.Contains("efface") || normPrompt.Contains("reinitialise") || normPrompt.Contains("montre la classe"))
            {
                var filterResponse = HandleFiltering(normPrompt, etudiantsActuels);
                if (filterResponse != null) return filterResponse;
            }

            if (etudiantsActuels == null || !etudiantsActuels.Any())
            {
                response.Title = "📂 Aucune donnée chargée";
                response.ResponseText = "Aucune donnée d'étudiant n'est actuellement chargée. Veuillez d'abord importer un fichier Excel ou charger une classe depuis l'onglet **Import Excel** ou **Tableau de Bord**.";
                response.SuggestedFollowUps.Add("Ouvre l'onglet Import");
                response.Action = new AiAction { Type = AiActionType.SwitchTab, TargetIndex = 1 };
                return response;
            }

            var stats = _statsCalculator.CalculerStatistiques(etudiantsActuels);

            // 5. Question : Mention spécifique
            if (normPrompt.Contains("mention") || normPrompt.Contains("bien") || normPrompt.Contains("passable") || normPrompt.Contains("assez bien") || normPrompt.Contains("tres bien"))
            {
                return HandleMentionQuery(normPrompt, etudiantsActuels, stats);
            }

            // 6. Question : Décisions & Taux de réussite
            if (normPrompt.Contains("admis") || normPrompt.Contains("ajourne") || normPrompt.Contains("exclu") || normPrompt.Contains("reussite") || normPrompt.Contains("reussi") || normPrompt.Contains("valide"))
            {
                return HandleDecisionQuery(normPrompt, etudiantsActuels, stats);
            }

            // 7. Question : Moyenne & Notes
            if (normPrompt.Contains("moyenne") || normPrompt.Contains("note") || normPrompt.Contains("score") || normPrompt.Contains("mg"))
            {
                return HandleAverageQuery(normPrompt, etudiantsActuels, stats);
            }

            // 8. Question : Nombre d'étudiants total / Généralités
            if (normPrompt.Contains("combien") || normPrompt.Contains("nombre") || normPrompt.Contains("total") || normPrompt.Contains("statistique") || normPrompt.Contains("resume"))
            {
                return HandleGeneralSummary(etudiantsActuels, stats);
            }

            // Fallback conversationnel
            response.Title = "🤖 Assistant IA";
            response.ResponseText = $"J'ai analysé votre demande : *\"{rawPrompt}\"*.\n\nActuellement, la promo contient **{etudiantsActuels.Count} étudiants** ({stats.NombreTotalAdmis} Admis, {stats.NbAjournes} Ajournés/Conseil, {stats.NombreRedoubleExclu} Redouble/Exclus).\n\nPour utiliser la puissance de Claude LLM avec des outils personnalisés, configurez `setx ANTHROPIC_API_KEY \"sk-ant-...\"` dans Windows.\n\nVoici ce que vous pouvez me demander :";
            response.StatHighlights.Add($"Effectif total: {etudiantsActuels.Count}");
            response.StatHighlights.Add($"Moyenne globale: {stats.MoyenneGeneraleGlobale:F2} / 20");
            response.SuggestedFollowUps.Add("Combien d'étudiants ont une mention Bien ?");
            response.SuggestedFollowUps.Add("Génère-moi le PV de la classe 3A40");
            response.SuggestedFollowUps.Add("Quel est le taux de réussite ?");

            return response;
        }

        private AiAssistantResponse HandleGeneratePV(string normPrompt, List<Etudiant> etudiants, string currentClassFilter)
        {
            var response = new AiAssistantResponse();
            response.Title = "📄 Génération de Procès-Verbal";

            string extractedClass = ExtractClassName(normPrompt);

            if (string.IsNullOrWhiteSpace(extractedClass) && !string.IsNullOrWhiteSpace(currentClassFilter))
            {
                extractedClass = currentClassFilter;
            }

            if (string.IsNullOrWhiteSpace(extractedClass) && etudiants != null && etudiants.Any())
            {
                extractedClass = etudiants.FirstOrDefault()?.ClasseGroupe ?? "3A40";
            }

            if (string.IsNullOrWhiteSpace(extractedClass))
            {
                extractedClass = "3A40";
            }

            response.ResponseText = $"J'ai préparé la génération du **Procès-Verbal (PV) Word** pour la classe **{extractedClass}**.\n\nJe bascule vers l'onglet **Génération PV** et je pré-remplis les informations.";
            response.Action = new AiAction
            {
                Type = AiActionType.GeneratePVWord,
                TargetParameter = extractedClass,
                TargetIndex = 2
            };

            response.SuggestedFollowUps.Add("Combien d'étudiants ont eu une mention Bien ?");
            response.SuggestedFollowUps.Add("Exporte la liste en Excel");
            return response;
        }

        private AiAssistantResponse HandleMentionQuery(string normPrompt, List<Etudiant> etudiants, DeliberationStatistics stats)
        {
            var response = new AiAssistantResponse();

            int countTB = stats.NombreTresBien;
            int countB = stats.NombreBien;
            int countAB = stats.NombreAssezBien;
            int countP = stats.NombrePassable;

            if (normPrompt.Contains("tres bien"))
            {
                response.Title = "🌟 Mentions Très Bien";
                response.ResponseText = $"Sur un total de **{etudiants.Count} étudiants**, **{countTB} étudiant(s)** ont obtenu la mention **Très Bien** (MG ≥ 16/20).";
            }
            else if (normPrompt.Contains("bien") && !normPrompt.Contains("assez bien") && !normPrompt.Contains("tres bien"))
            {
                response.Title = "🥇 Mentions Bien";
                response.ResponseText = $"Sur un total de **{etudiants.Count} étudiants**, **{countB} étudiant(s)** ont obtenu la mention **Bien** (14/20 ≤ MG < 16/20) ce semestre.";
            }
            else if (normPrompt.Contains("assez bien"))
            {
                response.Title = "🥈 Mentions Assez Bien";
                response.ResponseText = $"Sur un total de **{etudiants.Count} étudiants**, **{countAB} étudiant(s)** ont obtenu la mention **Assez Bien** (12/20 ≤ MG < 14/20).";
            }
            else if (normPrompt.Contains("passable"))
            {
                response.Title = "🥉 Mentions Passable";
                response.ResponseText = $"Sur un total de **{etudiants.Count} étudiants**, **{countP} étudiant(s)** ont obtenu la mention **Passable** (10/20 ≤ MG < 12/20).";
            }
            else
            {
                response.Title = "🏆 Répartition des Mentions";
                response.ResponseText = $"Voici la répartition complète des mentions pour cette promotion ({etudiants.Count} étudiants) :\n" +
                                        $"• **Très Bien** : {countTB} étudiant(s)\n" +
                                        $"• **Bien** : {countB} étudiant(s)\n" +
                                        $"• **Assez Bien** : {countAB} étudiant(s)\n" +
                                        $"• **Passable** : {countP} étudiant(s)";
            }

            response.StatHighlights.Add($"Très Bien: {countTB}");
            response.StatHighlights.Add($"Bien: {countB}");
            response.StatHighlights.Add($"Assez Bien: {countAB}");
            response.StatHighlights.Add($"Passable: {countP}");

            response.SuggestedFollowUps.Add("Génère-moi le PV de la classe");
            response.SuggestedFollowUps.Add("Combien d'étudiants sont admis ?");
            response.SuggestedFollowUps.Add("Exporte en Excel");

            return response;
        }

        private AiAssistantResponse HandleDecisionQuery(string normPrompt, List<Etudiant> etudiants, DeliberationStatistics stats)
        {
            var response = new AiAssistantResponse();
            decimal tauxReussite = stats.PourcentageAdmis;

            if (normPrompt.Contains("reussite") || normPrompt.Contains("reussi") || normPrompt.Contains("valide"))
            {
                response.Title = "📈 Taux de Réussite";
                response.ResponseText = $"Le taux de réussite global pour cette promotion est de **{tauxReussite:F1}%**.\n\n" +
                                        $"• **Admis** : {stats.NombreTotalAdmis} / {etudiants.Count} ({tauxReussite:F1}%)\n" +
                                        $"• **Ajournés / Conseil** : {stats.NbAjournes}\n" +
                                        $"• **Redouble / Exclus** : {stats.NombreRedoubleExclu}";
            }
            else if (normPrompt.Contains("ajourne"))
            {
                response.Title = "⏳ Étudiants Ajournés";
                response.ResponseText = $"Il y a **{stats.NbAjournes} étudiant(s) ajourné(s) / en conseil**.";
            }
            else if (normPrompt.Contains("exclu") || normPrompt.Contains("redouble"))
            {
                response.Title = "❌ Redoublement / Exclusions";
                response.ResponseText = $"Il y a **{stats.NombreRedoubleExclu} étudiant(s) en redoublement/exclusion**.";
            }
            else
            {
                response.Title = "✅ Décisions d'Admission";
                response.ResponseText = $"Voici le bilan des décisions d'admission sur **{etudiants.Count} étudiants** :\n" +
                                        $"• **Admis** : {stats.NombreTotalAdmis} ({tauxReussite:F1}%)\n" +
                                        $"• **Ajournés / Conseil** : {stats.NbAjournes}\n" +
                                        $"• **Redouble / Exclus** : {stats.NombreRedoubleExclu}";
            }

            response.StatHighlights.Add($"Admis: {stats.NombreTotalAdmis}");
            response.StatHighlights.Add($"Ajournés/Conseil: {stats.NbAjournes}");
            response.StatHighlights.Add($"Redouble/Exclus: {stats.NombreRedoubleExclu}");
            response.StatHighlights.Add($"Taux Réussite: {tauxReussite:F1}%");

            response.SuggestedFollowUps.Add("Combien d'étudiants ont eu une mention Bien ?");
            response.SuggestedFollowUps.Add("Génère-moi le PV de la classe");

            return response;
        }

        private AiAssistantResponse HandleAverageQuery(string normPrompt, List<Etudiant> etudiants, DeliberationStatistics stats)
        {
            var response = new AiAssistantResponse();
            response.Title = "📊 Analyse des Moyennes";

            response.ResponseText = $"Voici l'analyse des moyennes pour la promotion ({etudiants.Count} étudiants) :\n" +
                                    $"• **Moyenne générale globale** : **{stats.MoyenneGeneraleGlobale:F2} / 20**\n" +
                                    $"• **Moyenne des admis** : **{stats.MoyenneAdmis:F2} / 20**\n" +
                                    $"• **Moyenne minimale** : **{stats.MoyenneMini:F2} / 20**\n" +
                                    $"• **Moyenne maximale** : **{stats.MoyenneMaxi:F2} / 20**";

            response.StatHighlights.Add($"Globale: {stats.MoyenneGeneraleGlobale:F2}");
            response.StatHighlights.Add($"Admis: {stats.MoyenneAdmis:F2}");
            response.StatHighlights.Add($"Min/Max: {stats.MoyenneMini:F2} - {stats.MoyenneMaxi:F2}");

            response.SuggestedFollowUps.Add("Combien d'étudiants ont eu une mention Bien ?");
            response.SuggestedFollowUps.Add("Quel est le taux de réussite ?");

            return response;
        }

        private AiAssistantResponse HandleGeneralSummary(List<Etudiant> etudiants, DeliberationStatistics stats)
        {
            var response = new AiAssistantResponse();
            response.Title = "📋 Bilan Général de Délibération";

            response.ResponseText = $"La promotion compte **{etudiants.Count} étudiants**.\n\n" +
                                    $"• **Taux de réussite** : **{stats.PourcentageAdmis:F1}%** ({stats.NombreTotalAdmis} Admis)\n" +
                                    $"• **Moyenne Générale Promo** : **{stats.MoyenneGeneraleGlobale:F2} / 20**\n" +
                                    $"• **Ajournés / Conseil** : **{stats.NbAjournes}**\n" +
                                    $"• **Redouble / Exclus** : **{stats.NombreRedoubleExclu}**";

            response.StatHighlights.Add($"Total: {etudiants.Count}");
            response.StatHighlights.Add($"Admis: {stats.NombreTotalAdmis}");
            response.StatHighlights.Add($"Moyenne: {stats.MoyenneGeneraleGlobale:F2}");

            response.SuggestedFollowUps.Add("Combien d'étudiants ont eu une mention Bien ?");
            response.SuggestedFollowUps.Add("Génère-moi le PV de la classe 3A40");
            response.SuggestedFollowUps.Add("Exporte en Excel");

            return response;
        }

        private AiAssistantResponse HandleNavigation(string normPrompt)
        {
            int targetIndex = -1;
            string tabName = "";

            if (normPrompt.Contains("tableau") || normPrompt.Contains("bord") || normPrompt.Contains("dashboard"))
            {
                targetIndex = 0;
                tabName = "Tableau de Bord";
            }
            else if (normPrompt.Contains("import") || normPrompt.Contains("excel") || normPrompt.Contains("charger"))
            {
                targetIndex = 1;
                tabName = "Import Excel";
            }
            else if (normPrompt.Contains("pv") || normPrompt.Contains("generation") || normPrompt.Contains("word"))
            {
                targetIndex = 2;
                tabName = "Génération PV";
            }
            else if (normPrompt.Contains("historique") || normPrompt.Contains("archive"))
            {
                targetIndex = 3;
                tabName = "Historique";
            }
            else if (normPrompt.Contains("parametre") || normPrompt.Contains("regle") || normPrompt.Contains("seuil"))
            {
                targetIndex = 4;
                tabName = "Paramètres";
            }
            else if (normPrompt.Contains("admin") || normPrompt.Contains("utilisateur"))
            {
                targetIndex = 5;
                tabName = "Administration";
            }

            if (targetIndex >= 0)
            {
                return new AiAssistantResponse
                {
                    Title = $"🧭 Navigation vers {tabName}",
                    ResponseText = $"Je vous redirige vers l'onglet **{tabName}**.",
                    Action = new AiAction { Type = AiActionType.SwitchTab, TargetIndex = targetIndex }
                };
            }

            return null;
        }

        private AiAssistantResponse HandleFiltering(string normPrompt, List<Etudiant> etudiants)
        {
            if (normPrompt.Contains("efface") || normPrompt.Contains("reinitialise") || normPrompt.Contains("tous") || normPrompt.Contains("annuler"))
            {
                return new AiAssistantResponse
                {
                    Title = "🔄 Réinitialisation des Filtres",
                    ResponseText = "J'ai réinitialisé les filtres d'affichage. Tous les étudiants sont présentés.",
                    Action = new AiAction { Type = AiActionType.ClearFilter }
                };
            }

            string className = ExtractClassName(normPrompt);
            if (!string.IsNullOrWhiteSpace(className))
            {
                return new AiAssistantResponse
                {
                    Title = $"🔎 Filtrage de Classe: {className}",
                    ResponseText = $"J'ai appliqué le filtre sur la classe **{className}**.",
                    Action = new AiAction { Type = AiActionType.FilterClass, TargetParameter = className }
                };
            }

            return null;
        }

        private string ExtractClassName(string prompt)
        {
            var match = Regex.Match(prompt, @"\b[0-9][A-Za-z0-9]{2,7}\b", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                return match.Value.ToUpperInvariant();
            }
            return "";
        }

        private static string RemoveAccents(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return text;
            var normalizedString = text.Normalize(NormalizationForm.FormD);
            var stringBuilder = new StringBuilder();

            foreach (var c in normalizedString)
            {
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }

            return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
        }
    }

    /// <summary>
    /// Résultat de l'appel au chatbot Python
    /// </summary>
    public class PythonChatbotResult
    {
        public bool Success { get; set; }
        public string ResponseText { get; set; } = "";
        public string ActionUI { get; set; } = "";
        public string Classe { get; set; } = "";
        public string ErrorMessage { get; set; } = "";
    }
}
