package com.amna.project.services;

import com.amna.project.entities.SessionExam;
import java.util.List;
import java.util.Optional;

public interface SessionExamService {
    SessionExam save(SessionExam entity);
    Optional<SessionExam> findById(Integer id);
    List<SessionExam> findAll();
    void deleteById(Integer id);
}
