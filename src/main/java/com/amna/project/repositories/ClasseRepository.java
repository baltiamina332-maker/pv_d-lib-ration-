package com.amna.project.repositories;

import com.amna.project.entities.Classe;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.stereotype.Repository;

import java.util.Optional;

@Repository
public interface ClasseRepository extends JpaRepository<Classe, Long> {
    Optional<Classe> findByNomClasse(String nomClasse);
    boolean existsByNomClasse(String nomClasse);
}
