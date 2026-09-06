package com.amna.project.services.impl;

import com.amna.project.entities.NoteExamen;
import com.amna.project.entities.NoteId;
import com.amna.project.repositories.NoteExamenRepository;
import com.amna.project.services.NoteExamenService;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.stereotype.Service;
import java.util.List;
import java.util.Optional;

@Service
public class NoteExamenServiceImpl implements NoteExamenService {

    @Autowired
    private NoteExamenRepository repository;

    @Override
    public NoteExamen save(NoteExamen entity) {
        return repository.save(entity);
    }

    @Override
    public Optional<NoteExamen> findById(NoteId id) {
        return repository.findById(id);
    }

    @Override
    public List<NoteExamen> findAll() {
        return repository.findAll();
    }

    @Override
    public void deleteById(NoteId id) {
        repository.deleteById(id);
    }
}
