package com.amna.project.controllers;

import com.amna.project.entities.Etudiant;
import com.amna.project.services.EtudiantService;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.*;
import java.util.List;

@RestController
@RequestMapping("/api/etudiants")
public class EtudiantController {

    @Autowired
    private EtudiantService service;

    @PostMapping
    public Etudiant create(@RequestBody Etudiant entity) {
        return service.save(entity);
    }

    @GetMapping
    public List<Etudiant> getAll() {
        return service.findAll();
    }

    @GetMapping("/{id}")
    public ResponseEntity<Etudiant> getById(@PathVariable Integer id) {
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
