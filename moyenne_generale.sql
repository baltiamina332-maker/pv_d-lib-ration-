-- ============================================================================
-- DONNÉES ÉTUDIANTS AVEC MOYENNE GÉNÉRALE PRÉ-CALCULÉE
-- ============================================================================

-- Créer la table des étudiants si elle n'existe pas
CREATE TABLE IF NOT EXISTS etudiants (
    id_etudiant INT PRIMARY KEY AUTO_INCREMENT,
    nom VARCHAR(100) NOT NULL,
    prenom VARCHAR(100) NOT NULL,
    matricule VARCHAR(50) UNIQUE NOT NULL,
    classe_groupe VARCHAR(50) NOT NULL,
    id_session INT NOT NULL,
    type_session VARCHAR(50) NOT NULL,
    annee_universitaire VARCHAR(50) NOT NULL,
    moyenne_generale DECIMAL(5, 2) NOT NULL,
    ects INT DEFAULT 30,
    statut VARCHAR(50) NOT NULL,
    moyenne_ue DECIMAL(5, 2) DEFAULT 0,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Vider la table avant d'insérer les nouvelles données
DELETE FROM etudiants;

-- Insérer les données étudiants avec MG PRÉ-CALCULÉE
INSERT INTO etudiants (nom, prenom, matricule, classe_groupe, id_session, type_session, annee_universitaire, moyenne_generale, ects, statut, moyenne_ue) VALUES
('Balti', 'Amina', '20231045', '3A40', 1, 'Principale', '2025-2026', 12.43, 10, 'Ancien', 8.5),
('Trabelsi', 'Yasmine', '20231046', '3A40', 1, 'Principale', '2025-2026', 11.9, 18, 'Nouveau', 7.5),
('Dupont', 'Jean', '20231047', '3A40', 1, 'Principale', '2025-2026', 9.8, 15, 'Ancien', 7.0),
('Martin', 'Marie', '20231048', '3A40', 1, 'Principale', '2025-2026', 9.6, 12, 'Nouveau', 6.5),
('Dubois', 'Pierre', '20231049', '3A40', 1, 'Principale', '2025-2026', 9.0, 25, 'Nouveau', 5.0),
('Bernard', 'Sophie', '20231050', '3A40', 1, 'Principale', '2025-2026', 14.5, 30, 'Ancien', 8.0),
('Lefevre', 'Luc', '20231051', '3A40', 1, 'Principale', '2025-2026', 15.2, 8, 'Nouveau', 9.0),
('Moreau', 'Claire', '20231052', '3A40', 1, 'Principale', '2025-2026', 10.5, 20, 'Ancien', 7.8);

-- Vérifier les données insérées
SELECT * FROM etudiants ORDER BY moyenne_generale DESC;

-- Statistiques
SELECT 
    COUNT(*) as total_etudiants,
    AVG(moyenne_generale) as moyenne_mg,
    MIN(moyenne_generale) as min_mg,
    MAX(moyenne_generale) as max_mg,
    SUM(CASE WHEN moyenne_generale >= 10 THEN 1 ELSE 0 END) as nombre_admis,
    SUM(CASE WHEN moyenne_generale < 10 THEN 1 ELSE 0 END) as nombre_ajournes
FROM etudiants;

-- ============================================================================
-- FIN DU SCRIPT
-- ============================================================================
