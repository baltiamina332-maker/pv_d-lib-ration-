package com.amna.project.services.impl;

import com.amna.project.entities.NoteCc;
import com.amna.project.entities.NoteId;
import com.amna.project.repositories.NoteCcRepository;
import com.amna.project.services.NoteCcService;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.stereotype.Service;
import java.util.List;
import java.util.Optional;

@Service
public class NoteCcServiceImpl implements NoteCcService {

    @Autowired
    private NoteCcRepository repository;

    @Override
    public NoteCc save(NoteCc entity) {
        return repository.save(entity);
    }

    @Override
    public Optional<NoteCc> findById(NoteId id) {
        return repository.findById(id);
    }

    @Override
    public List<NoteCc> findAll() {
        return repository.findAll();
    }

    @Override
    public void deleteById(NoteId id) {
        repository.deleteById(id);
    }
}
