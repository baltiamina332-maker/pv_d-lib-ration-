package com.amna.project.entities;

import jakarta.persistence.*;
import lombok.*;

@Entity
@Table(name = "ETUDIANT")
@Data
@NoArgsConstructor
@AllArgsConstructor
@Builder
public class Etudiant {

    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    @Column(name = "id_etudiant")
    private Integer idEtudiant;

    private String nom;
    private String prenom;

    @ManyToOne(fetch = FetchType.EAGER)
    @JoinColumn(name = "classe_id")
    private Classe classe;
}
