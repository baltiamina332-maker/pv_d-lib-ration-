package com.amna.project.config;

import com.amna.project.entities.Affectation;
import com.amna.project.entities.Classe;
import com.amna.project.entities.Etudiant;
import com.amna.project.entities.Module;
import com.amna.project.entities.Role;
import com.amna.project.entities.SessionExam;
import com.amna.project.entities.UserEntity;
import com.amna.project.repositories.AffectationRepository;
import com.amna.project.repositories.ClasseRepository;
import com.amna.project.repositories.EtudiantRepository;
import com.amna.project.repositories.ModuleRepository;
import com.amna.project.repositories.SessionExamRepository;
import com.amna.project.repositories.UserRepository;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.CommandLineRunner;
import org.springframework.security.crypto.password.PasswordEncoder;
import org.springframework.stereotype.Component;

import java.math.BigDecimal;
import java.util.List;

@Component
public class DataInitializer implements CommandLineRunner {

    @Autowired
    private UserRepository userRepository;

    @Autowired
    private ClasseRepository classeRepository;

    @Autowired
    private EtudiantRepository etudiantRepository;

    @Autowired
    private AffectationRepository affectationRepository;

    @Autowired
    private ModuleRepository moduleRepository;

    @Autowired
    private SessionExamRepository sessionExamRepository;

    @Autowired
    private PasswordEncoder passwordEncoder;

