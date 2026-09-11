# -*- coding: utf-8 -*-
"""
Module Chatbot de Délibération avec support d'Appel d'Outils (Tool Use)
Intégration d'Anthropic Claude API & UI Actions.
"""

import os
import json
from pv_students import PVDatabase

TOOLS = [
    {
        "name": "obtenir_statistiques",
        "description": "Obtient les statistiques complètes de délibération d'une classe ou de la promotion entière (taux de réussite, admis, ajournés, mentions, moyenne générale).",
        "input_schema": {
            "type": "object",
            "properties": {
                "classe": {
                    "type": "string",
                    "description": "Le nom de la classe (ex: '3A40', '4TWIN1'). Optionnel si demande globale."
                }
            }
        }
    },
    {
        "name": "preparer_generation_pv",
        "description": "Prépare la génération du document Procès-Verbal (PV) Word pour une classe donnée et déclenche la bascule vers l'onglet de génération.",
        "input_schema": {
            "type": "object",
            "properties": {
                "classe": {
                    "type": "string",
                    "description": "Le nom de la classe pour laquelle générer le PV."
                }
            },
            "required": ["classe"]
        }
    },
    {
        "name": "exporter_excel",
        "description": "Déclenche l'exportation des données des étudiants sous forme de fichier Excel.",
        "input_schema": {
            "type": "object",
            "properties": {
                "decision": {
                    "type": "string",
                    "description": "Filtre de décision optionnel ('Admis', 'Ajourné')."
                }
            }
        }
    },
    {
        "name": "filtrer_classe",
        "description": "Applique un filtre d'affichage sur la grille des étudiants dans l'interface de l'application.",
        "input_schema": {
            "type": "object",
            "properties": {
                "classe": {
                    "type": "string",
                    "description": "Nom de la classe à filtrer (ex: '3A40')."
                }
            },
            "required": ["classe"]
        }
    },
    {
        "name": "supprimer_historique",
        "description": "Supprime une entrée spécifique de l'historique des PV générés.",
        "input_schema": {
            "type": "object",
            "properties": {
                "id_historique": {
                    "type": "integer",
                    "description": "ID de l'entrée d'historique à supprimer."
                }
            },
            "required": ["id_historique"]
        }
    },
    {
        "name": "basculer_onglet",
        "description": "Navigue vers un onglet spécifique de l'application.",
        "input_schema": {
            "type": "object",
            "properties": {
                "onglet": {
                    "type": "string",
                    "description": "Nom de l'onglet (tableau_bord, import_excel, generation_pv, historique, parametres, admin)."
                }
            },
            "required": ["onglet"]
        }
    },
    {
        "name": "envoyer_email_pv",
        "description": "Envoie le PV généré par email à une liste de destinataires.",
        "input_schema": {
            "type": "object",
            "properties": {
                "destinataires": {
                    "type": "array",
                    "items": {"type": "string"},
                    "description": "Liste d'adresses email des destinataires."
                },
                "classe": {
                    "type": "string",
                    "description": "Nom de la classe concernée par le PV."
                }
            },
            "required": ["destinataires", "classe"]
        }
    },
    {
        "name": "comparer_classes",
        "description": "Compare les performances entre deux classes ou sessions.",
        "input_schema": {
            "type": "object",
            "properties": {
                "classe1": {
                    "type": "string",
                    "description": "Première classe à comparer."
                },
                "classe2": {
                    "type": "string", 
                    "description": "Deuxième classe à comparer."
                }
            },
            "required": ["classe1", "classe2"]
        }
    }
]

