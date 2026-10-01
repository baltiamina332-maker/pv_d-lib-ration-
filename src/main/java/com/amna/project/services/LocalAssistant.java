package com.amna.project.services;

import com.amna.project.entities.HistoriqueGenerationEntity;
import com.amna.project.services.ChatbotKnowledgeService.ClassInfo;
import com.amna.project.services.ChatbotKnowledgeService.Snapshot;
import com.amna.project.services.ChatbotKnowledgeService.StudentInfo;
import org.springframework.stereotype.Component;

import java.text.Normalizer;
import java.time.format.DateTimeFormatter;
import java.util.List;
import java.util.Locale;
import java.util.stream.Collectors;

/**
 * Assistant de secours sans IA : répond aux questions fréquentes à partir des données
 * de la plateforme lorsque l'API Claude n'est pas configurée ou indisponible.
 */
@Component
public class LocalAssistant {

    private static final DateTimeFormatter FMT = DateTimeFormatter.ofPattern("dd/MM/yyyy HH:mm");

    public String answer(String question, Snapshot s) {
        String q = normalize(question);
        ClassInfo classe = findClass(q, s.classes());

        if (has(q, "comment", "ou trouver", "ou est", "etape", "how", "procedure", "faire pour")) {
            String guide = howTo(q, s.admin());
            if (guide != null) return guide;
        }
        if (classe == null && has(q, "regle", "rachat", "mention", "lmd", "seuil", "credit")) {
            return rules();
        }
        if (has(q, "combien", "nombre", "nbr")) {
            return counts(q, classe, s);
        }
        if (classe != null) {
            if (has(q, "enseignant", "prof", "affect", "matiere")) return teachers(classe);
            if (has(q, "meilleur", "major", "premier", "top", "classement", "rang")) return ranking(classe);
            if (has(q, "risque", "echec", "ajourne", "conseil", "redoubl", "faible", "exclu")) return atRisk(classe);
            if (has(q, "liste", "etudiant", "eleve")) return students(classe);
            return summary(classe);
        }
        if (has(q, "meilleur", "major", "premier")) {
            return s.classes().stream().filter(c -> !c.etudiants().isEmpty())
                    .map(c -> "• " + c.nom() + " : **" + c.etudiants().get(0).nomPrenom() + "** ("
                            + fmt(c.etudiants().get(0).moyenne()) + "/20)")
                    .collect(Collectors.joining("\n", "Major de chaque classe :\n", ""));
        }
        if (has(q, "historique", "dernier pv", "derniers pv", "pv genere", "generes", "generation")) {
            return history(s.historique());
        }
        if (has(q, "classe")) {
            return classes(s);
        }
        if (has(q, "taux", "reussite", "statistique", "resume", "bilan", "resultat")) {
            return s.classes().stream().map(this::oneLine)
                    .collect(Collectors.joining("\n", "Résumé par classe :\n", ""));
        }
        if (has(q, "bonjour", "salut", "aide", "help", "bonsoir", "que peux", "qui es")) {
            return intro(s);
        }
        return "Je n'ai pas compris la question en mode local. Exemples de questions :\n"
                + "• « Combien d'étudiants en 4 SAE ? »\n"
                + "• « Qui est le major de 3 LSI ? »\n"
                + "• « Quels étudiants sont à risque en 4 SAE ? »\n"
                + "• « Comment envoyer un PV par e-mail ? »\n"
                + "• « Explique les règles de rachat »\n\n"
                + "Pour des réponses libres, l'administrateur peut activer l'IA Claude (variable ANTHROPIC_API_KEY).";
    }

    private String intro(Snapshot s) {
        return "Bonjour " + s.username() + " ! Je suis l'assistant PV-Delib. Je connais "
                + s.classes().size() + " classe(s) et " + s.totalEtudiants() + " étudiant(s).\n"
                + "Je peux vous donner les résultats d'une classe, le classement, les étudiants à risque, "
                + "les enseignants affectés, l'historique des PV, expliquer les règles LMD "
                + "et vous guider dans l'application (import Excel, génération et envoi des PV, saisie des notes).";
    }

