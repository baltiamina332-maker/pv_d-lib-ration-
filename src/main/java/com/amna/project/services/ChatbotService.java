package com.amna.project.services;

import com.anthropic.client.AnthropicClient;
import com.anthropic.client.okhttp.AnthropicOkHttpClient;
import com.anthropic.errors.AnthropicIoException;
import com.anthropic.errors.AnthropicServiceException;
import com.anthropic.errors.RateLimitException;
import com.anthropic.errors.UnauthorizedException;
import com.anthropic.models.beta.messages.BetaMessage;
import com.anthropic.models.beta.messages.BetaOutputConfig;
import com.anthropic.models.beta.messages.BetaStopReason;
import com.anthropic.models.beta.messages.BetaTextBlock;
import com.anthropic.models.beta.messages.MessageCreateParams;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.stereotype.Service;

import java.time.Duration;
import java.util.List;
import java.util.stream.Collectors;

/**
 * Chatbot intelligent de PV-Delib.
 * - Avec une clé ANTHROPIC_API_KEY : réponses générées par Claude à partir des données de la plateforme.
 * - Sans clé (ou si l'API est indisponible) : réponses de l'assistant local.
 */
@Service
public class ChatbotService {

    private static final Logger log = LoggerFactory.getLogger(ChatbotService.class);
    private static final int MAX_HISTORY_TURNS = 12;

    private static final String SYSTEM_PROMPT = """
            Tu es l'assistant intelligent de PV-Delib, la plateforme web et desktop de gestion des délibérations
            universitaires (système LMD) : gestion des classes, des enseignants et des notes, application automatique
            des règles de délibération, génération des procès-verbaux (PV) Word et diagnostic par Machine Learning.

            Comment répondre :
            - Réponds dans la langue de l'utilisateur (français par défaut), de façon concise : quelques phrases ou une courte liste.
            - Pour tout ce qui concerne les classes, étudiants, moyennes, décisions, enseignants et PV, appuie-toi uniquement sur
              les données fournies dans <donnees_plateforme>. Si une information n'y figure pas, dis-le simplement au lieu de l'inventer.
            - Les moyennes, crédits ECTS non validés et décisions sont calculés par la plateforme à partir des notes saisies
              (moyenne d'un module = 0,2 CC + 0,2 TP + 0,6 examen, ou 0,3 CC + 0,7 examen sans TP).
            - Tu ne peux ni modifier les données ni lancer d'action : quand l'utilisateur veut faire quelque chose,
              explique-lui où cliquer dans l'application.
            - Un enseignant ne voit que ses propres classes ; ne présente pas les données comme exhaustives pour lui.
            - Mise en forme : texte simple, puces « • » ou listes numérotées, **gras** seulement pour les éléments clés,
              pas de tableaux ni de titres Markdown.

            Règles de délibération LMD appliquées par la plateforme :
            - MG ≥ 10 et ECTS non validés ≤ 15 → Admis ; MG ≥ 10 et ECTS non validés > 15 → Admis avec ECTS non validés.
            - Rachat par la moyenne : étudiant nouveau MG ≥ 9,5 ; ancien MG ≥ 9,7. Rachat par UE : nouveau avec moyenne UE ≥ 7.
            - Sinon : ECTS non validés ≤ 22 → Conseil d'École ; > 22 → Redouble / Exclu.
            - Mentions : Très Bien ≥ 16, Bien ≥ 14, Assez Bien ≥ 12, Passable ≥ 10.

            Guide de l'application :
            - Administrateur : « Utilisateurs & Accès » (approuver les comptes, rôles, adresses e-mail), « Affectations »
              (enseignant → matière → classe), « Classes & Étudiants », « Générateur & PV », « Modèles IA & ML », « Mon Profil ».
            - Assistant PV (4 étapes) : 1) import du fichier Excel (une feuille par classe, bouton « Modèle Excel » pour le gabarit),
              2) prévisualisation et contrôle par classe, 3) session et composition du jury, 4) téléchargement du PV Word d'une
              classe, archive ZIP de toutes les classes, ou envoi du PV par e-mail aux enseignants (réservé à l'administrateur).
            - Enseignant : « Mes Classes & Saisie des Notes » (notes CC, TP, examen, remarques, export du relevé Excel)
              et « Mon Profil » (mot de passe, adresse e-mail pour recevoir les PV).
            - Modèles IA & ML : diagnostic d'une classe par trois modèles (arbre de décision, KNN, forêt aléatoire)
              avec vote majoritaire, et simulateur pour un profil d'étudiant.
            - L'application desktop Windows affiche la même plateforme : toutes les fonctions y sont identiques.
            """;

