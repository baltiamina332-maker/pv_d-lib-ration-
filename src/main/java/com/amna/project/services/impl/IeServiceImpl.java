package com.amna.project.services.impl;

import com.amna.project.entities.Ie;
import com.amna.project.repositories.IeRepository;
import com.amna.project.services.IeService;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.stereotype.Service;
import java.util.List;
import java.util.Optional;

@Service
public class IeServiceImpl implements IeService {

    @Autowired
    private IeRepository repository;

    @Override
    public Ie save(Ie entity) {
        return repository.save(entity);
    }

    @Override
    public Optional<Ie> findById(Integer id) {
        return repository.findById(id);
    }

    @Override
    public List<Ie> findAll() {
        return repository.findAll();
    }

    @Override
    public void deleteById(Integer id) {
        repository.deleteById(id);
    }
}
