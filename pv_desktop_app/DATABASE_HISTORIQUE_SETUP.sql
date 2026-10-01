-- ============================================================================
-- SETUP TABLE HISTORIQUE (DELIBERATIONS)
-- ============================================================================

-- Créer la table deliberations si elle n'existe pas
CREATE TABLE IF NOT EXISTS deliberations (
    id INT PRIMARY KEY AUTO_INCREMENT,
    date_deliberation DATETIME DEFAULT CURRENT_TIMESTAMP,
    nb_etudiants INT NOT NULL DEFAULT 0,
    nb_admis INT NOT NULL DEFAULT 0,
    nb_ajournes INT NOT NULL DEFAULT 0,
    fichier_pv VARCHAR(500) NOT NULL,
    utilisateur_id INT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    INDEX idx_date (date_deliberation),
    INDEX idx_utilisateur (utilisateur_id)
);

-- Vérifier que la table a bien été créée
SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_SCHEMA = 'pv_deliberation' AND TABLE_NAME = 'deliberations';

-- ============================================================================
-- INSERT TEST DATA (Optional)
-- ============================================================================

-- Décommenter pour ajouter des données de test
/*
INSERT INTO deliberations (date_deliberation, nb_etudiants, nb_admis, nb_ajournes, fichier_pv, utilisateur_id)
VALUES 
    (DATE_SUB(NOW(), INTERVAL 5 DAY), 25, 20, 5, '/path/to/PV_1A_2026-07-18.docx', 1),
    (DATE_SUB(NOW(), INTERVAL 3 DAY), 30, 28, 2, '/path/to/PV_2A_2026-07-20.docx', 1),
    (NOW(), 22, 19, 3, '/path/to/PV_3A_2026-07-23.docx', 1);
*/

-- ============================================================================
-- VERIFY SETUP
-- ============================================================================

-- Check table structure
DESCRIBE deliberations;

-- Check table content
SELECT * FROM deliberations ORDER BY date_deliberation DESC;