    @Value("${anthropic.api-key:}")
    private String apiKey;

    @Value("${anthropic.model:claude-opus-5-5}")
    private String model;

    @Autowired
    private ChatbotKnowledgeService knowledgeService;

    @Autowired
    private LocalAssistant localAssistant;

    private volatile AnthropicClient client;

    public record ChatTurn(String role, String content) {
    }

    public record ChatReply(String reply, String mode) {
    }

    public boolean isClaudeEnabled() {
        return apiKey != null && !apiKey.isBlank();
    }

    public ChatReply reply(String message, List<ChatTurn> history, String username, boolean admin) {
        ChatbotKnowledgeService.Snapshot snapshot = knowledgeService.build(username, admin);
        if (!isClaudeEnabled()) {
            return new ChatReply(localAssistant.answer(message, snapshot), "local");
        }
        try {
            return new ChatReply(askClaude(message, history, snapshot), "claude");
        } catch (UnauthorizedException e) {
            log.warn("Clé API Claude refusée : {}", e.getMessage());
            return fallback(message, snapshot, "clé API Claude invalide");
        } catch (RateLimitException e) {
            log.warn("Limite de débit Claude atteinte : {}", e.getMessage());
            return fallback(message, snapshot, "limite de requêtes Claude atteinte, réessayez dans un instant");
        } catch (AnthropicServiceException e) {
            log.warn("Erreur API Claude ({}) : {}", e.statusCode(), e.getMessage());
            return fallback(message, snapshot, "Claude est momentanément indisponible");
        } catch (AnthropicIoException e) {
            log.warn("Connexion à l'API Claude impossible : {}", e.getMessage());
            return fallback(message, snapshot, "pas de connexion à l'API Claude");
        }
    }

    private ChatReply fallback(String message, ChatbotKnowledgeService.Snapshot snapshot, String reason) {
        return new ChatReply(localAssistant.answer(message, snapshot) + "\n\n(Réponse du mode local : " + reason + ".)", "local");
    }

    private String askClaude(String message, List<ChatTurn> history, ChatbotKnowledgeService.Snapshot snapshot) {
        MessageCreateParams.Builder params = MessageCreateParams.builder()
                .model(model)
                .maxTokens(16000L)
                // Prompt système stable + instantané des données de la plateforme (recalculé à chaque question)
                .system(SYSTEM_PROMPT + "\n" + knowledgeService.render(snapshot))
                // Questions de chat : effort faible = réponses rapides
                .outputConfig(BetaOutputConfig.builder().effort(BetaOutputConfig.Effort.LOW).build())
                // Si le modèle refuse la requête, l'API la réessaie automatiquement sur un modèle de repli
                .addBeta("server-side-fallback-2026-07-01")
                .fallbacksDefault();

        // Historique en texte seul (sans blocs de réflexion) ; il doit commencer par un message utilisateur
        List<ChatTurn> turns = history == null ? List.of()
                : history.subList(Math.max(0, history.size() - MAX_HISTORY_TURNS), history.size());
        boolean started = false;
        for (ChatTurn t : turns) {
            if (t == null || t.content() == null || t.content().isBlank()) continue;
            boolean isUser = "user".equals(t.role());
            if (!started && !isUser) continue;
            started = true;
            if (isUser) params.addUserMessage(t.content());
            else params.addAssistantMessage(t.content());
        }
        params.addUserMessage(message);

        BetaMessage response = claude().beta().messages().create(params.build());

        if (response.stopReason().map(BetaStopReason.REFUSAL::equals).orElse(false)) {
            return "Je ne peux pas répondre à cette demande. Posez-moi une question sur les classes, les notes, "
                    + "les délibérations ou l'utilisation de PV-Delib.";
        }
        String text = response.content().stream()
                .flatMap(block -> block.text().stream())
                .map(BetaTextBlock::text)
                .collect(Collectors.joining("\n"))
                .trim();
        if (response.stopReason().map(BetaStopReason.MAX_TOKENS::equals).orElse(false)) {
            text += "\n\n(Réponse tronquée.)";
        }
        return text.isEmpty() ? "Je n'ai pas pu formuler de réponse, pouvez-vous reformuler la question ?" : text;
    }

    private AnthropicClient claude() {
        if (client == null) {
            synchronized (this) {
                if (client == null) {
                    client = AnthropicOkHttpClient.builder()
                            .apiKey(apiKey)
                            .timeout(Duration.ofSeconds(90))
                            .build();
                }
            }
        }
        return client;
    }
}
