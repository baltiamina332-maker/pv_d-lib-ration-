package com.amna.project.dto;

import lombok.AllArgsConstructor;
import lombok.Builder;
import lombok.Data;
import lombok.NoArgsConstructor;

@Data
@NoArgsConstructor
@AllArgsConstructor
@Builder
public class EnseignantStatsDTO {
    private String nomClasse;
    private String matiere;
    private int totalEtudiants;
    private int etudiantsEvaluesCount;
    private double moyenneClasse;
    private double noteMin;
    private double noteMax;
    private double tauxReussite; // Percentage >= 10.0
    private int countMoins8;
    private int count8A10;
    private int count10A14;
    private int countPlus14;
}
