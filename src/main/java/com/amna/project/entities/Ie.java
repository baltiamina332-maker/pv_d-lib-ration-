package com.amna.project.entities;

import jakarta.persistence.*;
import lombok.*;
import java.math.BigDecimal;

@Entity
@Table(name = "IE")
@Data
@NoArgsConstructor
@AllArgsConstructor
public class Ie {
    @Id
    @Column(name = "id_ie")
    private Integer idIe;

    @Column(name = "nom_panier")
    private String nomPanier;
    
    @Column(name = "moyen_ie")
    private BigDecimal moyenIe;
}
