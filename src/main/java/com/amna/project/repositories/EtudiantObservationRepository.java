package com.amna.project.repositories;

import com.amna.project.entities.EtudiantObservation;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.stereotype.Repository;

import java.util.List;
import java.util.Optional;

@Repository
public interface EtudiantObservationRepository extends JpaRepository<EtudiantObservation, Long> {
    Optional<EtudiantObservation> findByEtudiantIdAndMatiere(Integer etudiantId, String matiere);
    List<EtudiantObservation> findByMatiere(String matiere);
}
