package com.amna.project.services.impl;

import com.amna.project.entities.Etudiant;
import com.amna.project.repositories.EtudiantRepository;
import com.amna.project.services.EtudiantService;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.stereotype.Service;
import java.util.List;
import java.util.Optional;

@Service
public class EtudiantServiceImpl implements EtudiantService {

    @Autowired
    private EtudiantRepository repository;

    @Override
    public Etudiant save(Etudiant entity) {
        if (entity.getIdEtudiant() == null) {
            int nextId = repository.findAll().stream()
                    .mapToInt(Etudiant::getIdEtudiant)
                    .max()
                    .orElse(0) + 1;
            entity.setIdEtudiant(nextId);
        }
        return repository.save(entity);
    }

    @Override
    public Optional<Etudiant> findById(Integer id) {
        return repository.findById(id);
    }

    @Override
    public List<Etudiant> findAll() {
        return repository.findAll();
    }

    @Override
    public void deleteById(Integer id) {
        repository.deleteById(id);
    }
}
