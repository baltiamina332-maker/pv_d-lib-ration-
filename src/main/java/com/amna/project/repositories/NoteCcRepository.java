package com.amna.project.repositories;

import com.amna.project.entities.NoteCc;
import com.amna.project.entities.NoteId;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.stereotype.Repository;

@Repository
public interface NoteCcRepository extends JpaRepository<NoteCc, NoteId> {
}
