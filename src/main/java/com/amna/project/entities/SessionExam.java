package com.amna.project.entities;

import jakarta.persistence.*;
import lombok.*;

@Entity
@Table(name = "SESSION_EXAM")
@Data
@NoArgsConstructor
@AllArgsConstructor
public class SessionExam {
    @Id
    @Column(name = "id_session")
    private Integer idSession;

    private String libelle;
}
