# -*- coding: utf-8 -*-
"""
Module de gestion et de requêtes sur la base de données des étudiants
pour le chatbot de délibération.
"""

import json
import os

class PVDatabase:
    def __init__(self, filename="donnees_etudiants_import.csv"):
        self.filename = filename
        self.etudiants = self._charger_donnees()

    def _charger_donnees(self):
        etudiants = []
        if not os.path.exists(self.filename):
            print(f"[PVDatabase] Fichier {self.filename} non trouvé, utilisation des données de démonstration")
            # Données factices de démonstration si le fichier CSV n'existe pas encore
            return [
                {"matricule": "12345", "nom_prenom": "Alice Dupont", "classe": "3A40", "moyenne": 15.5, "decision": "Admis", "mention": "Bien"},
                {"matricule": "12346", "nom_prenom": "Bob Martin", "classe": "3A40", "moyenne": 16.8, "decision": "Admis", "mention": "Très Bien"},
                {"matricule": "12347", "nom_prenom": "Charlie Durand", "classe": "3A40", "moyenne": 14.2, "decision": "Admis", "mention": "Bien"},
                {"matricule": "12348", "nom_prenom": "David Bernard", "classe": "3A40", "moyenne": 9.1, "decision": "Ajourné", "mention": ""},
                {"matricule": "12349", "nom_prenom": "Eve Petit", "classe": "4TWIN1", "moyenne": 12.0, "decision": "Admis", "mention": "Assez Bien"},
                {"matricule": "12350", "nom_prenom": "Frank Moreau", "classe": "4TWIN1", "moyenne": 17.2, "decision": "Admis", "mention": "Très Bien"},
                {"matricule": "12351", "nom_prenom": "Grace Leroy", "classe": "4TWIN1", "moyenne": 13.8, "decision": "Admis", "mention": "Bien"},
                {"matricule": "12352", "nom_prenom": "Henri Blanc", "classe": "3A40", "moyenne": 8.5, "decision": "Ajourné", "mention": ""}
            ]

        try:
            with open(self.filename, 'r', encoding='utf-8') as f:
                lines = f.readlines()
                if not lines:
                    print("[PVDatabase] Fichier CSV vide")
                    return etudiants
                
                # Ignorer la première ligne si c'est un header
                start_index = 1 if lines[0].startswith('matricule') or 'nom' in lines[0].lower() else 0
                
                for i, line in enumerate(lines[start_index:], start=start_index+1):
                    parts = line.strip().split(';')
                    if len(parts) >= 4:  # Au minimum matricule, nom, classe, moyenne
                        try:
                            # Gestion flexible de la moyenne (virgule ou point)
                            moy_str = parts[3].replace(',', '.')
                            moy = float(moy_str) if moy_str else 0.0
                        except (ValueError, IndexError):
                            print(f"[PVDatabase] Ligne {i}: moyenne invalide '{parts[3] if len(parts) > 3 else 'manquante'}'")
                            moy = 0.0
                        
                        etudiant = {
                            "matricule": parts[0].strip(),
                            "nom_prenom": parts[1].strip(),
                            "classe": parts[2].strip(),
                            "moyenne": moy,
                            "decision": parts[4].strip() if len(parts) > 4 else "En attente",
                            "mention": parts[5].strip() if len(parts) > 5 else ""
                        }
                        etudiants.append(etudiant)
                
                print(f"[PVDatabase] Chargé {len(etudiants)} étudiants depuis {self.filename}")
                        
        except Exception as e:
            print(f"[PVDatabase] Erreur de lecture : {e}")
        
        return etudiants

    def obtenir_statistiques(self, classe=None):
        filtrés = self.etudiants
        if classe:
            filtrés = [e for e in self.etudiants if e.get("classe", "").lower() == classe.lower()]

        total = len(filtrés)
        if total == 0:
            return {"total": 0, "message": f"Aucun étudiant trouvé pour la classe {classe}" if classe else "Aucun étudiant dans la base"}

        admis = [e for e in filtrés if "admis" in e.get("decision", "").lower()]
        ajournés = [e for e in filtrés if "ajourn" in e.get("decision", "").lower() or "conseil" in e.get("decision", "").lower()]
        exclus = [e for e in filtrés if "exclu" in e.get("decision", "").lower() or "redouble" in e.get("decision", "").lower()]

        mentions_tb = len([e for e in filtrés if "très bien" in e.get("mention", "").lower()])
        mentions_b = len([e for e in filtrés if "bien" in e.get("mention", "").lower() and "très" not in e.get("mention", "").lower() and "assez" not in e.get("mention", "").lower()])
        mentions_ab = len([e for e in filtrés if "assez bien" in e.get("mention", "").lower()])
        mentions_p = len([e for e in filtrés if "passable" in e.get("mention", "").lower()])

        moyennes = [e.get("moyenne", 0.0) for e in filtrés if e.get("moyenne") is not None]
        moy_gen = sum(moyennes) / len(moyennes) if moyennes else 0.0

        return {
            "classe": classe or "Toutes les classes",
            "total_etudiants": total,
            "nombre_admis": len(admis),
            "nombre_ajournes": len(ajournés),
            "nombre_exclus": len(exclus),
            "taux_reussite": round((len(admis) / total) * 100, 2),
            "moyenne_generale": round(moy_gen, 2),
            "mentions": {
                "tres_bien": mentions_tb,
                "bien": mentions_b,
                "assez_bien": mentions_ab,
                "passable": mentions_p
            }
        }
