package com.amna.project.dto;

import lombok.AllArgsConstructor;
import lombok.Builder;
import lombok.Data;
import lombok.NoArgsConstructor;

import java.util.ArrayList;
import java.util.List;

/**
 * Demande d'envoi d'un PV par e-mail aux enseignants.
 */
@Data
@NoArgsConstructor
@AllArgsConstructor
@Builder
public class PvEmailRequestDTO {
    private SessionDeliberationDTO sessionInfo;
    private ClassePreviewDTO classeData;
    @Builder.Default
    private List<Long> enseignantIds = new ArrayList<>();
    private String message;
}
