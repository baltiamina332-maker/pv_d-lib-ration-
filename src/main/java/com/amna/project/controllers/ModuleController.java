package com.amna.project.controllers;

import com.amna.project.entities.Module;
import com.amna.project.services.ModuleService;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.*;
import java.util.List;

@RestController
@RequestMapping("/api/modules")
public class ModuleController {

    @Autowired
    private ModuleService service;

    @PostMapping
    public Module create(@RequestBody Module entity) {
        return service.save(entity);
    }

    @GetMapping
    public List<Module> getAll() {
        return service.findAll();
    }

    @GetMapping("/{id}")
    public ResponseEntity<Module> getById(@PathVariable Integer id) {
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
