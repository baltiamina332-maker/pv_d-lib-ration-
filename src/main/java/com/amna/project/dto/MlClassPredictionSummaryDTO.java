package com.amna.project.dto;

import lombok.AllArgsConstructor;
import lombok.Builder;
import lombok.Data;
import lombok.NoArgsConstructor;

import java.util.List;

@Data
@Builder
@NoArgsConstructor
@AllArgsConstructor
public class MlClassPredictionSummaryDTO {
    private String classeName;
    private Long classeId;
    private int totalStudents;
    private int admisCount;
    private int rachatCount;
    private int ajourneCount;
    private double predictedSuccessRate;
    private double averageConfidence;
    private List<MlStudentPredictionDetailDTO> studentPredictions;

    @Data
    @Builder
    @NoArgsConstructor
    @AllArgsConstructor
    public static class MlStudentPredictionDetailDTO {
        private String matriculeCin;
        private String nomPrenom;
        private double moyenneGenerale;
        private int ectsNonValides;
        private String decisionTreeDecision;
        private String knnDecision;
        private String randomForestDecision;
        private String consensusDecision;
        private double confidence;
        private String riskLevel; // FAIBLE, MOYEN, ELEVE
        private String recommendation;
    }
}
