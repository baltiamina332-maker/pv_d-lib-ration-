package com.amna.project.entities;

import jakarta.persistence.*;
import lombok.*;
import java.math.BigDecimal;

@Entity
@Table(name = "NOTE_EXAMEN")
@Data
@NoArgsConstructor
@AllArgsConstructor
public class NoteExamen {

    @EmbeddedId
    private NoteId id;

    @ManyToOne
    @MapsId("idEtudiant")
    @JoinColumn(name = "id_etudiant")
    private Etudiant etudiant;

    @ManyToOne
    @MapsId("idModule")
    @JoinColumn(name = "id_module")
    private Module module;

    @ManyToOne
    @MapsId("idSession")
    @JoinColumn(name = "id_session")
    private SessionExam sessionExam;

    @Column(name = "note_exam")
    private BigDecimal noteExam;
}
