package com.amna.project.entities;

import jakarta.persistence.*;
import lombok.*;

@Entity
@Table(name = "CLASSES")
@Data
@NoArgsConstructor
@AllArgsConstructor
@Builder
public class Classe {

    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;

    @Column(name = "nom_classe", nullable = false, unique = true)
    private String nomClasse; // e.g. "4 SAE", "3 LSI"

    private String niveau; // e.g. "4ème Année"
    private String anneeUniversitaire; // e.g. "2025-2026"
}
