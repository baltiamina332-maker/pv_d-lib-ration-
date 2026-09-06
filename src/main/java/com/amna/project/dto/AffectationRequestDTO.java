package com.amna.project.dto;

import lombok.AllArgsConstructor;
import lombok.Data;
import lombok.NoArgsConstructor;

@Data
@NoArgsConstructor
@AllArgsConstructor
public class AffectationRequestDTO {
    private Long enseignantId;
    private Long classeId;
    private Integer moduleId;
    private String matiere;
    private String anneeUniversitaire;
}
