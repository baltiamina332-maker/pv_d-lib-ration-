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
public class SessionDeliberationDTO {
    private String etablissement;
    private String faculteDepartement;
    private String classe;
    private String anneeUniversitaire;
    private String dateSession;
    private String lieuDeliberation;
    private String typeSession;
    private String presidentJury;
    
    @Builder.Default
    private List<MembreJuryDTO> membresJury = new ArrayList<>();

    @Builder.Default
    private String modeCalcul = "AUTO"; // AUTO or PRECALC
}
