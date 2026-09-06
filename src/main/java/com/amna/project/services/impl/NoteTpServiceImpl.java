package com.amna.project.services.impl;

import com.amna.project.entities.NoteTp;
import com.amna.project.entities.NoteId;
import com.amna.project.repositories.NoteTpRepository;
import com.amna.project.services.NoteTpService;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.stereotype.Service;
import java.util.List;
import java.util.Optional;

@Service
public class NoteTpServiceImpl implements NoteTpService {

    @Autowired
    private NoteTpRepository repository;

    @Override
    public NoteTp save(NoteTp entity) {
        return repository.save(entity);
    }

    @Override
    public Optional<NoteTp> findById(NoteId id) {
        return repository.findById(id);
    }

    @Override
    public List<NoteTp> findAll() {
        return repository.findAll();
    }

    @Override
    public void deleteById(NoteId id) {
        repository.deleteById(id);
    }
}
