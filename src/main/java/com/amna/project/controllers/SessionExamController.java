package com.amna.project.controllers;

import com.amna.project.entities.SessionExam;
import com.amna.project.services.SessionExamService;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.*;
import java.util.List;

@RestController
@RequestMapping("/api/sessions")
public class SessionExamController {

    @Autowired
    private SessionExamService service;

    @PostMapping
    public SessionExam create(@RequestBody SessionExam entity) {
        return service.save(entity);
    }

    @GetMapping
    public List<SessionExam> getAll() {
        return service.findAll();
    }

    @GetMapping("/{id}")
    public ResponseEntity<SessionExam> getById(@PathVariable Integer id) {
        return service.findById(id)
                .map(ResponseEntity::ok)
                .orElse(ResponseEntity.notFound().build());
    }

    @DeleteMapping("/{id}")
    public ResponseEntity<Void> delete(@PathVariable Integer id) {
        service.deleteById(id);
        return ResponseEntity.ok().build();
    }
}
