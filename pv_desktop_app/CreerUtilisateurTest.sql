-- Script pour créer un utilisateur de test dans la base de données
-- À exécuter dans MySQL pour permettre la connexion

-- Créer la table users si elle n'existe pas
CREATE TABLE IF NOT EXISTS users (
    id INT PRIMARY KEY AUTO_INCREMENT,
    username VARCHAR(50) NOT NULL UNIQUE,
    password_hash VARCHAR(255) NOT NULL,
    full_name VARCHAR(100) NOT NULL,
    email VARCHAR(100),
    role ENUM('admin', 'user', 'viewer') DEFAULT 'user',
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    is_active BOOLEAN DEFAULT TRUE
);

-- Insérer un utilisateur de test : admin / admin
-- Le mot de passe 'admin' sera haché par l'application
INSERT IGNORE INTO users (username, password_hash, full_name, email, role) 
VALUES 
    ('admin', 'admin', 'Administrateur', 'admin@example.com', 'admin'),
    ('test', 'test', 'Utilisateur Test', 'test@example.com', 'user'),
    ('demo', 'demo', 'Utilisateur Demo', 'demo@example.com', 'user');

-- Vérifier les utilisateurs créés
SELECT * FROM users;