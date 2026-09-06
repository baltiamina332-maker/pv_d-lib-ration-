package com.amna.project.controllers;

import com.amna.project.dto.MlClassPredictionSummaryDTO;
import com.amna.project.dto.MlModelComparisonDTO;
import com.amna.project.dto.MlPredictionResultDTO;
import com.amna.project.dto.MlStudentFeaturesDTO;
import com.amna.project.services.MachineLearningService;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.*;

import java.util.List;

@RestController
@RequestMapping("/api/ml")
public class MachineLearningController {

    @Autowired
    private MachineLearningService machineLearningService;

    @PostMapping("/predict/decision-tree")
    public ResponseEntity<MlPredictionResultDTO> predictDecisionTree(@RequestBody MlStudentFeaturesDTO features) {
        return ResponseEntity.ok(machineLearningService.predictDecisionTree(features));
    }

    @PostMapping("/predict/knn")
    public ResponseEntity<MlPredictionResultDTO> predictKnn(
            @RequestBody MlStudentFeaturesDTO features,
            @RequestParam(value = "k", defaultValue = "3") int k) {
        return ResponseEntity.ok(machineLearningService.predictKnn(features, k));
    }

    @PostMapping("/predict/random-forest")
    public ResponseEntity<MlPredictionResultDTO> predictRandomForest(@RequestBody MlStudentFeaturesDTO features) {
        return ResponseEntity.ok(machineLearningService.predictRandomForest(features));
    }

    @PostMapping("/compare-all")
    public ResponseEntity<MlModelComparisonDTO> compareAllModels(@RequestBody MlStudentFeaturesDTO features) {
        return ResponseEntity.ok(machineLearningService.compareAllModels(features));
    }

    @GetMapping("/predict/class/{classeId}")
    public ResponseEntity<MlClassPredictionSummaryDTO> predictClassFromDb(@PathVariable Long classeId) {
        MlClassPredictionSummaryDTO result = machineLearningService.predictClassFromDatabase(classeId);
        if (result == null) {
            return ResponseEntity.notFound().build();
        }
        return ResponseEntity.ok(result);
    }

    @PostMapping("/predict/batch")
    public ResponseEntity<MlClassPredictionSummaryDTO> predictBatch(
            @RequestParam(value = "classeName", defaultValue = "Classe") String classeName,
            @RequestBody List<MlStudentFeaturesDTO> students) {
        return ResponseEntity.ok(machineLearningService.predictStudentFeaturesBatch(classeName, null, students));
    }
}
