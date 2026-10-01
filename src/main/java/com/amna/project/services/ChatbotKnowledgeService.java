package com.amna.project.services;

import com.amna.project.entities.Affectation;
import com.amna.project.entities.Classe;
import com.amna.project.entities.Etudiant;
import com.amna.project.entities.HistoriqueGenerationEntity;
import com.amna.project.entities.UserEntity;
import com.amna.project.repositories.AffectationRepository;
import com.amna.project.repositories.ClasseRepository;
import com.amna.project.repositories.EtudiantRepository;
import com.amna.project.repositories.HistoriqueGenerationRepository;
import com.amna.project.repositories.UserRepository;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.time.LocalDate;
import java.time.format.DateTimeFormatter;
import java.util.*;

/**
 * Construit un instantané des données de la plateforme (classes, étudiants, enseignants,
 * historique des PV) utilisé par le chatbot pour répondre aux questions.
 * Un enseignant ne voit que les classes qui lui sont affectées.
 */
@Service
public class ChatbotKnowledgeService {

    private static final DateTimeFormatter FMT = DateTimeFormatter.ofPattern("dd/MM/yyyy HH:mm");

    @Autowired
    private ClasseRepository classeRepository;
    @Autowired
    private EtudiantRepository etudiantRepository;
    @Autowired
    private AffectationRepository affectationRepository;
    @Autowired
    private HistoriqueGenerationRepository historiqueRepository;
    @Autowired
    private UserRepository userRepository;
    @Autowired
    private CalculNotesService calculNotesService;

    public record StudentInfo(String nomPrenom, String matricule, double moyenne, int ectsNonValides,
                              String decision, String mention) {
        public boolean isAdmis() {
            return decision != null && decision.toUpperCase().contains("ADMIS");
        }
    }

    public record ClassInfo(String nom, String niveau, String annee, List<StudentInfo> etudiants,
                            List<String> enseignants) {
        public long nbAdmis() {
            return etudiants.stream().filter(StudentInfo::isAdmis).count();
        }

        public long count(String decisionPart) {
            return etudiants.stream()
                    .filter(e -> e.decision() != null && e.decision().toUpperCase().contains(decisionPart))
                    .count();
        }

        public double tauxReussite() {
            return etudiants.isEmpty() ? 0.0 : Math.round(nbAdmis() * 1000.0 / etudiants.size()) / 10.0;
        }

        public double moyenneClasse() {
            return etudiants.stream().mapToDouble(StudentInfo::moyenne).average().orElse(0.0);
        }
    }

    public record Snapshot(String username, boolean admin, List<ClassInfo> classes,
                           List<HistoriqueGenerationEntity> historique, List<UserEntity> utilisateurs) {
        public int totalEtudiants() {
            return classes.stream().mapToInt(c -> c.etudiants().size()).sum();
        }
    }

    @Transactional(readOnly = true)
    public Snapshot build(String username, boolean admin) {
        List<Affectation> affectations = affectationRepository.findAll();

        Set<Long> classesVisibles = new HashSet<>();
        if (!admin) {
            for (Affectation a : affectations) {
                if (a.getEnseignant() != null && a.getClasse() != null
                        && username.equals(a.getEnseignant().getUsername())) {
                    classesVisibles.add(a.getClasse().getId());
                }
            }
        }

        List<ClassInfo> classes = new ArrayList<>();
        for (Classe c : classeRepository.findAll()) {
            if (!admin && !classesVisibles.contains(c.getId())) continue;

            List<StudentInfo> etudiants = new ArrayList<>();
            for (Etudiant e : etudiantRepository.findByClasseId(c.getId())) {
                CalculNotesService.EtudiantResultat r = calculNotesService.calculerResultat(e);
                etudiants.add(new StudentInfo((e.getNom() + " " + e.getPrenom()).trim(),
                        String.valueOf(e.getIdEtudiant()), r.moyenneGenerale, r.ectsNonValides, r.decision, r.mention));
            }
            etudiants.sort(Comparator.comparingDouble(StudentInfo::moyenne).reversed());

            List<String> enseignants = new ArrayList<>();
            for (Affectation a : affectations) {
                if (a.getClasse() == null || !c.getId().equals(a.getClasse().getId()) || a.getEnseignant() == null) continue;
                String matiere = a.getMatiere() != null ? a.getMatiere()
                        : (a.getModule() != null ? a.getModule().getNomModule() : "?");
                enseignants.add(a.getEnseignant().getUsername() + " (" + matiere + ")");
            }
            classes.add(new ClassInfo(c.getNomClasse(), c.getNiveau(), c.getAnneeUniversitaire(), etudiants, enseignants));
        }

        List<HistoriqueGenerationEntity> historique = historiqueRepository.findAllByOrderByDateGenerationDesc();
        if (!admin) {
            historique = historique.stream().filter(h -> username.equals(h.getCreatedBy())).toList();
        }
        historique = historique.stream().limit(15).toList();

        List<UserEntity> utilisateurs = admin ? userRepository.findAll() : List.of();
        return new Snapshot(username, admin, classes, historique, utilisateurs);
    }

