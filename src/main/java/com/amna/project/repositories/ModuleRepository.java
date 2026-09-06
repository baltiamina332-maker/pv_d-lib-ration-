package com.amna.project.repositories;

import com.amna.project.entities.Module;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.stereotype.Repository;

import java.util.Optional;

@Repository
public interface ModuleRepository extends JpaRepository<Module, Integer> {
    Optional<Module> findByNomModule(String nomModule);
}
