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
public class PvGenerateRequestDTO {
    private SessionDeliberationDTO sessionInfo;
    @Builder.Default
    private List<ClassePreviewDTO> classesData = new ArrayList<>();
    @Builder.Default
    private List<String> selectedClasses = new ArrayList<>();
}
