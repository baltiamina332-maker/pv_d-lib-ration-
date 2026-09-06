package com.amna.project.services;

import com.amna.project.entities.NoteCc;
import com.amna.project.entities.NoteId;
import java.util.List;
import java.util.Optional;

public interface NoteCcService {
    NoteCc save(NoteCc entity);
    Optional<NoteCc> findById(NoteId id);
    List<NoteCc> findAll();
    void deleteById(NoteId id);
}
