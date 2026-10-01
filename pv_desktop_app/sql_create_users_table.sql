-- Créer la table des utilisateurs
CREATE TABLE IF NOT EXISTS users (
    id INT AUTO_INCREMENT PRIMARY KEY,
    username VARCHAR(50) NOT NULL UNIQUE,
    password VARCHAR(255) NOT NULL,
    full_name VARCHAR(100) NOT NULL,
    role ENUM('Admin', 'Utilisateur') NOT NULL DEFAULT 'Utilisateur',
    email VARCHAR(100),
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
);

-- Insérer les utilisateurs de test
INSERT INTO users (username, password, full_name, role, email, is_active) VALUES
('admin', 'admin123', 'Administrateur', 'Admin', 'admin@example.com', TRUE),
('user', 'user123', 'Utilisateur Standard', 'Utilisateur', 'user@example.com', TRUE),
('prof', 'prof123', 'Professeur', 'Utilisateur', 'prof@example.com', TRUE);
