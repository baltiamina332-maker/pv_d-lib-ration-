package com.amna.project.entities;

import jakarta.persistence.*;
import lombok.*;
import java.math.BigDecimal;

@Entity
@Table(name = "MODULE")
@Data
@NoArgsConstructor
@AllArgsConstructor
public class Module {
    @Id
    @Column(name = "id_module")
    private Integer idModule;

    @Column(name = "nom_module")
    private String nomModule;

    private String enseignant;
    private BigDecimal coef;
    private Integer ects = 3; // Default 3 ECTS
    
    @Column(name = "moyen_module")
    private BigDecimal moyenModule;

    @ManyToOne
    @JoinColumn(name = "id_ie")
    private Ie ie;
}
