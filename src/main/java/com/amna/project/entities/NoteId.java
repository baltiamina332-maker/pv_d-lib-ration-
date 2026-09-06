package com.amna.project.entities;

import jakarta.persistence.*;
import lombok.*;
import java.io.Serializable;

@Embeddable
@Data
@NoArgsConstructor
@AllArgsConstructor
@EqualsAndHashCode
public class NoteId implements Serializable {
    @Column(name = "id_etudiant")
    private Integer idEtudiant;

    @Column(name = "id_module")
    private Integer idModule;

    @Column(name = "id_session")
    private Integer idSession;
}
