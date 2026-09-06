package com.amna.project.services;

import com.amna.project.entities.Module;
import java.util.List;
import java.util.Optional;

public interface ModuleService {
    Module save(Module entity);
    Optional<Module> findById(Integer id);
    List<Module> findAll();
    void deleteById(Integer id);
}