    private String counts(String q, ClassInfo classe, Snapshot s) {
        if (has(q, "classe") && classe == null && !has(q, "etudiant", "eleve")) {
            return "Il y a **" + s.classes().size() + "** classe(s) : "
                    + s.classes().stream().map(ClassInfo::nom).collect(Collectors.joining(", ")) + ".";
        }
        if (has(q, "enseignant", "prof")) {
            if (classe != null) return teachers(classe);
            long n = s.utilisateurs().stream().filter(u -> u.getRole() != null && u.getRole().name().equals("ROLE_USER")).count();
            return s.admin() ? "Il y a **" + n + "** compte(s) enseignant." : "Cette information est réservée à l'administrateur.";
        }
        if (classe != null) {
            if (has(q, "admis")) return "En " + classe.nom() + " : **" + classe.nbAdmis() + "** admis sur " + classe.etudiants().size() + ".";
            if (has(q, "conseil")) return "En " + classe.nom() + " : **" + classe.count("CONSEIL") + "** étudiant(s) en conseil d'école.";
            if (has(q, "redoubl", "exclu", "ajourne")) return "En " + classe.nom() + " : **" + classe.count("REDOUBLE") + "** redoublant(s) / exclu(s) et "
                    + classe.count("CONSEIL") + " en conseil d'école.";
            return "La classe " + classe.nom() + " compte **" + classe.etudiants().size() + "** étudiant(s).";
        }
        return s.classes().stream().map(c -> "• " + c.nom() + " : " + c.etudiants().size() + " étudiant(s)")
                .collect(Collectors.joining("\n", "Il y a **" + s.totalEtudiants() + "** étudiant(s) au total :\n", ""));
    }

    private String summary(ClassInfo c) {
        return "**Classe " + c.nom() + "**" + (c.niveau() != null ? " (" + c.niveau() + ")" : "") + "\n"
                + "• Effectif : " + c.etudiants().size() + "\n"
                + "• Admis : " + c.nbAdmis() + " — taux de réussite " + fmt(c.tauxReussite()) + " %\n"
                + "• Conseil d'école : " + c.count("CONSEIL") + " ; redouble / exclu : " + c.count("REDOUBLE") + "\n"
                + "• Moyenne de la classe : " + fmt(c.moyenneClasse()) + "/20\n"
                + "• Enseignants : " + (c.enseignants().isEmpty() ? "aucun" : String.join(", ", c.enseignants()));
    }

    private String oneLine(ClassInfo c) {
        return "• " + c.nom() + " : " + c.etudiants().size() + " étudiant(s), " + c.nbAdmis() + " admis ("
                + fmt(c.tauxReussite()) + " %), moyenne " + fmt(c.moyenneClasse()) + "/20";
    }

    private String teachers(ClassInfo c) {
        return c.enseignants().isEmpty() ? "Aucun enseignant n'est affecté à la classe " + c.nom() + "."
                : "Enseignants affectés à " + c.nom() + " :\n• " + String.join("\n• ", c.enseignants());
    }

    private String ranking(ClassInfo c) {
        if (c.etudiants().isEmpty()) return "La classe " + c.nom() + " n'a pas d'étudiant.";
        StringBuilder sb = new StringBuilder("Classement de " + c.nom() + " :\n");
        int rang = 1;
        for (StudentInfo e : c.etudiants().subList(0, Math.min(5, c.etudiants().size()))) {
            sb.append(rang++).append(". **").append(e.nomPrenom()).append("** — ").append(fmt(e.moyenne()))
                    .append("/20, ").append(e.decision()).append('\n');
        }
        return sb.toString().trim();
    }

    private String atRisk(ClassInfo c) {
        List<StudentInfo> risque = c.etudiants().stream().filter(e -> !e.isAdmis()).toList();
        if (risque.isEmpty()) return "Aucun étudiant à risque en " + c.nom() + " : tous sont admis.";
        return risque.stream().map(e -> "• " + e.nomPrenom() + " — " + fmt(e.moyenne()) + "/20, "
                        + e.ectsNonValides() + " ECTS non validés → " + e.decision())
                .collect(Collectors.joining("\n", "Étudiants non admis en " + c.nom() + " (" + risque.size() + ") :\n", ""));
    }

    private String students(ClassInfo c) {
        if (c.etudiants().isEmpty()) return "La classe " + c.nom() + " n'a pas d'étudiant.";
        return c.etudiants().stream().map(e -> "• " + e.nomPrenom() + " (" + e.matricule() + ") — " + fmt(e.moyenne()) + "/20, " + e.decision())
                .collect(Collectors.joining("\n", "Étudiants de " + c.nom() + " :\n", ""));
    }

    private String classes(Snapshot s) {
        if (s.classes().isEmpty()) return "Aucune classe visible pour votre compte.";
        return s.classes().stream().map(this::oneLine).collect(Collectors.joining("\n", "Classes :\n", ""));
    }

