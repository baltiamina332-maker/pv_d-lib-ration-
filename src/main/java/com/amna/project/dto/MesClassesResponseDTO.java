package com.amna.project.dto;

import com.amna.project.entities.Etudiant;
import lombok.AllArgsConstructor;
import lombok.Builder;
import lombok.Data;
import lombok.NoArgsConstructor;

import java.util.List;

@Data
@NoArgsConstructor
@AllArgsConstructor
@Builder
public class MesClassesResponseDTO {
    private String enseignantUsername;
    private List<ClasseAffecteeDTO> classesAffectees;

    @Data
    @NoArgsConstructor
    @AllArgsConstructor
    @Builder
    public static class ClasseAffecteeDTO {
        private Long affectationId;
        private Long classeId;
        private String nomClasse;
        private String niveau;
        private String matiere;
        private String anneeUniversitaire;
        private int totalEtudiants;
        private List<Etudiant> etudiants;
    }
}