    @Override
    public void run(String... args) throws Exception {
        // 0. Initialisation des Sessions d'Examen (idSession = 1: Principale)
        if (sessionExamRepository.count() == 0) {
            SessionExam s1 = new SessionExam(1, "Session Principale");
            SessionExam s2 = new SessionExam(2, "Session Rattrapage");
            sessionExamRepository.saveAll(List.of(s1, s2));
        }

        // 1. Initialisation de l'Administrateur par défaut (Garantir isApproved = true & ROLE_ADMIN)
        UserEntity admin = userRepository.findByUsername("admin").orElse(null);
        if (admin == null) {
            admin = UserEntity.builder()
                    .username("admin")
                    .password(passwordEncoder.encode("admin123"))
                    .role(Role.ROLE_ADMIN)
                    .isApproved(true)
                    .build();
        } else {
            admin.setApproved(true);
            admin.setRole(Role.ROLE_ADMIN);
            admin.setPassword(passwordEncoder.encode("admin123"));
        }
        userRepository.save(admin);

        // 2. Initialisation de l'Enseignant "Foulen" (Garantir isApproved = true)
        UserEntity foulen = userRepository.findByUsername("Foulen").orElse(null);
        if (foulen == null) {
            foulen = UserEntity.builder()
                    .username("Foulen")
                    .password(passwordEncoder.encode("password123"))
                    .role(Role.ROLE_USER)
                    .isApproved(true)
                    .build();
        } else {
            foulen.setApproved(true);
            foulen.setPassword(passwordEncoder.encode("password123"));
        }
        userRepository.save(foulen);

        // 3. Initialisation de l'Enseignant par défaut "user"
        UserEntity user = userRepository.findByUsername("user").orElse(null);
        if (user == null) {
            user = UserEntity.builder()
                    .username("user")
                    .password(passwordEncoder.encode("user123"))
                    .role(Role.ROLE_USER)
                    .isApproved(true)
                    .build();
        } else {
            user.setApproved(true);
            user.setPassword(passwordEncoder.encode("user123"));
        }
        userRepository.save(user);

        // 4. Initialisation des Modules dans la table 'module'
        Module modAngular = moduleRepository.findByNomModule("Angular").orElse(null);
        if (modAngular == null) {
            modAngular = new Module();
            modAngular.setIdModule(1);
            modAngular.setNomModule("Angular");
            modAngular.setEnseignant("Foulen");
            modAngular.setCoef(BigDecimal.valueOf(3.0));
            modAngular = moduleRepository.save(modAngular);
        }

        Module modSpringBoot = moduleRepository.findByNomModule("Angular & Spring Boot").orElse(null);
        if (modSpringBoot == null) {
            modSpringBoot = new Module();
            modSpringBoot.setIdModule(2);
            modSpringBoot.setNomModule("Angular & Spring Boot");
            modSpringBoot.setEnseignant("user");
            modSpringBoot.setCoef(BigDecimal.valueOf(4.0));
            modSpringBoot = moduleRepository.save(modSpringBoot);
        }

        Module modMySQL = moduleRepository.findByNomModule("Bases de Données MySQL").orElse(null);
        if (modMySQL == null) {
            modMySQL = new Module();
            modMySQL.setIdModule(3);
            modMySQL.setNomModule("Bases de Données MySQL");
            modMySQL.setEnseignant("Foulen");
            modMySQL.setCoef(BigDecimal.valueOf(3.0));
            modMySQL = moduleRepository.save(modMySQL);
        }

        // 5. Initialisation des Classes
        Classe classe4SAE = classeRepository.findByNomClasse("4 SAE").orElse(null);
        if (classe4SAE == null) {
            classe4SAE = classeRepository.save(Classe.builder()
                    .nomClasse("4 SAE")
                    .niveau("4ème Année")
                    .anneeUniversitaire("2025-2026")
                    .build());
        }

        Classe classe3LSI = classeRepository.findByNomClasse("3 LSI").orElse(null);
        if (classe3LSI == null) {
            classe3LSI = classeRepository.save(Classe.builder()
                    .nomClasse("3 LSI")
                    .niveau("3ème Année")
                    .anneeUniversitaire("2025-2026")
                    .build());
        }

        // 6. Initialisation des Étudiants de "4 SAE"
        if (etudiantRepository.count() == 0) {
            etudiantRepository.saveAll(List.of(
                    Etudiant.builder().idEtudiant(101).nom("Ben Ali").prenom("Ahmed").classe(classe4SAE).build(),
                    Etudiant.builder().idEtudiant(102).nom("Trabelsi").prenom("Sarra").classe(classe4SAE).build(),
                    Etudiant.builder().idEtudiant(103).nom("Gharbi").prenom("Mohamed").classe(classe4SAE).build(),
                    Etudiant.builder().idEtudiant(104).nom("Ayari").prenom("Mariem").classe(classe4SAE).build(),
                    Etudiant.builder().idEtudiant(201).nom("Kacem").prenom("Youssef").classe(classe3LSI).build()
            ));
        }

        // 7. Initialisation ou mise à jour des Affectations avec module_id lié
        if (affectationRepository.count() == 0) {
            affectationRepository.save(Affectation.builder()
                    .enseignant(foulen)
                    .classe(classe4SAE)
                    .module(modAngular)
                    .matiere("Angular")
                    .anneeUniversitaire("2025-2026")
                    .build());

            affectationRepository.save(Affectation.builder()
                    .enseignant(user)
                    .classe(classe4SAE)
                    .module(modSpringBoot)
                    .matiere("Angular & Spring Boot")
                    .anneeUniversitaire("2025-2026")
                    .build());

            affectationRepository.save(Affectation.builder()
                    .enseignant(foulen)
                    .classe(classe3LSI)
                    .module(modMySQL)
                    .matiere("Bases de Données MySQL")
                    .anneeUniversitaire("2025-2026")
                    .build());
        } else {
            // Update existing affectation records to bind module_id if missing
            List<Affectation> list = affectationRepository.findAll();
            for (Affectation aff : list) {
                if (aff.getModule() == null) {
                    if ("Angular".equalsIgnoreCase(aff.getMatiere())) {
                        aff.setModule(modAngular);
                    } else if ("Angular & Spring Boot".equalsIgnoreCase(aff.getMatiere())) {
                        aff.setModule(modSpringBoot);
                    } else if ("Bases de Données MySQL".equalsIgnoreCase(aff.getMatiere())) {
                        aff.setModule(modMySQL);
                    }
                    affectationRepository.save(aff);
                }
            }
        }
    }
}
