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
public class ValidationReportDTO {
    private boolean valid;
    private int totalClasses;
    @Builder.Default
    private List<ClassePreviewDTO> classes = new ArrayList<>();
    @Builder.Default
    private List<String> errors = new ArrayList<>();
    @Builder.Default
    private List<String> warnings = new ArrayList<>();
}
