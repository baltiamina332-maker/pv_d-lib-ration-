package com.amna.project.services.impl;

import com.amna.project.entities.Module;
import com.amna.project.repositories.ModuleRepository;
import com.amna.project.services.ModuleService;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.stereotype.Service;
import java.util.List;
import java.util.Optional;

@Service
public class ModuleServiceImpl implements ModuleService {

    @Autowired
    private ModuleRepository repository;

    @Override
    public Module save(Module entity) {
        return repository.save(entity);
    }

    @Override
    public Optional<Module> findById(Integer id) {
        return repository.findById(id);
    }

    @Override
    public List<Module> findAll() {
        return repository.findAll();
    }

    @Override
    public void deleteById(Integer id) {
        repository.deleteById(id);
    }
}
