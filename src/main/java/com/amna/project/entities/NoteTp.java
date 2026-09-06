package com.amna.project.entities;

import jakarta.persistence.*;
import lombok.*;
import java.math.BigDecimal;

@Entity
@Table(name = "NOTETP")
@Data
@NoArgsConstructor
@AllArgsConstructor
public class NoteTp {

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

    @Column(name = "note_tp")
    private BigDecimal noteTp;
}
