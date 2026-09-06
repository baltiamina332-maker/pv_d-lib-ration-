package com.amna.project.dto;

import lombok.AllArgsConstructor;
import lombok.Builder;
import lombok.Data;
import lombok.NoArgsConstructor;

import java.math.BigDecimal;
import java.util.List;

@Data
@NoArgsConstructor
@AllArgsConstructor
@Builder
public class SaisieNotesRequestDTO {
    private Long affectationId;
    private Long classeId;
    private Integer moduleId;
    private String matiere;
    private List<EtudiantNoteItemDTO> notes;

    @Data
    @NoArgsConstructor
    @AllArgsConstructor
    @Builder
    public static class EtudiantNoteItemDTO {
        private Integer etudiantId;
        private BigDecimal noteCc;
        private BigDecimal noteTp;
        private BigDecimal noteExam;
    }
}
