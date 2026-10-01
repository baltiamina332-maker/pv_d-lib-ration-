#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Script de test pour vérifier le fonctionnement du chatbot de délibération
"""

import os
import sys
from pv_students import PVDatabase
from chatbot_deliberation import ChatbotDeliberation

def test_database():
    """Test de la base de données"""
    print("🔍 Test de PVDatabase...")
    try:
        db = PVDatabase()
        etudiants = db.etudiants
        print(f"✅ Base de données: {len(etudiants)} étudiants chargés")
        
        # Afficher quelques étudiants
        for i, etudiant in enumerate(etudiants[:3]):
            print(f"   {i+1}. {etudiant['nom_prenom']} - {etudiant['classe']} - {etudiant['moyenne']}/20")
        
        # Test des statistiques
        stats = db.obtenir_statistiques()
        print(f"✅ Statistiques globales: {stats['total_etudiants']} étudiants, {stats['taux_reussite']}% réussite")
        
        # Test d'une classe spécifique
        stats_classe = db.obtenir_statistiques("3A40")
        print(f"✅ Statistiques classe 3A40: {stats_classe['total_etudiants']} étudiants")
        
        return True
    except Exception as e:
        print(f"❌ Erreur base de données: {e}")
        return False

def test_chatbot():
    """Test du chatbot"""
    print("\n🤖 Test de ChatbotDeliberation...")
    try:
        db = PVDatabase()
        bot = ChatbotDeliberation(db)
        
        # Vérifier la détection de l'API
        api_status = "OUI" if bot.api_key else "NON"
        client_status = "OUI" if bot.client else "NON" 
        print(f"✅ Chatbot initialisé - API Key: {api_status}, Client Anthropic: {client_status}")
        
        # Tests avec différents types de messages
        test_messages = [
            "Combien d'étudiants ont eu une mention Bien ?",
            "Génère-moi le PV de la classe 3A40",
            "Quel est le taux de réussite ?",
            "Exporte les données en Excel",
            "Statistiques globales"
        ]
        
        for i, message in enumerate(test_messages, 1):
            print(f"\n📝 Test {i}: {message}")
            try:
                resultat = bot.envoyer_message(message)
                print(f"   ✅ Réponse: {resultat['texte'][:100]}...")
                if resultat.get('action_ui'):
                    print(f"   🎯 Action UI: {resultat['action_ui']}")
                if resultat.get('classe'):
                    print(f"   🏫 Classe: {resultat['classe']}")
            except Exception as e:
                print(f"   ❌ Erreur: {e}")
        
        return True
    except Exception as e:
        print(f"❌ Erreur chatbot: {e}")
        return False

def test_environment():
    """Test de l'environnement"""
    print("🔧 Test de l'environnement...")
    
    # Test Python
    print(f"✅ Python {sys.version}")
    
    # Test modules
    try:
        import anthropic
        print("✅ Module anthropic disponible")
    except ImportError:
        print("⚠️  Module anthropic non installé (pip install anthropic)")
    
    # Test clé API
    api_key = os.environ.get("ANTHROPIC_API_KEY")
    if api_key:
        print(f"✅ ANTHROPIC_API_KEY configurée (sk-ant-...{api_key[-4:]})")
    else:
        print("⚠️  ANTHROPIC_API_KEY non configurée")
    
    # Test fichiers
    files_to_check = [
        "chatbot_deliberation.py",
        "pv_students.py"
    ]
    
    for file in files_to_check:
        if os.path.exists(file):
            print(f"✅ {file} trouvé")
        else:
            print(f"❌ {file} manquant")
    
    return True

def main():
    print("═══════════════════════════════════════════════════════════════")
    print("   🧪 TEST DU CHATBOT DE DÉLIBÉRATION")
    print("═══════════════════════════════════════════════════════════════")
    
    success_count = 0
    total_tests = 3
    
    # Test de l'environnement
    if test_environment():
        success_count += 1
    
    # Test de la base de données
    if test_database():
        success_count += 1
    
    # Test du chatbot
    if test_chatbot():
        success_count += 1
    
    print("\n═══════════════════════════════════════════════════════════════")
    print(f"   📊 RÉSULTATS: {success_count}/{total_tests} tests réussis")
    print("═══════════════════════════════════════════════════════════════")
    
    if success_count == total_tests:
        print("🎉 Tous les tests sont passés ! Le chatbot est prêt à utiliser.")
    elif success_count >= 2:
        print("⚠️  Le chatbot fonctionne avec des limitations.")
    else:
        print("❌ Des problèmes importants ont été détectés.")
    
    return success_count == total_tests

if __name__ == "__main__":
    success = main()
    
    if len(sys.argv) > 1 and sys.argv[1] == "--interactive":
        print("\n🎮 Mode interactif activé...")
        db = PVDatabase()
        bot = ChatbotDeliberation(db)
        
        while True:
            try:
                user_input = input("\n💬 Votre question (ou 'quit' pour quitter): ").strip()
                if user_input.lower() in ['quit', 'q', 'exit']:
                    break
                    
                if user_input:
                    res = bot.envoyer_message(user_input)
                    print(f"🤖 {res['texte']}")
                    if res.get('action_ui'):
                        print(f"🎯 Action: {res.get('action_ui')}")
                        
            except KeyboardInterrupt:
                break
            except Exception as e:
                print(f"❌ Erreur: {e}")
        
        print("\n👋 Au revoir !")
    
    sys.exit(0 if success else 1)