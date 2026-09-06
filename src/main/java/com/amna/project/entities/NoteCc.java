package com.amna.project.entities;

import jakarta.persistence.*;
import lombok.*;
import java.math.BigDecimal;

@Entity
@Table(name = "NOTECC")
@Data
@NoArgsConstructor
@AllArgsConstructor
public class NoteCc {

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

    @Column(name = "note_cc")
    private BigDecimal noteCc;
}