class ChatbotDeliberation:
    def __init__(self, db: PVDatabase):
        self.db = db
        self.api_key = os.environ.get("ANTHROPIC_API_KEY")
        self.client = None

        if self.api_key:
            try:
                import anthropic
                self.client = anthropic.Anthropic(api_key=self.api_key)
            except ImportError:
                print("[Chatbot] Module 'anthropic' non installé. Exécutez 'pip install anthropic'.")

    def _executer_outil(self, nom_outil, arguments):
        """
        Étape 4 : Exécution dynamique des outils selon le nom d'outil renvoyé par Claude
        """
        if nom_outil == "obtenir_statistiques":
            classe = arguments.get("classe")
            stats = self.db.obtenir_statistiques(classe)
            return json.dumps(stats, ensure_ascii=False)

        elif nom_outil == "preparer_generation_pv":
            classe = arguments.get("classe", "3A40")
            return json.dumps({
                "statut": "succès",
                "message": f"Formulaire pré-rempli et prêt pour la classe {classe}.",
                "action_ui": "basculer_onglet_generation_pv",
                "classe": classe
            }, ensure_ascii=False)

        elif nom_outil == "exporter_excel":
            decision = arguments.get("decision")
            return json.dumps({
                "statut": "succès",
                "message": f"Exportation Excel démarrée{' pour ' + decision if decision else ''}.",
                "action_ui": "exporter_excel",
                "decision": decision
            }, ensure_ascii=False)

        elif nom_outil == "filtrer_classe":
            classe = arguments.get("classe")
            return json.dumps({
                "statut": "succès",
                "message": f"Filtre appliqué pour la classe {classe}.",
                "action_ui": "filtrer_classe",
                "classe": classe
            }, ensure_ascii=False)

        elif nom_outil == "supprimer_historique":
            id_historique = arguments.get("id_historique")
            return json.dumps({
                "statut": "succès",
                "message": f"Suppression de l'entrée d'historique #{id_historique} en cours.",
                "action_ui": "supprimer_historique",
                "id_historique": id_historique
            }, ensure_ascii=False)

        elif nom_outil == "basculer_onglet":
            onglet = arguments.get("onglet")
            return json.dumps({
                "statut": "succès",
                "message": f"Basculement vers l'onglet {onglet}.",
                "action_ui": "basculer_onglet",
                "onglet": onglet
            }, ensure_ascii=False)

        elif nom_outil == "envoyer_email_pv":
            destinataires = arguments.get("destinataires", [])
            classe = arguments.get("classe", "")
            return json.dumps({
                "statut": "succès",
                "message": f"Envoi du PV de la classe {classe} à {len(destinataires)} destinataire(s).",
                "action_ui": "envoyer_email",
                "destinataires": destinataires,
                "classe": classe
            }, ensure_ascii=False)

        elif nom_outil == "comparer_classes":
            classe1 = arguments.get("classe1")
            classe2 = arguments.get("classe2")
            # Ici on pourrait implémenter la logique de comparaison
            stats1 = self.db.obtenir_statistiques(classe1)
            stats2 = self.db.obtenir_statistiques(classe2)
            
            comparaison = {
                "classe1": classe1,
                "classe2": classe2,
                "stats1": stats1,
                "stats2": stats2,
                "differences": {
                    "taux_reussite": stats1.get("taux_reussite", 0) - stats2.get("taux_reussite", 0),
                    "moyenne_generale": stats1.get("moyenne_generale", 0) - stats2.get("moyenne_generale", 0)
                }
            }
            
            return json.dumps({
                "statut": "succès",
                "message": f"Comparaison entre {classe1} et {classe2} effectuée.",
                "action_ui": "afficher_comparaison",
                "comparaison": comparaison
            }, ensure_ascii=False)

        return json.dumps({"erreur": f"Outil inconnu '{nom_outil}'"})

    def envoyer_message(self, texte_utilisateur):
        """
        Envoie un message au chatbot et gère la boucle d'appel d'outils (Tool Calling).
        Renoie un dictionnaire avec 'texte' et optionnellement 'action_ui' et 'classe'.
        """
        if not self.client:
            # Mode de secours sans API key ou sans module anthropic
            return self._fallback_local(texte_utilisateur)

        try:
            messages = [{"role": "user", "content": texte_utilisateur}]
            
            # Détecter si la question porte sur les données d'étudiants
            if self._est_question_donnees_etudiants(texte_utilisateur):
                system_prompt = """Vous êtes l'assistant IA de délibération universitaire spécialisé dans l'analyse des données étudiants.

IMPORTANT : Cette conversation porte sur des données RÉELLES d'étudiants. 
- Utilisez UNIQUEMENT les outils fournis pour obtenir des informations factuelles
- Ne jamais inventer ou estimer de chiffres 
- Si vous n'avez pas l'information exacte via un outil, dites-le clairement
- Répondez en français de manière professionnelle et précise

Les outils disponibles vous donnent accès aux vraies données de délibération."""
                
                # Appel avec outils pour les questions sur les données
                response = self.client.messages.create(
                    model="claude-3-5-sonnet-20241022",
                    max_tokens=1024,
                    system=system_prompt,
                    tools=TOOLS,
                    messages=messages
                )
            else:
                # Réponse directe sans outils pour les questions générales
                system_prompt = """Vous êtes un assistant IA polyvalent et conversationnel.

Répondez naturellement à toutes les questions générales : météo, actualités, calculs, conseils, culture générale, etc.

Vous êtes aussi spécialisé en délibérations universitaires, mais pour cette question, répondez comme un assistant général classique.

Soyez naturel, utile et conversationnel en français."""
                
                response = self.client.messages.create(
                    model="claude-3-5-sonnet-20241022",
                    max_tokens=1024,
                    system=system_prompt,
                    messages=messages
                )

            action_ui_result = None
            classe_result = None

            # Si Claude décide d'appeler un ou plusieurs outils
            if response.stop_reason == "tool_use":
                tool_use_blocks = [b for b in response.content if b.type == "tool_use"]
                
                # Ajouter la réponse assistant avec tool_use
                messages.append({"role": "assistant", "content": response.content})

                tool_results_content = []
                for tool_block in tool_use_blocks:
                    nom_outil = tool_block.name
                    args_outil = tool_block.input
                    
                    # Exécuter l'outil
                    resultat_str = self._executer_outil(nom_outil, args_outil)
                    
                    try:
                        res_json = json.loads(resultat_str)
                        if "action_ui" in res_json:
                            action_ui_result = res_json["action_ui"]
                        if "classe" in res_json:
                            classe_result = res_json["classe"]
                    except:
                        pass

                    tool_results_content.append({
                        "type": "tool_result",
                        "tool_use_id": tool_block.id,
                        "content": resultat_str
                    })

                # Renvoyer le résultat de l'outil à Claude pour synthétiser la réponse utilisateur
                messages.append({"role": "user", "content": tool_results_content})
                
                final_response = self.client.messages.create(
                    model="claude-3-5-sonnet-20241022",
                    max_tokens=1024,
                    system=system_prompt,
                    messages=messages
                )

                texte_final = "".join([b.text for b in final_response.content if hasattr(b, 'text')])
                return {
                    "texte": texte_final,
                    "action_ui": action_ui_result,
                    "classe": classe_result
                }

            else:
                texte_final = "".join([b.text for b in response.content if hasattr(b, 'text')])
                return {
                    "texte": texte_final,
                    "action_ui": None
                }

        except Exception as e:
            return {
                "texte": f"Erreur lors de la communication avec l'IA Anthropic : {str(e)}",
                "action_ui": None
            }

    def _est_question_donnees_etudiants(self, texte):
        """
        Détermine si une question porte sur les données spécifiques des étudiants
        """
        texte_norm = texte.lower().strip()
        
        # Mots-clés indiquant une question sur les données étudiants/délibération
        mots_cles_etudiants = [
            "etudiant", "etudiante", "etudiants", "eleve", "eleves",
            "mention", "moyenne", "note", "admis", "ajourne", "exclu",
            "reussite", "echec", "classe", "promotion", "promo",
            "deliberation", "pv", "proces", "verbal",
            "statistique", "taux", "pourcentage", "effectif",
            "bien", "tres bien", "assez bien", "passable",
            "mg", "score", "resultat", "decision", "conseil",
            "export", "genere", "generer", "filtre"
        ]

        # Contextes spécifiques aux données 
        mots_contextuels = ["combien", "nombre", "total", "resume"]

        # Vérification directe des mots-clés étudiants
        for mot_cle in mots_cles_etudiants:
            if mot_cle in texte_norm:
                return True

        # Vérification contextuelle (ex: "combien" seul = général, "combien d'étudiants" = données)
        for mot_contexte in mots_contextuels:
            if mot_contexte in texte_norm:
                # Vérifier si c'est dans un contexte étudiant
                for mot_etudiant in mots_cles_etudiants:
                    if mot_etudiant in texte_norm:
                        return True

        return False

    def _fallback_local(self, texte):
        """Mode local d'analyse intelligente si aucune clé ANTHROPIC_API_KEY n'est configurée."""
        t = texte.lower()
        
        # Vérifier si c'est une question sur les données étudiants
        if self._est_question_donnees_etudiants(texte):
            # Traitement des questions spécialisées délibération
            if "pv" in t or "genere" in t or "génère" in t:
                # Extraire classe si présente
                classe = "3A40"
                for word in t.split():
                    if len(word) >= 3 and word[0].isdigit():
                        classe = word.upper()
                return {
                    "texte": f"J'ai préparé la génération du PV de la classe {classe} et je vous bascule sur l'onglet correspondant. (Pour utiliser Anthropic Claude, configurez ANTHROPIC_API_KEY).",
                    "action_ui": "basculer_onglet_generation_pv",
                    "classe": classe
                }
            elif "stat" in t or "mention" in t or "reussite" in t or "réussite" in t or "bien" in t:
                stats = self.db.obtenir_statistiques()
                txt = f"📊 Statistiques de délibération :\n" \
                      f"• Effectif total : {stats.get('total_etudiants')}\n" \
                      f"• Admis : {stats.get('nombre_admis')} (Taux : {stats.get('taux_reussite')}%)\n" \
                      f"• Ajournés : {stats.get('nombre_ajournes')}\n" \
                      f"• Moyenne générale : {stats.get('moyenne_generale')}/20\n" \
                      f"• Mentions Bien : {stats.get('mentions', {}).get('bien')}"
                return {"texte": txt, "action_ui": None}
            elif "export" in t or "excel" in t:
                return {
                    "texte": "Démarrage de l'exportation Excel des étudiants...",
                    "action_ui": "exporter_excel"
                }
            else:
                return {
                    "texte": f"Question sur les délibérations détectée, mais je ne peux traiter que les statistiques, génération PV et exports sans clé API Claude. Pour des réponses plus sophistiquées, configurez ANTHROPIC_API_KEY.",
                    "action_ui": None
                }
        else:
            # Questions générales - Réponses d'assistant polyvalent
            return self._reponse_generale_locale(texte)

    def _reponse_generale_locale(self, texte):
        """Gère les questions générales sans API Claude"""
        t = texte.lower()
        
        # Salutations
        if any(mot in t for mot in ["bonjour", "salut", "hello", "qui es tu", "presentation"]):
            return {
                "texte": "Bonjour ! Je suis l'Assistant IA de délibération universitaire, mais je peux aussi répondre à des questions générales !\n\n" +
                        "• 🎓 Questions sur les délibérations (statistiques, PV, exports)\n" +
                        "• 💬 Questions générales (calculs, aide, informations)\n\n" +
                        "Pour des réponses avancées avec Anthropic Claude, configurez ANTHROPIC_API_KEY.",
                "action_ui": None
            }
        
        # Questions sur l'heure et la date
        elif any(mot in t for mot in ["quelle heure", "quel jour", "date"]):
            import datetime
            now = datetime.datetime.now()
            return {
                "texte": f"🕒 Nous sommes le {now.strftime('%A %d %B %Y')} et il est {now.strftime('%H:%M')}.\n\n" +
                        "Besoin d'aide pour programmer une délibération ou autre chose ?",
                "action_ui": None
            }
        
        # Calculs simples
        elif any(mot in t for mot in ["calcul", "combien font", "plus", "moins", "multiplie"]) and not any(mot in t for mot in ["etudiant", "moyenne"]):
            return {
                "texte": "🔢 Je peux faire des calculs simples !\n\n" +
                        "Exemples: 'Combien font 15 + 27 ?', 'Quelle est la racine de 144 ?'\n\n" +
                        "Pour des calculs sur les notes d'étudiants, utilisez mes fonctions de délibération spécialisées.",
                "action_ui": None
            }
        
        # Questions sur l'application
        elif any(mot in t for mot in ["application", "comment ca marche", "aide", "utiliser"]):
            return {
                "texte": "📱 Cette application de délibération vous permet de :\n\n" +
                        "• Importer des fichiers Excel avec les notes\n" +
                        "• Générer automatiquement les PV de délibération\n" +
                        "• Exporter les résultats\n" +
                        "• Poser des questions à cet Assistant IA\n\n" +
                        "Commencez par l'onglet 'Import Excel' !",
                "action_ui": None
            }
        
        # Questions météo, actualités, culture générale
        elif any(mot in t for mot in ["meteo", "temperature", "actualite", "nouvelles"]):
            return {
                "texte": "🌍 Je suis spécialisé dans les délibérations universitaires, mais je peux vous aider !\n\n" +
                        "Pour des informations précises sur la météo ou l'actualité, consultez des services spécialisés.\n\n" +
                        "Par contre, pour tout ce qui concerne vos délibérations étudiantes, je suis votre expert !",
                "action_ui": None
            }
        
        # Fallback général
        else:
            return {
                "texte": f"🤖 Vous m'avez demandé : '{texte}'\n\n" +
                        "Je suis un assistant polyvalent spécialisé en délibérations universitaires.\n\n" +
                        "**Mes spécialités :**\n" +
                        "• Questions sur vos données étudiants\n" +
                        "• Génération de PV et exports\n" +
                        "• Questions générales et conseils\n\n" +
                        "Comment puis-je vous aider précisément ?",
                "action_ui": None
            }


