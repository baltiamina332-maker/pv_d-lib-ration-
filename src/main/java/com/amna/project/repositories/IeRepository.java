package com.amna.project.repositories;

import com.amna.project.entities.Ie;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.stereotype.Repository;

@Repository
public interface IeRepository extends JpaRepository<Ie, Integer> {
}
