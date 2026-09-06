package com.amna.project.services;

import com.amna.project.entities.NoteTp;
import com.amna.project.entities.NoteId;
import java.util.List;
import java.util.Optional;

public interface NoteTpService {
    NoteTp save(NoteTp entity);
    Optional<NoteTp> findById(NoteId id);
    List<NoteTp> findAll();
    void deleteById(NoteId id);
}
