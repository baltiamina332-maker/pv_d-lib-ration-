package com.amna.project.repositories;

import com.amna.project.entities.NoteExamen;
import com.amna.project.entities.NoteId;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.stereotype.Repository;

@Repository
public interface NoteExamenRepository extends JpaRepository<NoteExamen, NoteId> {
}
