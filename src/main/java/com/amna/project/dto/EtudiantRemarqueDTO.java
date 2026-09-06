package com.amna.project.dto;

import lombok.AllArgsConstructor;
import lombok.Builder;
import lombok.Data;
import lombok.NoArgsConstructor;

@Data
@NoArgsConstructor
@AllArgsConstructor
@Builder
public class EtudiantRemarqueDTO {
    private Integer etudiantId;
    private String matiere;
    private String remarque;
    private String enseignantUsername;
}
