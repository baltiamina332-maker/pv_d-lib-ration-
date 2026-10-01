-- ============================================================================
-- SCRIPT COMPLET DE CRÉATION ET D'INITIALISATION POUR PHPMYADMIN / MYSQL
-- BASE DE DONNÉES : pv_deliberation
-- ============================================================================

CREATE DATABASE IF NOT EXISTS `pv_deliberation` DEFAULT CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
USE `pv_deliberation`;

-- ----------------------------------------------------------------------------
-- 1. TABLE UTILISATEURS (users)
-- ----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS `users` (
    `id` INT AUTO_INCREMENT PRIMARY KEY,
    `nom_utilisateur` VARCHAR(50) NOT NULL UNIQUE,
    `mot_de_passe_hash` VARCHAR(255) NOT NULL,
    `role` VARCHAR(50) NOT NULL DEFAULT 'utilisateur',
    `email` VARCHAR(100) DEFAULT '',
    `actif` BOOLEAN DEFAULT TRUE,
    `date_creation` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    `date_modification` TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Insertion des utilisateurs de test (Admin et Enseignant/User)
INSERT INTO `users` (`nom_utilisateur`, `mot_de_passe_hash`, `role`, `email`, `actif`) VALUES
('admin', 'admin', 'admin', 'admin@esprit.tn', 1),
('user', 'user', 'utilisateur', 'user@esprit.tn', 1),
('prof', 'prof', 'utilisateur', 'prof@esprit.tn', 1),
('demo', 'demo', 'admin', 'demo@esprit.tn', 1)
ON DUPLICATE KEY UPDATE `actif` = 1;

-- ----------------------------------------------------------------------------
-- 2. TABLE HISTORIQUE DES DÉLIBÉRATIONS (deliberations)
-- ----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS `deliberations` (
    `id` INT AUTO_INCREMENT PRIMARY KEY,
    `date_deliberation` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `nb_etudiants` INT NOT NULL DEFAULT 0,
    `nb_admis` INT NOT NULL DEFAULT 0,
    `nb_ajournes` INT NOT NULL DEFAULT 0,
    `fichier_pv` VARCHAR(500) NULL,
    `chemin_fichier` VARCHAR(500) NULL,
    `utilisateur_id` INT NULL,
    `classe_groupe` VARCHAR(100) NULL,
    `annee_universitaire` VARCHAR(20) NULL,
    `session_type` VARCHAR(50) NULL DEFAULT 'Principale',
    `moyenne_generale` DECIMAL(5,3) NULL,
    `commentaires` TEXT NULL,
    `date_creation` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    INDEX `idx_date` (`date_deliberation`),
    INDEX `idx_classe` (`classe_groupe`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ----------------------------------------------------------------------------
-- 3. TABLE ÉTUDIANTS (etudiant)
-- ----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS `etudiant` (
    `id_etudiant` INT AUTO_INCREMENT PRIMARY KEY,
    `num_ordre` INT DEFAULT 1,
    `nom` VARCHAR(100),
    `prenom` VARCHAR(100),
    `nom_prenom` VARCHAR(200),
    `matricule` VARCHAR(50),
    `classe_groupe` VARCHAR(50),
    `filiere` VARCHAR(100) DEFAULT '',
    `statut` VARCHAR(50) DEFAULT 'Actif',
    `moyenne_generale` DECIMAL(5,3) DEFAULT 0.000,
    `ects_valides` INT DEFAULT 30,
    `decision` VARCHAR(100) DEFAULT '',
    `mention` VARCHAR(100) DEFAULT '',
    `validation` VARCHAR(50) DEFAULT '',
    `observation` TEXT,
    `date_creation` TIMESTAMP DEFAULT CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ----------------------------------------------------------------------------
-- 4. TABLE DÉCISION / MENTION (decision_mention)
-- ----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS `decision_mention` (
    `id` INT AUTO_INCREMENT PRIMARY KEY,
    `id_etudiant` INT,
    `moyenne` DECIMAL(5,3),
    `decision` VARCHAR(100),
    `mention` VARCHAR(100),
    `date_creation` DATETIME DEFAULT CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ============================================================================
-- VERIFICATION DES TABLES CRÉÉES
-- ============================================================================
SHOW TABLES;
SELECT id, nom_utilisateur, role, email, actif FROM users;
