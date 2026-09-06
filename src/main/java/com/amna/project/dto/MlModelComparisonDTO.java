package com.amna.project.dto;

import lombok.AllArgsConstructor;
import lombok.Builder;
import lombok.Data;
import lombok.NoArgsConstructor;

import java.util.Map;

@Data
@Builder
@NoArgsConstructor
@AllArgsConstructor
public class MlModelComparisonDTO {
    private MlPredictionResultDTO decisionTreeResult;
    private MlPredictionResultDTO knnResult;
    private MlPredictionResultDTO randomForestResult;
    private String consensusDecision;
    private Double globalAgreementPercentage;
    private Map<String, ModelPerformanceMetrics> modelMetrics;

    @Data
    @Builder
    @NoArgsConstructor
    @AllArgsConstructor
    public static class ModelPerformanceMetrics {
        private Double accuracy;
        private Double precision;
        private Double recall;
        private Double f1Score;
    }
}
