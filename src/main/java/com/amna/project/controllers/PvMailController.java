package com.amna.project.controllers;

import com.amna.project.dto.ClassePreviewDTO;
import com.amna.project.dto.PvEmailRequestDTO;
import com.amna.project.dto.SessionDeliberationDTO;
import com.amna.project.entities.Affectation;
import com.amna.project.entities.UserEntity;
import com.amna.project.repositories.AffectationRepository;
import com.amna.project.repositories.UserRepository;
import com.amna.project.services.EmailValidator;
import com.amna.project.services.MailService;
import com.amna.project.services.WordGeneratorService;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.http.ResponseEntity;
import org.springframework.security.access.prepost.PreAuthorize;
import org.springframework.web.bind.annotation.*;
import org.springframework.web.util.HtmlUtils;

import java.util.*;

/**
 * Envoi des PV générés aux enseignants par e-mail (réservé à l'administrateur).
 */
@RestController
@RequestMapping("/api/admin/pv-mail")
@PreAuthorize("hasAuthority('ROLE_ADMIN')")
public class PvMailController {

    @Autowired
    private UserRepository userRepository;

    @Autowired
    private AffectationRepository affectationRepository;

    @Autowired
    private WordGeneratorService wordGeneratorService;

    @Autowired
    private MailService mailService;

    @GetMapping("/status")
    public ResponseEntity<?> status() {
        return ResponseEntity.ok(Map.of(
                "configured", mailService.isConfigured(),
                "from", mailService.getFrom() != null ? mailService.getFrom() : ""));
    }

    /**
     * Liste des destinataires possibles : les enseignants affectés à la classe en premier.
     */
    @GetMapping("/enseignants")
    public ResponseEntity<List<Map<String, Object>>> enseignants(@RequestParam(required = false) String classe) {
        Map<Long, Set<String>> matieresParEnseignant = new HashMap<>();
        for (Affectation a : affectationRepository.findAll()) {
            if (a.getEnseignant() == null || a.getClasse() == null) continue;
            if (classe != null && !a.getClasse().getNomClasse().trim().equalsIgnoreCase(classe.trim())) continue;
            String matiere = a.getMatiere() != null ? a.getMatiere()
                    : (a.getModule() != null ? a.getModule().getNomModule() : "");
            matieresParEnseignant.computeIfAbsent(a.getEnseignant().getId(), k -> new TreeSet<>()).add(matiere);
        }

        List<Map<String, Object>> result = new ArrayList<>();
        for (UserEntity u : userRepository.findAll()) {
            Set<String> matieres = matieresParEnseignant.getOrDefault(u.getId(), Set.of());
            Map<String, Object> row = new LinkedHashMap<>();
            row.put("id", u.getId());
            row.put("username", u.getUsername());
            row.put("role", u.getRole().name());
            row.put("email", u.getEmail() != null ? u.getEmail() : "");
            row.put("affecte", !matieres.isEmpty());
            row.put("matieres", new ArrayList<>(matieres));
            result.add(row);
        }
        result.sort(Comparator.comparing((Map<String, Object> r) -> !(Boolean) r.get("affecte"))
                .thenComparing(r -> ((String) r.get("username")).toLowerCase()));
        return ResponseEntity.ok(result);
    }

