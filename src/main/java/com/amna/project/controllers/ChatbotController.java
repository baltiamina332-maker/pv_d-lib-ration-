package com.amna.project.controllers;

import com.amna.project.services.ChatbotService;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.http.ResponseEntity;
import org.springframework.security.core.Authentication;
import org.springframework.web.bind.annotation.*;

import java.util.List;
import java.util.Map;

/**
 * Chatbot de la plateforme (web et desktop). Accessible à tout utilisateur connecté.
 */
@RestController
@RequestMapping("/api/chatbot")
public class ChatbotController {

    private static final int MAX_MESSAGE_LENGTH = 2000;

    @Autowired
    private ChatbotService chatbotService;

    public record ChatRequest(String message, List<ChatbotService.ChatTurn> history) {
    }

    @GetMapping("/status")
    public ResponseEntity<?> status() {
        return ResponseEntity.ok(Map.of("mode", chatbotService.isClaudeEnabled() ? "claude" : "local"));
    }

    @PostMapping("/message")
    public ResponseEntity<?> message(@RequestBody ChatRequest request, Authentication authentication) {
        String message = request.message() != null ? request.message().trim() : "";
        if (message.isEmpty()) {
            return ResponseEntity.badRequest().body(Map.of("message", "Message vide."));
        }
        if (message.length() > MAX_MESSAGE_LENGTH) {
            return ResponseEntity.badRequest().body(Map.of("message", "Message trop long (2000 caractères maximum)."));
        }
        boolean admin = authentication.getAuthorities().stream()
                .anyMatch(a -> "ROLE_ADMIN".equals(a.getAuthority()));
        ChatbotService.ChatReply reply = chatbotService.reply(message, request.history(), authentication.getName(), admin);
        return ResponseEntity.ok(Map.of("reply", reply.reply(), "mode", reply.mode()));
    }
}