if __name__ == "__main__":
    import sys
    
    if len(sys.argv) > 1:
        # Mode CLI appelé depuis C#
        message_utilisateur = sys.argv[1]
        
        try:
            db = PVDatabase()
            bot = ChatbotDeliberation(db)
            resultat = bot.envoyer_message(message_utilisateur)
            
            # Retourner le résultat en JSON pour C#
            print(json.dumps(resultat, ensure_ascii=False))
        except Exception as e:
            # En cas d'erreur, retourner un JSON d'erreur
            error_result = {
                "texte": f"Erreur du chatbot Python: {str(e)}",
                "action_ui": None,
                "classe": None
            }
            print(json.dumps(error_result, ensure_ascii=False))
    else:
        # Mode test interactif
        db = PVDatabase()
        bot = ChatbotDeliberation(db)
        print("=== CHATBOT DELIBERATION (CLI TEST) ===")
        print("Clé API détectée :", "OUI" if bot.api_key else "NON (Mode local)")
        
        while True:
            try:
                user_input = input("\nVotre question (ou 'quit' pour quitter): ").strip()
                if user_input.lower() in ['quit', 'q', 'exit']:
                    break
                    
                if user_input:
                    res = bot.envoyer_message(user_input)
                    print("\nRéponse :", res["texte"])
                    if res.get("action_ui"):
                        print("Action UI :", res.get("action_ui"))
                    if res.get("classe"):
                        print("Classe :", res.get("classe"))
            except KeyboardInterrupt:
                break
            except Exception as e:
                print(f"Erreur: {e}")
        
        print("\nAu revoir !")
