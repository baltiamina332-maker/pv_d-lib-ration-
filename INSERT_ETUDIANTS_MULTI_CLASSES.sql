-- ============================================================================
-- SCRIPT D'INSERTION D'ÉTUDIANTS MULTI-CLASSES DANS LA BASE DE DONNÉES
-- BASE DE DONNÉES : pv_deliberation
-- ============================================================================

USE `pv_deliberation`;

-- Vider la table etudiant si nécessaire (optionnel)
-- TRUNCATE TABLE `etudiant`;

-- Insérer les étudiants de différentes classes/filières
INSERT INTO `etudiant` 
(`num_ordre`, `nom`, `prenom`, `nom_prenom`, `matricule`, `classe_groupe`, `filiere`, `statut`, `moyenne_generale`, `ects_valides`, `decision`, `mention`, `validation`, `observation`) 
VALUES
-- Classe 3A40 (Génie Informatique)
(1, 'Balti', 'Amina', 'Balti Amina', '20231045', '3A40', 'Génie Informatique', 'Actif', 16.500, 30, 'Admis', 'Très Bien', 'Oui', 'Passage d\'année'),
(2, 'Trabelsi', 'Yasmine', 'Trabelsi Yasmine', '20231046', '3A40', 'Génie Informatique', 'Actif', 14.200, 30, 'Admis', 'Bien', 'Oui', 'Passage d\'année'),
(3, 'Cherni', 'Mohamed', 'Cherni Mohamed', '20231047', '3A40', 'Génie Informatique', 'Actif', 12.800, 30, 'Admis', 'Assez Bien', 'Oui', 'Passage d\'année'),
(4, 'Sassi', 'Fatma', 'Sassi Fatma', '20231048', '3A40', 'Génie Informatique', 'Actif', 11.500, 30, 'Admis', 'Passable', 'Oui', 'Passage d\'année'),
(5, 'Nouri', 'Ahmed', 'Nouri Ahmed', '20231049', '3A40', 'Génie Informatique', 'Actif', 8.750, 18, 'Ajourné', 'Session de rattrapage', 'Non', 'Admis en session de rattrapage'),

-- Classe 4TWIN1 (Technologies Web & Internet)
(1, 'Ben Ali', 'Karim', 'Ben Ali Karim', '20232010', '4TWIN1', 'Web & Internet', 'Actif', 17.200, 30, 'Admis', 'Très Bien', 'Oui', 'Passage d\'année'),
(2, 'Mansouri', 'Salma', 'Mansouri Salma', '20232011', '4TWIN1', 'Web & Internet', 'Actif', 13.900, 30, 'Admis', 'Assez Bien', 'Oui', 'Passage d\'année'),
(3, 'Gharbi', 'Omar', 'Gharbi Omar', '20232012', '4TWIN1', 'Web & Internet', 'Actif', 10.400, 30, 'Admis', 'Passable', 'Oui', 'Passage d\'année'),
(4, 'Hammami', 'Ines', 'Hammami Ines', '20232013', '4TWIN1', 'Web & Internet', 'Actif', 9.100, 20, 'Ajourné', 'Session de rattrapage', 'Non', 'Admis en session de rattrapage'),
(5, 'Jebali', 'Youssef', 'Jebali Youssef', '20232014', '4TWIN1', 'Web & Internet', 'Actif', 7.250, 12, 'Refusé', 'Ajourné', 'Non', 'Exclu / Refusé selon règlement'),

-- Classe 2GL2 (Génie Logiciel)
(1, 'Dridi', 'Sonia', 'Dridi Sonia', '20233001', '2GL2', 'Génie Logiciel', 'Actif', 16.100, 30, 'Admis', 'Très Bien', 'Oui', 'Passage d\'année'),
(2, 'Khelifi', 'Mehdi', 'Khelifi Mehdi', '20233002', '2GL2', 'Génie Logiciel', 'Actif', 15.000, 30, 'Admis', 'Bien', 'Oui', 'Passage d\'année'),
(3, 'Ayari', 'Rim', 'Ayari Rim', '20233003', '2GL2', 'Génie Logiciel', 'Actif', 12.150, 30, 'Admis', 'Assez Bien', 'Oui', 'Passage d\'année'),
(4, 'Toumi', 'Bilel', 'Toumi Bilel', '20233004', '2GL2', 'Génie Logiciel', 'Actif', 8.500, 15, 'Ajourné', 'Session de rattrapage', 'Non', 'Admis en session de rattrapage'),
(5, 'Sellami', 'Mariem', 'Sellami Mariem', '20233005', '2GL2', 'Génie Logiciel', 'Actif', 6.800, 10, 'Refusé', 'Ajourné', 'Non', 'Exclu / Refusé selon règlement'),

-- Classe 5SIM3 (Systèmes d'Information & Mobiles)
(1, 'Bouazizi', 'Skander', 'Bouazizi Skander', '20234020', '5SIM3', 'Systèmes Mobiles', 'Actif', 18.000, 30, 'Admis', 'Très Bien', 'Oui', 'Passage d\'année - Major de promotion'),
(2, 'Mabrouk', 'Nour', 'Mabrouk Nour', '20234021', '5SIM3', 'Systèmes Mobiles', 'Actif', 14.800, 30, 'Admis', 'Bien', 'Oui', 'Passage d\'année'),
(3, 'Ferchichi', 'Hamza', 'Ferchichi Hamza', '20234022', '5SIM3', 'Systèmes Mobiles', 'Actif', 10.800, 30, 'Admis', 'Passable', 'Oui', 'Passage d\'année'),
(4, 'Chaabane', 'Syrine', 'Chaabane Syrine', '20234023', '5SIM3', 'Systèmes Mobiles', 'Actif', 9.300, 22, 'Ajourné', 'Session de rattrapage', 'Non', 'Admis en session de rattrapage'),

-- Classe 1NFIN1 (Tronc Commun Informatique)
(1, 'Zaibi', 'Rayen', 'Zaibi Rayen', '20235005', '1NFIN1', 'Informatique', 'Actif', 15.500, 30, 'Admis', 'Bien', 'Oui', 'Passage d\'année'),
(2, 'Rebai', 'Eya', 'Rebai Eya', '20235006', '1NFIN1', 'Informatique', 'Actif', 12.750, 30, 'Admis', 'Assez Bien', 'Oui', 'Passage d\'année'),
(3, 'Jaziri', 'Walid', 'Jaziri Walid', '20235007', '1NFIN1', 'Informatique', 'Actif', 11.100, 30, 'Admis', 'Passable', 'Oui', 'Passage d\'année'),
(4, 'Zoghlami', 'Khadija', 'Zoghlami Khadija', '20235008', '1NFIN1', 'Informatique', 'Actif', 7.900, 14, 'Refusé', 'Ajourné', 'Non', 'Exclu / Refusé selon règlement');

-- Afficher le nombre d'étudiants par classe
SELECT classe_groupe, COUNT(*) as total_etudiants FROM etudiant GROUP BY classe_groupe;
