package com.amna.project.entities;

import jakarta.persistence.*;
import lombok.AllArgsConstructor;
import lombok.Builder;
import lombok.Data;
import lombok.NoArgsConstructor;

import java.time.LocalDateTime;

@Entity
@Table(name = "historique_generation")
@Data
@NoArgsConstructor
@AllArgsConstructor
@Builder
public class HistoriqueGenerationEntity {

    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;

    private String nomClasse;
    private String filename;
    private LocalDateTime dateGeneration;
    private Integer totalEtudiants;
    private Integer nbAdmis;
    private Integer nbRachats;
    private Integer nbAjournes;
    private Double tauxReussite;
    private String createdBy; // The username of the user who generated the PV
}
