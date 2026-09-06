package com.amna.project.repositories;

import com.amna.project.entities.Affectation;
import com.amna.project.entities.UserEntity;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.stereotype.Repository;

import java.util.List;

@Repository
public interface AffectationRepository extends JpaRepository<Affectation, Long> {
    List<Affectation> findByEnseignant(UserEntity enseignant);
    List<Affectation> findByEnseignantUsername(String username);
    List<Affectation> findByClasseId(Long classeId);
}
