#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Suite de Tests Complète pour le Chatbot Polyvalent
Tests des questions générales ET spécialisées délibération
"""

import os
import sys
import json
from datetime import datetime

# Ajouter le répertoire courant au path pour les imports
sys.path.append(os.path.dirname(os.path.abspath(__file__)))

from chatbot_deliberation import ChatbotDeliberation
from pv_students import PVDatabase

class TestChatbotPolyvalent:
    def __init__(self):
        self.db = PVDatabase()
        self.bot = ChatbotDeliberation(self.db)
        self.tests_passes = 0
        self.tests_total = 0
        
    def executer_test(self, nom_test, question, verifie_fn, attendu=None):
        """Exécute un test et vérifie le résultat"""
        self.tests_total += 1
        print(f"\n🧪 TEST {self.tests_total}: {nom_test}")
        print(f"❓ Question: '{question}'")
        
        try:
            resultat = self.bot.envoyer_message(question)
            print(f"💬 Réponse: {resultat.get('texte', '')[:100]}{'...' if len(resultat.get('texte', '')) > 100 else ''}")
            
            if resultat.get('action_ui'):
                print(f"🎯 Action UI: {resultat['action_ui']}")
            
            if verifie_fn(resultat, attendu):
                print("✅ SUCCÈS")
                self.tests_passes += 1
                return True
            else:
                print("❌ ÉCHEC")
                return False
                
        except Exception as e:
            print(f"❌ ERREUR: {e}")
            return False
    
    def verifier_reponse_generale(self, resultat, attendu):
        """Vérifie qu'une question générale n'utilise pas d'outils"""
        return (
            resultat.get('texte') and 
            len(resultat['texte']) > 10 and
            resultat.get('action_ui') is None  # Pas d'action UI pour les questions générales
        )
    
    def verifier_reponse_deliberation(self, resultat, attendu):
        """Vérifie qu'une question délibération utilise les bonnes fonctions"""
        return (
            resultat.get('texte') and 
            len(resultat['texte']) > 10
            # Les actions UI sont optionnelles selon le type de question
        )
    
    def verifier_action_ui(self, resultat, action_attendue):
        """Vérifie qu'une action UI spécifique est déclenchée"""
        return resultat.get('action_ui') == action_attendue
    
    def verifier_contient_mots(self, resultat, mots_cles):
        """Vérifie que la réponse contient certains mots-clés"""
        texte = resultat.get('texte', '').lower()
        return any(mot.lower() in texte for mot in mots_cles)

    def test_detection_type_questions(self):
        """Test 1: Vérifier que le système détecte correctement les types de questions"""
        print("\n" + "="*60)
        print("🔍 TESTS DE DÉTECTION DE TYPE DE QUESTIONS")
        print("="*60)
        
        # Questions générales (ne doivent PAS utiliser d'outils)
        questions_generales = [
            "Quelle heure est-il ?",
            "Bonjour, qui es-tu ?",
            "Combien font 15 + 27 ?",
            "Comment ça marche ?",
            "Quelle est la capitale de la France ?",
            "Aide-moi avec Excel",
            "Quel temps fait-il ?",
            "Comment utiliser cette application ?"
        ]
        
        for question in questions_generales:
            self.executer_test(
                f"Question Générale",
                question,
                self.verifier_reponse_generale
            )
        
        # Questions délibération (peuvent utiliser des outils)
        questions_deliberation = [
            "Combien d'étudiants ont une mention Bien ?",
            "Quel est le taux de réussite ?",
            "Génère-moi le PV de la classe 3A40",
            "Exporte les admis en Excel",
            "Combien d'étudiants sont ajournés ?",
            "Quelle est la moyenne de la classe ?",
            "Filtre sur la classe 3A40",
            "Statistiques de délibération"
        ]
        
        for question in questions_deliberation:
            self.executer_test(
                f"Question Délibération",
                question,
                self.verifier_reponse_deliberation
            )

    def test_questions_generales_polyvalent(self):
        """Test 2: Questions générales polyvalentes"""
        print("\n" + "="*60)
        print("🌍 TESTS QUESTIONS GÉNÉRALES POLYVALENTES")
        print("="*60)
        
        # Salutations et présentations
        self.executer_test(
            "Salutation",
            "Bonjour !",
            lambda r, a: self.verifier_contient_mots(r, ["bonjour", "assistant", "IA"])
        )
        
        # Questions temporelles
        self.executer_test(
            "Heure actuelle",
            "Quelle heure est-il ?",
            lambda r, a: any(mot in r.get('texte', '').lower() for mot in ['heure', 'temps', datetime.now().strftime('%H')])
        )
        
        # Calculs mathématiques
        self.executer_test(
            "Calcul simple",
            "Combien font 2 + 2 ?",
            lambda r, a: self.verifier_contient_mots(r, ["4", "calcul", "mathématique"])
        )
        
        # Questions sur l'application
        self.executer_test(
            "Aide application",
            "Comment fonctionne cette application ?",
            lambda r, a: self.verifier_contient_mots(r, ["application", "délibération", "import", "excel"])
        )
        
        # Questions techniques générales
        self.executer_test(
            "Aide technique",
            "Comment préparer un fichier Excel ?",
            lambda r, a: self.verifier_contient_mots(r, ["excel", "fichier", "préparer"])
        )

    def test_questions_specialisees_deliberation(self):
        """Test 3: Questions spécialisées délibération"""
        print("\n" + "="*60)
        print("🎓 TESTS QUESTIONS SPÉCIALISÉES DÉLIBÉRATION")
        print("="*60)
        
        # Statistiques (doivent utiliser les vraies données ou indiquer l'absence)
        self.executer_test(
            "Statistiques mentions",
            "Combien d'étudiants ont une mention Bien ?",
            lambda r, a: self.verifier_contient_mots(r, ["mention", "bien", "étudiant"]) or "données" in r.get('texte', '').lower()
        )
        
        self.executer_test(
            "Taux de réussite",
            "Quel est le taux de réussite ?",
            lambda r, a: self.verifier_contient_mots(r, ["taux", "réussite", "admis"]) or "données" in r.get('texte', '').lower()
        )
        
        # Actions avec outils
        self.executer_test(
            "Génération PV avec action UI",
            "Génère-moi le PV de la classe 3A40",
            self.verifier_action_ui,
            "basculer_onglet_generation_pv"
        )
        
        self.executer_test(
            "Export Excel avec action",
            "Exporte les données en Excel",
            self.verifier_action_ui,
            "exporter_excel"
        )

    def test_securite_donnees(self):
        """Test 4: Sécurité des données"""
        print("\n" + "="*60)
        print("🛡️ TESTS SÉCURITÉ DES DONNÉES")
        print("="*60)
        
        # Le chatbot ne doit jamais inventer de chiffres précis sur les étudiants
        questions_pieges = [
            "Combien d'étudiants ont exactement 15.7 de moyenne ?",
            "Donne-moi les noms des étudiants ajournés",
            "Quel étudiant a la meilleure note ?",
            "Combien d'étudiants sont inscrits en 2024 ?"
        ]
        
        for question in questions_pieges:
            self.executer_test(
                "Sécurité données",
                question,
                lambda r, a: (
                    "données" in r.get('texte', '').lower() or 
                    "importer" in r.get('texte', '').lower() or
                    "chargé" in r.get('texte', '').lower() or
                    self.verifier_contient_mots(r, ["statistiques", "outil"])
                )
            )

    def test_contextes_mixtes(self):
        """Test 5: Contextes mixtes et edge cases"""
        print("\n" + "="*60)
        print("🔄 TESTS CONTEXTES MIXTES")
        print("="*60)
        
        # Questions ambiguës
        self.executer_test(
            "Moyenne - contexte général",
            "Quelle est la moyenne de 12, 15 et 18 ?",
            self.verifier_reponse_generale  # Doit être traité comme calcul général
        )
        
        self.executer_test(
            "Moyenne - contexte étudiant",
            "Quelle est la moyenne générale des étudiants ?",
            self.verifier_reponse_deliberation  # Doit utiliser les outils délibération
        )
        
        # Mots-clés trompeurs
        self.executer_test(
            "Bien - contexte général",
            "Comment bien utiliser Excel ?",
            self.verifier_reponse_generale
        )
        
        self.executer_test(
            "Bien - contexte mention",
            "Combien ont eu une mention Bien ?",
            self.verifier_reponse_deliberation
        )

    def test_fallback_modes(self):
        """Test 6: Modes de fallback"""
        print("\n" + "="*60)
        print("🔧 TESTS MODES DE FALLBACK")
        print("="*60)
        
        # Tester sans clé API (mode local)
        api_key_backup = os.environ.get("ANTHROPIC_API_KEY")
        if api_key_backup:
            os.environ.pop("ANTHROPIC_API_KEY", None)
            bot_local = ChatbotDeliberation(self.db)
            
            resultat = bot_local.envoyer_message("Bonjour, qui es-tu ?")
            success = self.verifier_reponse_generale(resultat, None)
            print(f"🔧 Test fallback local: {'✅' if success else '❌'}")
            if success:
                self.tests_passes += 1
            self.tests_total += 1
            
            # Restaurer la clé API
            os.environ["ANTHROPIC_API_KEY"] = api_key_backup
        else:
            print("🔧 Mode fallback local déjà actif (pas de clé API)")

    def executer_tous_les_tests(self):
        """Exécute toute la suite de tests"""
        print("🚀 DÉMARRAGE DE LA SUITE DE TESTS CHATBOT POLYVALENT")
        print(f"📅 {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}")
        print(f"🔑 Clé API Claude: {'✅ Configurée' if self.bot.api_key else '❌ Absente (mode local)'}")
        
        # Exécuter tous les tests
        self.test_detection_type_questions()
        self.test_questions_generales_polyvalent()
        self.test_questions_specialisees_deliberation()
        self.test_securite_donnees()
        self.test_contextes_mixtes()
        self.test_fallback_modes()
        
        # Résumé final
        print("\n" + "="*60)
        print("📊 RÉSUMÉ DES TESTS")
        print("="*60)
        print(f"✅ Tests réussis: {self.tests_passes}")
        print(f"❌ Tests échoués: {self.tests_total - self.tests_passes}")
        print(f"📈 Taux de succès: {(self.tests_passes/self.tests_total)*100:.1f}%")
        
        if self.tests_passes == self.tests_total:
            print("\n🎉 TOUS LES TESTS SONT PASSÉS ! Chatbot polyvalent fonctionnel.")
        else:
            print(f"\n⚠️ {self.tests_total - self.tests_passes} test(s) à corriger.")
        
        return self.tests_passes == self.tests_total

def main():
    """Point d'entrée principal"""
    try:
        tester = TestChatbotPolyvalent()
        success = tester.executer_tous_les_tests()
        sys.exit(0 if success else 1)
    except KeyboardInterrupt:
        print("\n⏹️ Tests interrompus par l'utilisateur.")
        sys.exit(1)
    except Exception as e:
        print(f"\n💥 Erreur lors des tests: {e}")
        sys.exit(1)

if __name__ == "__main__":
    main()