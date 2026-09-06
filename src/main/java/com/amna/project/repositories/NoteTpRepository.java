package com.amna.project.repositories;

import com.amna.project.entities.NoteTp;
import com.amna.project.entities.NoteId;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.stereotype.Repository;

@Repository
public interface NoteTpRepository extends JpaRepository<NoteTp, NoteId> {
}
