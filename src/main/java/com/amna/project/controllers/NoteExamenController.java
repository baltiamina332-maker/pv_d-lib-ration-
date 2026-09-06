package com.amna.project.controllers;

import com.amna.project.entities.NoteExamen;
import com.amna.project.entities.NoteId;
import com.amna.project.services.NoteExamenService;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.*;
import java.util.List;

@RestController
@RequestMapping("/api/noteexamens")
public class NoteExamenController {

    @Autowired
    private NoteExamenService service;

    @PostMapping
    public NoteExamen create(@RequestBody NoteExamen entity) {
        return service.save(entity);
    }

    @GetMapping
    public List<NoteExamen> getAll() {
        return service.findAll();
    }

    @GetMapping("/detail")
    public ResponseEntity<NoteExamen> getById(
            @RequestParam Integer idEtudiant,
            @RequestParam Integer idModule,
            @RequestParam Integer idSession) {
        NoteId id = new NoteId(idEtudiant, idModule, idSession);
        return service.findById(id)
                .map(ResponseEntity::ok)
                .orElse(ResponseEntity.notFound().build());
    }

    @DeleteMapping("/detail")
    public ResponseEntity<Void> delete(
            @RequestParam Integer idEtudiant,
            @RequestParam Integer idModule,
            @RequestParam Integer idSession) {
        NoteId id = new NoteId(idEtudiant, idModule, idSession);
        service.deleteById(id);
        return ResponseEntity.ok().build();
    }
}
