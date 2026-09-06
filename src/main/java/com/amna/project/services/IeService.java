package com.amna.project.services;

import com.amna.project.entities.Ie;
import java.util.List;
import java.util.Optional;

public interface IeService {
    Ie save(Ie entity);
    Optional<Ie> findById(Integer id);
    List<Ie> findAll();
    void deleteById(Integer id);
}
