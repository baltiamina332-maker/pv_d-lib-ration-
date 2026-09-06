package com.amna.project.repositories;

import com.amna.project.entities.Etudiant;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.stereotype.Repository;

import java.util.List;

@Repository
public interface EtudiantRepository extends JpaRepository<Etudiant, Integer> {
    List<Etudiant> findByClasseId(Long classeId);
    List<Etudiant> findByClasseNomClasse(String nomClasse);
}
