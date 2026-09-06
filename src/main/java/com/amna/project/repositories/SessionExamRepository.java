package com.amna.project.repositories;

import com.amna.project.entities.SessionExam;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.stereotype.Repository;

@Repository
public interface SessionExamRepository extends JpaRepository<SessionExam, Integer> {
}
