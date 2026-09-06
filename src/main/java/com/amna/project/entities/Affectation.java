package com.amna.project.entities;

import jakarta.persistence.*;
import lombok.*;

@Entity
@Table(name = "AFFECTATIONS")
@Data
@NoArgsConstructor
@AllArgsConstructor
@Builder
public class Affectation {

    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;

    @ManyToOne(fetch = FetchType.EAGER)
    @JoinColumn(name = "enseignant_id", nullable = false)
    private UserEntity enseignant;

    @ManyToOne(fetch = FetchType.EAGER)
    @JoinColumn(name = "classe_id", nullable = false)
    private Classe classe;

    @ManyToOne(fetch = FetchType.EAGER)
    @JoinColumn(name = "module_id")
    private Module module;

    @Column(name = "matiere", nullable = false)
    private String matiere; // e.g. "Angular", "Spring Boot", "DevOps"

    @Column(name = "annee_universitaire")
    private String anneeUniversitaire; // e.g. "2025-2026"

    public String getMatiere() {
        if (module != null && module.getNomModule() != null) {
            return module.getNomModule();
        }
        return matiere;
    }
}
