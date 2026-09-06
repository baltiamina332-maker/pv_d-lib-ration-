package com.amna.project.controllers;

import com.amna.project.entities.Ie;
import com.amna.project.services.IeService;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.*;
import java.util.List;

@RestController
@RequestMapping("/api/ies")
public class IeController {

    @Autowired
    private IeService service;

    @PostMapping
    public Ie create(@RequestBody Ie entity) {
        return service.save(entity);
    }

    @GetMapping
    public List<Ie> getAll() {
        return service.findAll();
    }

    @GetMapping("/{id}")
    public ResponseEntity<Ie> getById(@PathVariable Integer id) {
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
