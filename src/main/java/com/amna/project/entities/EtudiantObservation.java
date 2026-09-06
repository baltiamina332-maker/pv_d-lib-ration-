package com.amna.project.entities;

import jakarta.persistence.*;
import lombok.*;
import java.time.LocalDateTime;

@Entity
@Table(name = "ETUDIANT_OBSERVATION")
@Data
@NoArgsConstructor
@AllArgsConstructor
@Builder
public class EtudiantObservation {

    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;

    private Integer etudiantId;
    private String matiere;
    private String enseignantUsername;

    @Column(length = 1000)
    private String remarque;

    private LocalDateTime dateCreation;

    @PrePersist
    public void prePersist() {
        if (dateCreation == null) {
            dateCreation = LocalDateTime.now();
        }
    }
}