    private String history(List<HistoriqueGenerationEntity> h) {
        if (h.isEmpty()) return "Aucun PV n'a encore été généré.";
        return h.stream().limit(5).map(x -> "• " + (x.getDateGeneration() != null ? x.getDateGeneration().format(FMT) : "?")
                        + " — " + x.getNomClasse() + " (" + x.getFilename() + "), taux " + fmt(x.getTauxReussite() != null ? x.getTauxReussite() : 0) + " %, par " + x.getCreatedBy())
                .collect(Collectors.joining("\n", "Derniers PV générés :\n", ""));
    }

    private String rules() {
        return "**Règles de délibération LMD**\n"
                + "• MG ≥ 10 et ECTS non validés ≤ 15 → Admis (mention : TB ≥ 16, B ≥ 14, AB ≥ 12, P ≥ 10)\n"
                + "• MG ≥ 10 et ECTS non validés > 15 → Admis avec ECTS non validés\n"
                + "• Rachat par la moyenne : nouveau MG ≥ 9,5 ; ancien MG ≥ 9,7 → Admis (rachat)\n"
                + "• Rachat par UE : nouveau avec moyenne UE ≥ 7 → Admis (rachat UE)\n"
                + "• Sinon : ECTS non validés ≤ 22 → Conseil d'École ; > 22 → Redouble / Exclu";
    }

    private String howTo(String q, boolean admin) {
        if (has(q, "mail", "envoy")) {
            return admin ? "Pour envoyer un PV par e-mail :\n1. Générateur & PV → Assistant PV, importez le fichier Excel.\n"
                    + "2. Vérifiez la prévisualisation, puis renseignez la session et le jury.\n"
                    + "3. À l'étape 4, carte « Envoyer le PV par e-mail » : choisissez la classe, cochez les enseignants et cliquez sur Envoyer.\n"
                    + "Les enseignants doivent avoir une adresse e-mail (Utilisateurs & Accès ou Mon Profil)."
                    : "L'envoi des PV par e-mail est réservé à l'administrateur. Vérifiez que votre adresse e-mail est renseignée dans Mon Profil pour les recevoir.";
        }
        if (has(q, "import", "excel", "fichier")) {
            return "Pour importer un fichier Excel : Générateur & PV → Assistant PV → étape 1, déposez le fichier .xlsx "
                    + "(une feuille par classe) puis cliquez sur « Analyser et Continuer ». Le bouton « Modèle Excel » donne un gabarit.";
        }
        if (has(q, "note", "saisi")) {
            return "Pour saisir les notes : Mes Classes & Saisie des Notes → choisissez la classe, remplissez CC, TP et examen, puis « Enregistrer les Notes ».";
        }
        if (has(q, " ia", "l'ia", "machine learning", "diagnostic", "predi", "modele")) {
            return "Pour le diagnostic IA : Modèles IA & ML → « Diagnostic IA par Classe », choisissez la classe puis « Exécuter le Diagnostic IA (3 Modèles) ».";
        }
        if (has(q, "mot de passe", "profil")) {
            return "Mon Profil → « Changer le mot de passe » ; vous pouvez aussi y renseigner votre adresse e-mail.";
        }
        if (has(q, "pv", "proces", "gener", "word", "zip")) {
            return "Pour générer un PV : Générateur & PV → Assistant PV. Étape 1 import Excel, étape 2 contrôle, étape 3 session et jury, "
                    + "étape 4 « Télécharger le PV Word » (une classe) ou « Générer l'Archive ZIP » (toutes les classes).";
        }
        return null;
    }

    private ClassInfo findClass(String q, List<ClassInfo> classes) {
        String compact = q.replace(" ", "");
        ClassInfo best = null;
        for (ClassInfo c : classes) {
            String name = normalize(c.nom()).replace(" ", "");
            if (!name.isEmpty() && compact.contains(name) && (best == null || name.length() > normalize(best.nom()).replace(" ", "").length())) {
                best = c;
            }
        }
        return best;
    }

    private static boolean has(String q, String... words) {
        for (String w : words) if (q.contains(w)) return true;
        return false;
    }

    private static String normalize(String s) {
        String n = Normalizer.normalize(s == null ? "" : s, Normalizer.Form.NFD).replaceAll("\\p{M}", "");
        return n.toLowerCase(Locale.ROOT).replace('’', '\'');
    }

    private static String fmt(double v) {
        return String.format(Locale.FRANCE, "%.2f", v);
    }
}
