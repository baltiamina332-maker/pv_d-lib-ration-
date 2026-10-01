package com.amna.project.controllers;

import com.amna.project.dto.AffectationRequestDTO;
import com.amna.project.entities.Affectation;
import com.amna.project.entities.Classe;
import com.amna.project.entities.Etudiant;
import com.amna.project.entities.Module;
import com.amna.project.entities.Role;
import com.amna.project.entities.UserEntity;
import com.amna.project.repositories.AffectationRepository;
import com.amna.project.repositories.ClasseRepository;
import com.amna.project.repositories.EtudiantRepository;
import com.amna.project.repositories.ModuleRepository;
import com.amna.project.repositories.UserRepository;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.http.ResponseEntity;
import org.springframework.security.access.prepost.PreAuthorize;
import org.springframework.web.bind.annotation.*;

import java.util.List;
import java.util.Map;

@RestController
@RequestMapping("/api/admin")
@PreAuthorize("hasAuthority('ROLE_ADMIN')")
public class AdminController {

    @Autowired
    private UserRepository userRepository;

    @Autowired
    private ClasseRepository classeRepository;

    @Autowired
    private AffectationRepository affectationRepository;

    @Autowired
    private EtudiantRepository etudiantRepository;

    @Autowired
    private ModuleRepository moduleRepository;

    // --- User Management ---

    @GetMapping("/users")
    public ResponseEntity<List<UserEntity>> getAllUsers() {
        return ResponseEntity.ok(userRepository.findAll());
    }

    @PostMapping("/users/{id}/approve")
    public ResponseEntity<?> approveUser(@PathVariable Long id) {
        return userRepository.findById(id).map(user -> {
            user.setApproved(true);
            userRepository.save(user);
            return ResponseEntity.ok(Map.of("message", "Utilisateur approuvé avec succès."));
        }).orElse(ResponseEntity.badRequest().body(Map.of("message", "Utilisateur non trouvé.")));
    }

    @PostMapping("/users/{id}/reject")
    public ResponseEntity<?> rejectUser(@PathVariable Long id) {
        return userRepository.findById(id).map(user -> {
            user.setApproved(false);
            userRepository.save(user);
            return ResponseEntity.ok(Map.of("message", "Utilisateur révoqué avec succès."));
        }).orElse(ResponseEntity.badRequest().body(Map.of("message", "Utilisateur non trouvé.")));
    }

    @PutMapping("/users/{id}/email")
    public ResponseEntity<?> updateEmail(@PathVariable Long id, @RequestBody Map<String, String> body) {
        String email = body.getOrDefault("email", "").trim();
        if (!email.isEmpty() && !com.amna.project.services.EmailValidator.isValid(email)) {
            return ResponseEntity.badRequest().body(Map.of("message", "Adresse e-mail invalide."));
        }
        return userRepository.findById(id).map(user -> {
            user.setEmail(email.isEmpty() ? null : email);
            userRepository.save(user);
            return ResponseEntity.ok(Map.of("message", "E-mail de " + user.getUsername() + " mis à jour."));
        }).orElse(ResponseEntity.badRequest().body(Map.of("message", "Utilisateur non trouvé.")));
    }

    @PostMapping("/users/{id}/role")
    public ResponseEntity<?> changeRole(@PathVariable Long id, @RequestBody Map<String, String> body) {
        String roleStr = body.get("role");
        return userRepository.findById(id).map(user -> {
            try {
                user.setRole(Role.valueOf(roleStr));
                userRepository.save(user);
                return ResponseEntity.ok(Map.of("message", "Rôle mis à jour avec succès."));
            } catch (Exception e) {
                return ResponseEntity.badRequest().body(Map.of("message", "Rôle invalide."));
            }
        }).orElse(ResponseEntity.badRequest().body(Map.of("message", "Utilisateur non trouvé.")));
    }

    // --- Classe Management ---

    @GetMapping("/classes")
    public ResponseEntity<List<Classe>> getAllClasses() {
        return ResponseEntity.ok(classeRepository.findAll());
    }

    @PostMapping("/classes")
    public ResponseEntity<?> createClasse(@RequestBody Classe classe) {
        if (classeRepository.existsByNomClasse(classe.getNomClasse())) {
            return ResponseEntity.badRequest().body(Map.of("message", "Cette classe existe déjà."));
        }
        if (classe.getAnneeUniversitaire() == null || classe.getAnneeUniversitaire().isBlank()) {
            classe.setAnneeUniversitaire("2025-2026");
        }
        Classe saved = classeRepository.save(classe);
        return ResponseEntity.ok(saved);
    }

