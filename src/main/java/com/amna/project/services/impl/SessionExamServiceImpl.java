package com.amna.project.services.impl;

import com.amna.project.entities.SessionExam;
import com.amna.project.repositories.SessionExamRepository;
import com.amna.project.services.SessionExamService;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.stereotype.Service;
import java.util.List;
import java.util.Optional;

@Service
public class SessionExamServiceImpl implements SessionExamService {

    @Autowired
    private SessionExamRepository repository;

    @Override
    public SessionExam save(SessionExam entity) {
        return repository.save(entity);
    }

    @Override
    public Optional<SessionExam> findById(Integer id) {
        return repository.findById(id);
    }

    @Override
    public List<SessionExam> findAll() {
        return repository.findAll();
    }

    @Override
    public void deleteById(Integer id) {
        repository.deleteById(id);
    }
}