    /** Rendu texte de l'instantané pour le prompt système de Claude. */
    public String render(Snapshot s) {
        StringBuilder sb = new StringBuilder();
        sb.append("<donnees_plateforme>\n");
        sb.append("Date du jour : ").append(LocalDate.now().format(DateTimeFormatter.ofPattern("dd/MM/yyyy"))).append('\n');
        sb.append("Utilisateur connecté : ").append(s.username())
                .append(s.admin() ? " (administrateur, voit toutes les classes)" : " (enseignant, ne voit que ses classes)").append('\n');
        sb.append("Nombre de classes : ").append(s.classes().size())
                .append(" ; nombre total d'étudiants : ").append(s.totalEtudiants()).append("\n\n");

        for (ClassInfo c : s.classes()) {
            sb.append("## Classe ").append(c.nom());
            if (c.niveau() != null) sb.append(" — ").append(c.niveau());
            if (c.annee() != null) sb.append(" — ").append(c.annee());
            sb.append('\n');
            sb.append("Enseignants affectés : ").append(c.enseignants().isEmpty() ? "aucun" : String.join(", ", c.enseignants())).append('\n');
            sb.append(String.format(Locale.FRANCE, "Effectif %d ; admis %d ; conseil d'école %d ; redouble/exclu %d ; taux de réussite %.1f %% ; moyenne de classe %.2f%n",
                    c.etudiants().size(), c.nbAdmis(), c.count("CONSEIL"), c.count("REDOUBLE"), c.tauxReussite(), c.moyenneClasse()));
            sb.append("Étudiants (classés par moyenne générale décroissante) :\n");
            int rang = 1;
            for (StudentInfo e : c.etudiants()) {
                sb.append(String.format(Locale.FRANCE, "%d. %s (matricule %s) — MG %.2f/20 ; ECTS non validés %d ; décision : %s ; mention : %s%n",
                        rang++, e.nomPrenom(), e.matricule(), e.moyenne(), e.ectsNonValides(), e.decision(), e.mention()));
            }
            sb.append('\n');
        }

        sb.append("## Historique des PV générés (les plus récents)\n");
        if (s.historique().isEmpty()) sb.append("Aucun PV généré.\n");
        for (HistoriqueGenerationEntity h : s.historique()) {
            sb.append(String.format(Locale.FRANCE, "- %s : classe %s, fichier %s, effectif %d, admis %d, rachats %d, ajournés %d, taux %.1f %%, par %s%n",
                    h.getDateGeneration() != null ? h.getDateGeneration().format(FMT) : "?", h.getNomClasse(), h.getFilename(),
                    nz(h.getTotalEtudiants()), nz(h.getNbAdmis()), nz(h.getNbRachats()), nz(h.getNbAjournes()),
                    h.getTauxReussite() != null ? h.getTauxReussite() : 0.0, h.getCreatedBy()));
        }

        if (s.admin()) {
            sb.append("\n## Comptes utilisateurs\n");
            for (UserEntity u : s.utilisateurs()) {
                sb.append("- ").append(u.getUsername())
                        .append(u.getRole() != null && u.getRole().name().equals("ROLE_ADMIN") ? " (administrateur)" : " (enseignant)")
                        .append(u.isApproved() ? ", compte approuvé" : ", en attente d'approbation")
                        .append(u.getEmail() != null ? ", e-mail renseigné" : ", sans e-mail").append('\n');
            }
        }
        sb.append("</donnees_plateforme>");
        return sb.toString();
    }

    private static int nz(Integer v) {
        return v != null ? v : 0;
    }
}
