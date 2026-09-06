package com.amna.project.dto;

import lombok.AllArgsConstructor;
import lombok.Builder;
import lombok.Data;
import lombok.NoArgsConstructor;

import java.util.ArrayList;
import java.util.List;

@Data
@NoArgsConstructor
@AllArgsConstructor
@Builder
public class ClassePreviewDTO {
    private String nomClasse;
    @Builder.Default
    private List<EtudiantDTO> etudiants = new ArrayList<>();
    private int totalEtudiants;
    private int nbAdmis;
    private int nbAdmisEcts;
    private int nbRachats;
    private int nbConseilEcole;
    private int nbAjournes;
    private double tauxReussite;
    
    @Builder.Default
    private List<String> anomalies = new ArrayList<>();
}
