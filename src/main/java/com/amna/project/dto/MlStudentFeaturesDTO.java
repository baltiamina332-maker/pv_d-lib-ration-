package com.amna.project.dto;

import lombok.AllArgsConstructor;
import lombok.Builder;
import lombok.Data;
import lombok.NoArgsConstructor;

@Data
@Builder
@NoArgsConstructor
@AllArgsConstructor
public class MlStudentFeaturesDTO {
    private String matriculeCin;
    private String nomPrenom;
    private Double moyenneGenerale;
    private Integer ectsNonValides;
    private String statutEtudiant; // "nouveau" ou "ancien"
    private Double moyenneUe;
    private Double noteCc;
    private Double noteTp;
    private Double noteExam;
}
