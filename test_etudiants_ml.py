#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Script pour créer un fichier Excel de test avec des étudiants
pour démontrer le Dashboard IA à 3 modèles ML
"""

import pandas as pd
import numpy as np
from datetime import datetime
import os

def create_test_excel():
    """Créer un fichier Excel de test avec des étudiants variés"""
    
    # Données de test avec différentes situations
    etudiants = [
        # Étudiants avec de bonnes moyennes (devrait être Admis)
        {"NumeroOrdre": 1, "Matricule": "20231001", "NomPrenom": "Balti Amina", "ClasseGroupe": "3A40", "MoyenneGenerale": 16.50, "EctsValides": 30},
        {"NumeroOrdre": 2, "Matricule": "20231002", "NomPrenom": "Trabelsi Yasmine", "ClasseGroupe": "3A40", "MoyenneGenerale": 15.20, "EctsValides": 30},
        {"NumeroOrdre": 3, "Matricule": "20231003", "NomPrenom": "Ben Ali Mohamed", "ClasseGroupe": "3A40", "MoyenneGenerale": 14.75, "EctsValides": 30},
        {"NumeroOrdre": 4, "Matricule": "20231004", "NomPrenom": "Gharbi Sarra", "ClasseGroupe": "3A40", "MoyenneGenerale": 13.90, "EctsValides": 30},
        {"NumeroOrdre": 5, "Matricule": "20231005", "NomPrenom": "Mansour Ahmed", "ClasseGroupe": "3A40", "MoyenneGenerale": 12.50, "EctsValides": 30},
        
        # Étudiants moyens (limites d'admission)
        {"NumeroOrdre": 6, "Matricule": "20231006", "NomPrenom": "Jebali Fatma", "ClasseGroupe": "3A40", "MoyenneGenerale": 10.10, "EctsValides": 30},
        {"NumeroOrdre": 7, "Matricule": "20231007", "NomPrenom": "Nouri Omar", "ClasseGroupe": "3A40", "MoyenneGenerale": 9.95, "EctsValides": 30},
        {"NumeroOrdre": 8, "Matricule": "20231008", "NomPrenom": "Cherni Leila", "ClasseGroupe": "3A40", "MoyenneGenerale": 10.25, "EctsValides": 24},
        
        # Étudiants en difficulté (Rattrapage/Ajourné)
        {"NumeroOrdre": 9, "Matricule": "20231009", "NomPrenom": "Dridi Youssef", "ClasseGroupe": "3A40", "MoyenneGenerale": 8.75, "EctsValides": 18},
        {"NumeroOrdre": 10, "Matricule": "20231010", "NomPrenom": "Sassi Monia", "ClasseGroupe": "3A40", "MoyenneGenerale": 9.20, "EctsValides": 20},
        {"NumeroOrdre": 11, "Matricule": "20231011", "NomPrenom": "Kouki Walid", "ClasseGroupe": "3A40", "MoyenneGenerale": 8.50, "EctsValides": 15},
        
        # Étudiants très faibles (Exclu)
        {"NumeroOrdre": 12, "Matricule": "20231012", "NomPrenom": "Bouali Rim", "ClasseGroupe": "3A40", "MoyenneGenerale": 7.25, "EctsValides": 12},
        {"NumeroOrdre": 13, "Matricule": "20231013", "NomPrenom": "Zouari Karim", "ClasseGroupe": "3A40", "MoyenneGenerale": 6.80, "EctsValides": 10},
        
        # Classe 4TWIN1 pour diversité
        {"NumeroOrdre": 14, "Matricule": "20232001", "NomPrenom": "Hamdi Salma", "ClasseGroupe": "4TWIN1", "MoyenneGenerale": 17.20, "EctsValides": 30},
        {"NumeroOrdre": 15, "Matricule": "20232002", "NomPrenom": "Rebai Mehdi", "ClasseGroupe": "4TWIN1", "MoyenneGenerale": 11.40, "EctsValides": 30},
        {"NumeroOrdre": 16, "Matricule": "20232003", "NomPrenom": "Turki Ines", "ClasseGroupe": "4TWIN1", "MoyenneGenerale": 9.60, "EctsValides": 22},
        
        # Classe 2GL2 pour plus de diversité
        {"NumeroOrdre": 17, "Matricule": "20233001", "NomPrenom": "Hajji Rami", "ClasseGroupe": "2GL2", "MoyenneGenerale": 15.80, "EctsValides": 30},
        {"NumeroOrdre": 18, "Matricule": "20233002", "NomPrenom": "Miled Nour", "ClasseGroupe": "2GL2", "MoyenneGenerale": 10.75, "EctsValides": 30},
        {"NumeroOrdre": 19, "Matricule": "20233003", "NomPrenom": "Ouali Sami", "ClasseGroupe": "2GL2", "MoyenneGenerale": 8.90, "EctsValides": 18},
        {"NumeroOrdre": 20, "Matricule": "20233004", "NomPrenom": "Kacem Lilia", "ClasseGroupe": "2GL2", "MoyenneGenerale": 7.50, "EctsValides": 12},
    ]
    
    # Créer le DataFrame
    df = pd.DataFrame(etudiants)
    
    # Ajouter des colonnes calculées pour enrichir les données
    df['Decision'] = df.apply(lambda row: 
        'Admis' if row['MoyenneGenerale'] >= 10.0 and row['EctsValides'] >= 20 
        else ('Ajourné' if row['MoyenneGenerale'] >= 8.0 else 'Exclu'), axis=1)
    
    df['Mention'] = df.apply(lambda row:
        'Très Bien' if row['MoyenneGenerale'] >= 16.0 
        else ('Bien' if row['MoyenneGenerale'] >= 14.0 
              else ('Assez Bien' if row['MoyenneGenerale'] >= 12.0 
                    else ('Passable' if row['MoyenneGenerale'] >= 10.0 else ''))), axis=1)
    
    df['Validation'] = df.apply(lambda row: 'Validé' if row['Decision'] == 'Admis' else 'Non Validé', axis=1)
    df['Observation'] = df.apply(lambda row: 
        f"Excellent niveau ({row['MoyenneGenerale']:.2f}/20)" if row['MoyenneGenerale'] >= 15.0
        else (f"Niveau satisfaisant ({row['MoyenneGenerale']:.2f}/20)" if row['MoyenneGenerale'] >= 10.0
              else f"Difficultés ({row['MoyenneGenerale']:.2f}/20) - {row['EctsValides']} ECTS"), axis=1)
    
    # Nom du fichier avec horodatage
    filename = f"test_etudiants_ml_{datetime.now().strftime('%Y%m%d_%H%M%S')}.xlsx"
    
    try:
        # Créer le fichier Excel
        df.to_excel(filename, index=False, sheet_name='Etudiants')
        
        print(f"✅ Fichier Excel créé avec succès : {filename}")
        print(f"📊 {len(df)} étudiants générés")
        print(f"📁 Chemin complet : {os.path.abspath(filename)}")
        print()
        print("📈 Répartition des données :")
        print(f"   • Admis : {len(df[df['Decision'] == 'Admis'])}")
        print(f"   • Ajournés : {len(df[df['Decision'] == 'Ajourné'])}")
        print(f"   • Exclus : {len(df[df['Decision'] == 'Exclu'])}")
        print()
        print("🎯 Classes représentées :")
        for classe in df['ClasseGroupe'].unique():
            count = len(df[df['ClasseGroupe'] == classe])
            print(f"   • {classe} : {count} étudiants")
        print()
        print("🤖 Maintenant, vous pouvez :")
        print("   1. Ouvrir l'application DesktopApp")
        print("   2. Aller dans l'onglet 'Dashboard IA'")
        print(f"   3. Cliquer sur 'Charger Fichier Excel' et sélectionner {filename}")
        print("   4. Cliquer sur 'Exécuter Analyse ML' pour voir les 3 modèles")
        print()
        print("🎉 Les 3 modèles ML analyseront automatiquement les données !")
        
        return filename
        
    except ImportError as e:
        print(f"❌ Erreur : Module manquant - {e}")
        print("💡 Solution : pip install pandas openpyxl")
        return None
    except Exception as e:
        print(f"❌ Erreur lors de la création du fichier : {e}")
        return None

if __name__ == "__main__":
    create_test_excel()