    @DeleteMapping("/classes/{id}")
    public ResponseEntity<?> deleteClasse(@PathVariable Long id) {
        if (!classeRepository.existsById(id)) {
            return ResponseEntity.badRequest().body(Map.of("message", "Classe non trouvée."));
        }
        classeRepository.deleteById(id);
        return ResponseEntity.ok(Map.of("message", "Classe supprimée avec succès."));
    }

    @PutMapping("/classes/{id}")
    public ResponseEntity<?> updateClasse(@PathVariable Long id, @RequestBody Classe classeData) {
        Classe classe = classeRepository.findById(id).orElse(null);
        if (classe == null) {
            return ResponseEntity.badRequest().body(Map.of("message", "Classe non trouvée."));
        }
        if (classeData.getNomClasse() != null && !classeData.getNomClasse().isBlank()) {
            classe.setNomClasse(classeData.getNomClasse());
        }
        if (classeData.getNiveau() != null && !classeData.getNiveau().isBlank()) {
            classe.setNiveau(classeData.getNiveau());
        }
        if (classeData.getAnneeUniversitaire() != null && !classeData.getAnneeUniversitaire().isBlank()) {
            classe.setAnneeUniversitaire(classeData.getAnneeUniversitaire());
        }
        Classe updated = classeRepository.save(classe);
        return ResponseEntity.ok(updated);
    }

    // --- Affectation Management (Enseignant -> Module/Matière -> Classe) ---

    @GetMapping("/affectations")
    public ResponseEntity<List<Affectation>> getAllAffectations() {
        return ResponseEntity.ok(affectationRepository.findAll());
    }

    @PostMapping("/affectations")
    public ResponseEntity<?> createAffectation(@RequestBody AffectationRequestDTO dto) {
        UserEntity enseignant = userRepository.findById(dto.getEnseignantId()).orElse(null);
        if (enseignant == null) {
            return ResponseEntity.badRequest().body(Map.of("message", "Enseignant introuvable."));
        }

        Classe classe = classeRepository.findById(dto.getClasseId()).orElse(null);
        if (classe == null) {
            return ResponseEntity.badRequest().body(Map.of("message", "Classe introuvable."));
        }

        Module module = null;
        if (dto.getModuleId() != null) {
            module = moduleRepository.findById(dto.getModuleId()).orElse(null);
        } else if (dto.getMatiere() != null) {
            module = moduleRepository.findByNomModule(dto.getMatiere()).orElse(null);
        }

        Affectation affectation = Affectation.builder()
                .enseignant(enseignant)
                .classe(classe)
                .module(module)
                .matiere(dto.getMatiere() != null ? dto.getMatiere() : (module != null ? module.getNomModule() : ""))
                .anneeUniversitaire(dto.getAnneeUniversitaire() != null ? dto.getAnneeUniversitaire() : "2025-2026")
                .build();

        Affectation saved = affectationRepository.save(affectation);
        return ResponseEntity.ok(saved);
    }

    @PutMapping("/affectations/{id}")
    public ResponseEntity<?> updateAffectation(@PathVariable Long id, @RequestBody AffectationRequestDTO dto) {
        Affectation affectation = affectationRepository.findById(id).orElse(null);
        if (affectation == null) {
            return ResponseEntity.badRequest().body(Map.of("message", "Affectation introuvable."));
        }

        UserEntity enseignant = userRepository.findById(dto.getEnseignantId()).orElse(null);
        if (enseignant == null) {
            return ResponseEntity.badRequest().body(Map.of("message", "Enseignant introuvable."));
        }

        Classe classe = classeRepository.findById(dto.getClasseId()).orElse(null);
        if (classe == null) {
            return ResponseEntity.badRequest().body(Map.of("message", "Classe introuvable."));
        }

        Module module = null;
        if (dto.getModuleId() != null) {
            module = moduleRepository.findById(dto.getModuleId()).orElse(null);
        } else if (dto.getMatiere() != null) {
            module = moduleRepository.findByNomModule(dto.getMatiere()).orElse(null);
        }

        affectation.setEnseignant(enseignant);
        affectation.setClasse(classe);
        affectation.setModule(module);
        affectation.setMatiere(dto.getMatiere() != null ? dto.getMatiere() : (module != null ? module.getNomModule() : ""));
        if (dto.getAnneeUniversitaire() != null && !dto.getAnneeUniversitaire().isBlank()) {
            affectation.setAnneeUniversitaire(dto.getAnneeUniversitaire());
        }

        Affectation updated = affectationRepository.save(affectation);
        return ResponseEntity.ok(updated);
    }

