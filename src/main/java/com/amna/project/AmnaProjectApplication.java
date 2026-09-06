package com.amna.project;

import com.amna.project.entities.Role;
import com.amna.project.entities.UserEntity;
import com.amna.project.repositories.UserRepository;
import org.springframework.boot.CommandLineRunner;
import org.springframework.boot.SpringApplication;
import org.springframework.boot.autoconfigure.SpringBootApplication;
import org.springframework.context.annotation.Bean;
import org.springframework.security.crypto.password.PasswordEncoder;

@SpringBootApplication
public class AmnaProjectApplication {

    public static void main(String[] args) {
        SpringApplication.run(AmnaProjectApplication.class, args);
    }

    @Bean
    CommandLineRunner initDatabase(UserRepository userRepository, PasswordEncoder passwordEncoder) {
        return args -> {
            if (!userRepository.existsByUsername("admin")) {
                UserEntity admin = UserEntity.builder()
                        .username("admin")
                        .password(passwordEncoder.encode("admin"))
                        .role(Role.ROLE_ADMIN)
                        .isApproved(true)
                        .build();
                userRepository.save(admin);
            }
        };
    }
}

