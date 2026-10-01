package com.amna.project.services;

import jakarta.mail.internet.MimeMessage;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.core.io.ByteArrayResource;
import org.springframework.mail.javamail.JavaMailSender;
import org.springframework.mail.javamail.MimeMessageHelper;
import org.springframework.stereotype.Service;

import java.nio.charset.StandardCharsets;

/**
 * Envoi des procès-verbaux par e-mail (pièce jointe Word).
 * La configuration SMTP est lue dans application.properties (variables MAIL_*).
 */
@Service
public class MailService {

    private static final String DOCX_TYPE = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";

    private final JavaMailSender mailSender;

    @Value("${app.mail.from:}")
    private String from;

    @Value("${spring.mail.host:}")
    private String host;

    public MailService(JavaMailSender mailSender) {
        this.mailSender = mailSender;
    }

    /** Le serveur SMTP et l'expéditeur sont-ils configurés ? */
    public boolean isConfigured() {
        return host != null && !host.isBlank() && from != null && !from.isBlank();
    }

    public String getFrom() {
        return from;
    }

    public void sendWithAttachment(String to, String subject, String htmlBody,
                                   byte[] attachment, String attachmentName) throws Exception {
        MimeMessage message = mailSender.createMimeMessage();
        MimeMessageHelper helper = new MimeMessageHelper(message, true, StandardCharsets.UTF_8.name());
        helper.setFrom(from);
        helper.setTo(to);
        helper.setSubject(subject);
        helper.setText(htmlBody, true);
        helper.addAttachment(attachmentName, new ByteArrayResource(attachment), DOCX_TYPE);
        mailSender.send(message);
    }
}
