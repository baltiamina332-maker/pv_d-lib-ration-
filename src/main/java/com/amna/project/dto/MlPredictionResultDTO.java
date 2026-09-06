package com.amna.project.dto;

import lombok.AllArgsConstructor;
import lombok.Builder;
import lombok.Data;
import lombok.NoArgsConstructor;

import java.util.List;
import java.util.Map;

@Data
@Builder
@NoArgsConstructor
@AllArgsConstructor
public class MlPredictionResultDTO {
    private String modelName;           // "Arbre de Décision", "K-Plus Proches Voisins (KNN)", "Forêt Aléatoire (Random Forest)"
    private String decisionPredite;      // "Admis", "Admis (Rachat)", "Admis avec Réserve ECTS", "Conseil École", "Ajourné / Redouble"
    private Double confidenceScore;      // e.g. 96.5%
    private String mentionPredite;       // "Très Bien", "Bien", "Assez Bien", "Passable", "Ajourné"
    private String explicationRegle;     // Detailed AI explanation
    private Map<String, Double> probabilitesClasses; // Probability distribution per outcome
    private List<String> decisionPath;   // For Decision Tree: sequence of node decisions
    private Map<String, Double> featureImportances; // For Random Forest
    private List<String> kNearestNeighbors; // For KNN
}