    @PostMapping("/send")
    public ResponseEntity<?> sendPv(@RequestBody PvEmailRequestDTO request) {
        if (!mailService.isConfigured()) {
            return ResponseEntity.badRequest().body(Map.of("message",
                    "L'envoi d'e-mails n'est pas configuré sur le serveur (variables MAIL_USERNAME, MAIL_PASSWORD, MAIL_FROM)."));
        }
        ClassePreviewDTO classe = request.getClasseData();
        if (classe == null || classe.getEtudiants() == null) {
            return ResponseEntity.badRequest().body(Map.of("message", "Aucune classe à envoyer."));
        }
        if (request.getEnseignantIds() == null || request.getEnseignantIds().isEmpty()) {
            return ResponseEntity.badRequest().body(Map.of("message", "Sélectionnez au moins un destinataire."));
        }

        byte[] docx;
        try {
            docx = wordGeneratorService.generatePvWordDocument(classe, request.getSessionInfo());
        } catch (Exception e) {
            return ResponseEntity.internalServerError().body(Map.of("message", "Erreur lors de la génération du PV : " + e.getMessage()));
        }
        String filename = "PV_" + classe.getNomClasse().replaceAll("[^a-zA-Z0-9_-]", "_") + ".docx";
        String subject = "Procès-verbal de délibération – " + classe.getNomClasse();

        List<Map<String, String>> envoyes = new ArrayList<>();
        List<Map<String, String>> echecs = new ArrayList<>();
        for (UserEntity u : userRepository.findAllById(request.getEnseignantIds())) {
            if (!EmailValidator.isValid(u.getEmail())) {
                echecs.add(Map.of("username", u.getUsername(), "raison", "Aucune adresse e-mail valide"));
                continue;
            }
            try {
                mailService.sendWithAttachment(u.getEmail(), subject,
                        buildBody(u.getUsername(), classe, request.getSessionInfo(), request.getMessage()),
                        docx, filename);
                envoyes.add(Map.of("username", u.getUsername(), "email", u.getEmail()));
            } catch (Exception e) {
                echecs.add(Map.of("username", u.getUsername(), "raison", "Échec SMTP : " + e.getMessage()));
            }
        }

        Map<String, Object> body = new LinkedHashMap<>();
        body.put("fichier", filename);
        body.put("envoyes", envoyes);
        body.put("echecs", echecs);
        body.put("message", envoyes.size() + " e-mail(s) envoyé(s)" + (echecs.isEmpty() ? "." : ", " + echecs.size() + " échec(s)."));
        return ResponseEntity.ok(body);
    }

    private String buildBody(String username, ClassePreviewDTO classe, SessionDeliberationDTO session, String message) {
        String esc = HtmlUtils.htmlEscape(classe.getNomClasse(), "UTF-8");
        StringBuilder sb = new StringBuilder();
        sb.append("<div style=\"font-family:Arial,sans-serif;font-size:14px;color:#1f2937\">");
        sb.append("<p>Bonjour ").append(HtmlUtils.htmlEscape(username, "UTF-8")).append(",</p>");
        sb.append("<p>Veuillez trouver ci-joint le procès-verbal de délibération de la classe <strong>")
                .append(esc).append("</strong>");
        if (session != null && session.getTypeSession() != null) {
            sb.append(" (session ").append(HtmlUtils.htmlEscape(session.getTypeSession(), "UTF-8")).append(")");
        }
        sb.append(".</p>");
        sb.append("<table style=\"border-collapse:collapse;margin:8px 0\">");
        row(sb, "Effectif", String.valueOf(classe.getTotalEtudiants()));
        row(sb, "Admis", String.valueOf(classe.getNbAdmis()));
        row(sb, "Conseil d'École", String.valueOf(classe.getNbConseilEcole()));
        row(sb, "Taux de réussite", classe.getTauxReussite() + " %");
        sb.append("</table>");
        if (message != null && !message.isBlank()) {
            sb.append("<p>").append(HtmlUtils.htmlEscape(message, "UTF-8").replace("\n", "<br>")).append("</p>");
        }
        sb.append("<p>Cordialement,<br>L'administration – PV-Delib</p></div>");
        return sb.toString();
    }

    private void row(StringBuilder sb, String label, String value) {
        sb.append("<tr><td style=\"padding:4px 12px;border:1px solid #d1d5db;background:#eff6ff\">")
                .append(HtmlUtils.htmlEscape(label, "UTF-8")).append("</td><td style=\"padding:4px 12px;border:1px solid #d1d5db\">")
                .append(HtmlUtils.htmlEscape(value, "UTF-8")).append("</td></tr>");
    }
}
