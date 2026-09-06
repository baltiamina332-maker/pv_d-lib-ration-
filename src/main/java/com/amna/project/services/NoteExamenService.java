package com.amna.project.services;

import com.amna.project.entities.NoteExamen;
import com.amna.project.entities.NoteId;
import java.util.List;
import java.util.Optional;

public interface NoteExamenService {
    NoteExamen save(NoteExamen entity);
    Optional<NoteExamen> findById(NoteId id);
    List<NoteExamen> findAll();
    void deleteById(NoteId id);
}
