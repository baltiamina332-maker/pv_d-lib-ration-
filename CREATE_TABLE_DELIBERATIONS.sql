-- Script SQL pour créer la table 'deliberations' dans la base pv_deliberation
-- À exécuter dans MySQL Workbench ou phpMyAdmin

USE pv_deliberation;

-- Créer la table deliberations si elle n'existe pas
CREATE TABLE IF NOT EXISTS deliberations (
    id INT AUTO_INCREMENT PRIMARY KEY,
    date_deliberation DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    nb_etudiants INT NOT NULL DEFAULT 0,
    nb_admis INT NOT NULL DEFAULT 0,
    nb_ajournes INT NOT NULL DEFAULT 0,
    chemin_fichier VARCHAR(500) NULL,
    utilisateur_id INT NULL,
    classe_groupe VARCHAR(100) NULL,
    annee_universitaire VARCHAR(20) NULL,
    session_type VARCHAR(50) NULL DEFAULT 'Principale',
    moyenne_generale DECIMAL(5,2) NULL,
    commentaires TEXT NULL,
    date_creation TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    date_modification TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    INDEX idx_date (date_deliberation),
    INDEX idx_classe (classe_groupe),
    INDEX idx_annee (annee_universitaire)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Insérer quelques données de test (optionnel)
INSERT INTO deliberations 
(date_deliberation, nb_etudiants, nb_admis, nb_ajournes, classe_groupe, annee_universitaire) 
VALUES 
(NOW(), 0, 0, 0, 'Test', '2025-2026');

-- Vérifier que la table a été créée
SELECT COUNT(*) as nb_records FROM deliberations;
DESCRIBE deliberations;