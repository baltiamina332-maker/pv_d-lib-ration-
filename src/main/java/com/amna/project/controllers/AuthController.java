package com.amna.project.controllers;

import com.amna.project.dto.JwtResponseDTO;
import com.amna.project.dto.LoginRequestDTO;
import com.amna.project.dto.SignupRequestDTO;
import com.amna.project.entities.Role;
import com.amna.project.entities.UserEntity;
import com.amna.project.repositories.UserRepository;
import com.amna.project.security.JwtUtils;
import com.amna.project.security.UserDetailsImpl;
import com.amna.project.services.EmailValidator;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.http.ResponseEntity;
import org.springframework.security.authentication.AuthenticationManager;
import org.springframework.security.authentication.UsernamePasswordAuthenticationToken;
import org.springframework.security.core.Authentication;
import org.springframework.security.core.context.SecurityContextHolder;
import org.springframework.security.crypto.password.PasswordEncoder;
import org.springframework.web.bind.annotation.*;

import java.util.Map;

@RestController
@RequestMapping("/api/auth")
public class AuthController {

    @Autowired
    AuthenticationManager authenticationManager;

    @Autowired
    UserRepository userRepository;

    @Autowired
    PasswordEncoder encoder;

    @Autowired
    JwtUtils jwtUtils;

    @PostMapping("/login")
    public ResponseEntity<?> authenticateUser(@RequestBody LoginRequestDTO loginRequest) {

        // First check if user exists and is approved to provide a clear error message
        UserEntity user = userRepository.findByUsername(loginRequest.getUsername()).orElse(null);
        if (user == null) {
            return ResponseEntity.badRequest().body(Map.of("message", "Utilisateur non trouvé."));
        }
        if (!user.isApproved()) {
            return ResponseEntity.badRequest().body(Map.of("message", "Votre compte est en attente d'approbation par l'administrateur."));
        }

        Authentication authentication = authenticationManager.authenticate(
                new UsernamePasswordAuthenticationToken(loginRequest.getUsername(), loginRequest.getPassword()));

        SecurityContextHolder.getContext().setAuthentication(authentication);
        String jwt = jwtUtils.generateJwtToken(authentication);

        UserDetailsImpl userDetails = (UserDetailsImpl) authentication.getPrincipal();

        return ResponseEntity.ok(new JwtResponseDTO(
                jwt,
                userDetails.getId(),
                userDetails.getUsername(),
                user.getRole().name(),
                userDetails.isApproved()));
    }

    @PostMapping("/register")
    public ResponseEntity<?> registerUser(@RequestBody SignupRequestDTO signUpRequest) {
        if (userRepository.existsByUsername(signUpRequest.getUsername())) {
            return ResponseEntity
                    .badRequest()
                    .body(Map.of("message", "Erreur: Ce nom d'utilisateur est déjà pris!"));
        }

        String email = signUpRequest.getEmail() != null ? signUpRequest.getEmail().trim() : "";
        if (!email.isEmpty() && !EmailValidator.isValid(email)) {
            return ResponseEntity.badRequest().body(Map.of("message", "Adresse e-mail invalide."));
        }

        // Create new user's account
        UserEntity user = UserEntity.builder()
                .username(signUpRequest.getUsername())
                .password(encoder.encode(signUpRequest.getPassword()))
                .role(Role.ROLE_USER)
                .isApproved(false) // Needs admin approval by default
                .email(email.isEmpty() ? null : email)
                .build();

        userRepository.save(user);

        return ResponseEntity.ok(Map.of("message", "Utilisateur enregistré avec succès. En attente d'approbation."));
    }

    @PostMapping("/change-password")
    public ResponseEntity<?> changePassword(@RequestBody com.amna.project.dto.ChangePasswordRequestDTO request, Authentication authentication) {
        String username = authentication.getName();
        UserEntity user = userRepository.findByUsername(username).orElse(null);

        if (user == null) {
            return ResponseEntity.badRequest().body(Map.of("message", "Utilisateur introuvable."));
        }

        if (!encoder.matches(request.getOldPassword(), user.getPassword())) {
            return ResponseEntity.badRequest().body(Map.of("message", "L'ancien mot de passe est incorrect."));
        }

        user.setPassword(encoder.encode(request.getNewPassword()));
        userRepository.save(user);

        return ResponseEntity.ok(Map.of("message", "Mot de passe mis à jour avec succès."));
    }

    @GetMapping("/me")
    public ResponseEntity<?> currentUser(Authentication authentication) {
        if (authentication == null) {
            return ResponseEntity.status(401).body(Map.of("message", "Non authentifié."));
        }
        return userRepository.findByUsername(authentication.getName())
                .<ResponseEntity<?>>map(u -> ResponseEntity.ok(Map.of(
                        "id", u.getId(),
                        "username", u.getUsername(),
                        "role", u.getRole().name(),
                        "email", u.getEmail() != null ? u.getEmail() : "")))
                .orElse(ResponseEntity.badRequest().body(Map.of("message", "Utilisateur introuvable.")));
    }

    @PutMapping("/email")
    public ResponseEntity<?> updateMyEmail(@RequestBody Map<String, String> body, Authentication authentication) {
        if (authentication == null) {
            return ResponseEntity.status(401).body(Map.of("message", "Non authentifié."));
        }
        String email = body.getOrDefault("email", "").trim();
        if (!email.isEmpty() && !EmailValidator.isValid(email)) {
            return ResponseEntity.badRequest().body(Map.of("message", "Adresse e-mail invalide."));
        }
        UserEntity user = userRepository.findByUsername(authentication.getName()).orElse(null);
        if (user == null) {
            return ResponseEntity.badRequest().body(Map.of("message", "Utilisateur introuvable."));
        }
        user.setEmail(email.isEmpty() ? null : email);
        userRepository.save(user);
        return ResponseEntity.ok(Map.of("message", "Adresse e-mail mise à jour."));
    }
}
