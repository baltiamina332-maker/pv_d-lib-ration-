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
public class EtudiantDTO {
    private Integer numOrder;
    private String nomPrenom;
    private String matriculeCin;
    private String classe;
    private Double moyenneGenerale;
    private Integer ectsNonValides;
    private String statutEtudiant; // "ancien" or "nouveau"
    private Double moyenneUe;
    private String decision;
    private String mention;
    private String observation;
    
    @Builder.Default
    private List<String> anomalies = new ArrayList<>();
}
