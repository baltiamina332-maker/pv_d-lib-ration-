-- ============================================================================
-- DATABASE SETUP FOR EMAIL FIELD FUNCTIONALITY
-- ============================================================================

-- 1. CHECK IF EMAIL COLUMN EXISTS IN USERS TABLE
-- If it doesn't exist, this will add it

ALTER TABLE users ADD COLUMN email VARCHAR(255) NULL DEFAULT '';

-- 2. VERIFY USERS TABLE STRUCTURE
-- Expected columns: id, nom_utilisateur, mot_de_passe_hash, role, email, actif

-- 3. CREATE TEST DATA WITH EMAIL (if needed)
-- Uncomment the INSERT statements below to add test users

-- INSERT INTO users (nom_utilisateur, mot_de_passe_hash, role, email, actif) 
-- VALUES ('admin', 'admin123', 'admin', 'admin@esprit.tn', 1);

-- INSERT INTO users (nom_utilisateur, mot_de_passe_hash, role, email, actif) 
-- VALUES ('user', 'user123', 'utilisateur', 'user@esprit.tn', 1);

-- 4. UPDATE EXISTING USERS WITH EMAIL (if they don't have one)
-- UPDATE users SET email = CONCAT(nom_utilisateur, '@esprit.tn') 
-- WHERE email IS NULL OR email = '';

-- ============================================================================
-- VERIFY SETUP
-- ============================================================================

-- Check if email column was added successfully
SELECT COLUMN_NAME, COLUMN_TYPE, IS_NULLABLE 
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'users' AND COLUMN_NAME = 'email';

-- Check all users with their emails
SELECT id, nom_utilisateur, role, email, actif FROM users;

-- ============================================================================
-- END OF SETUP SCRIPT
-- ============================================================================