    @DeleteMapping("/affectations/{id}")
    public ResponseEntity<?> deleteAffectation(@PathVariable Long id) {
        if (!affectationRepository.existsById(id)) {
            return ResponseEntity.badRequest().body(Map.of("message", "Affectation introuvable."));
        }
        affectationRepository.deleteById(id);
        return ResponseEntity.ok(Map.of("message", "Affectation supprimée avec succès."));
    }

    // --- Student Assignment to Classe ---

    @GetMapping("/etudiants")
    public ResponseEntity<List<Etudiant>> getAllEtudiants() {
        return ResponseEntity.ok(etudiantRepository.findAll());
    }

    @PostMapping("/etudiants/assign-classe")
    public ResponseEntity<?> assignEtudiantToClasse(@RequestBody Map<String, Object> body) {
        Integer etudiantId = Integer.parseInt(body.get("etudiantId").toString());
        Long classeId = Long.parseLong(body.get("classeId").toString());

        Etudiant etudiant = etudiantRepository.findById(etudiantId).orElse(null);
        Classe classe = classeRepository.findById(classeId).orElse(null);

        if (etudiant == null || classe == null) {
            return ResponseEntity.badRequest().body(Map.of("message", "Étudiant ou Classe non trouvé."));
        }

        etudiant.setClasse(classe);
        etudiantRepository.save(etudiant);
        return ResponseEntity.ok(Map.of("message", "Étudiant affecté à la classe avec succès."));
    }

    @PostMapping("/etudiants")
    public ResponseEntity<?> createEtudiant(@RequestBody Map<String, Object> body) {
        try {
            String nom = body.get("nom") != null ? body.get("nom").toString() : "";
            String prenom = body.get("prenom") != null ? body.get("prenom").toString() : "";

            if (nom.isBlank() || prenom.isBlank()) {
                return ResponseEntity.badRequest().body(Map.of("message", "Le Nom et le Prénom de l'étudiant sont obligatoires."));
            }

            Integer idEtudiant = null;
            if (body.get("idEtudiant") != null && !body.get("idEtudiant").toString().isBlank()) {
                idEtudiant = Integer.parseInt(body.get("idEtudiant").toString());
            } else {
                idEtudiant = etudiantRepository.findAll().stream()
                        .mapToInt(Etudiant::getIdEtudiant)
                        .max()
                        .orElse(0) + 1;
            }

            Long classeId = body.get("classeId") != null && !body.get("classeId").toString().isBlank() 
                    ? Long.parseLong(body.get("classeId").toString()) : null;

            Classe classe = (classeId != null) ? classeRepository.findById(classeId).orElse(null) : null;

            Etudiant etudiant = Etudiant.builder()
                    .idEtudiant(idEtudiant)
                    .nom(nom)
                    .prenom(prenom)
                    .classe(classe)
                    .build();

            Etudiant saved = etudiantRepository.save(etudiant);
            return ResponseEntity.ok(saved);
        } catch (Exception e) {
            return ResponseEntity.badRequest().body(Map.of("message", "Erreur lors de la création de l'étudiant: " + e.getMessage()));
        }
    }

    @PutMapping("/etudiants/{id}")
    public ResponseEntity<?> updateEtudiant(@PathVariable Integer id, @RequestBody Map<String, Object> body) {
        Etudiant etudiant = etudiantRepository.findById(id).orElse(null);
        if (etudiant == null) {
            return ResponseEntity.badRequest().body(Map.of("message", "Étudiant non trouvé."));
        }
        if (body.containsKey("nom") && body.get("nom") != null) {
            etudiant.setNom(body.get("nom").toString());
        }
        if (body.containsKey("prenom") && body.get("prenom") != null) {
            etudiant.setPrenom(body.get("prenom").toString());
        }
        if (body.containsKey("classeId")) {
            Long classeId = body.get("classeId") != null && !body.get("classeId").toString().isBlank()
                    ? Long.parseLong(body.get("classeId").toString()) : null;
            Classe classe = (classeId != null) ? classeRepository.findById(classeId).orElse(null) : null;
            etudiant.setClasse(classe);
        }
        Etudiant updated = etudiantRepository.save(etudiant);
        return ResponseEntity.ok(updated);
    }

    @DeleteMapping("/etudiants/{id}")
    public ResponseEntity<?> deleteEtudiant(@PathVariable Integer id) {
        if (!etudiantRepository.existsById(id)) {
            return ResponseEntity.badRequest().body(Map.of("message", "Étudiant non trouvé."));
        }
        etudiantRepository.deleteById(id);
        return ResponseEntity.ok(Map.of("message", "Étudiant supprimé avec succès."));
    }

    @GetMapping("/modules")
    public ResponseEntity<List<Module>> getAllModules() {
        return ResponseEntity.ok(moduleRepository.findAll());
    }
}
