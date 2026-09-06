package com.amna.project.repositories;

import com.amna.project.entities.HistoriqueGenerationEntity;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.stereotype.Repository;

import java.util.List;

@Repository
public interface HistoriqueGenerationRepository extends JpaRepository<HistoriqueGenerationEntity, Long> {
    List<HistoriqueGenerationEntity> findAllByOrderByDateGenerationDesc();
}
