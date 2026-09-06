package com.amna.project.services;

import com.amna.project.entities.Etudiant;
import java.util.List;
import java.util.Optional;

public interface EtudiantService {
    Etudiant save(Etudiant entity);
    Optional<Etudiant> findById(Integer id);
    List<Etudiant> findAll();
    void deleteById(Integer id);
}
