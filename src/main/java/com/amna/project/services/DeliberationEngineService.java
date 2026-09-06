package com.amna.project.services;

import com.amna.project.dto.ClassePreviewDTO;
import com.amna.project.dto.EtudiantDTO;
import org.springframework.stereotype.Service;

import java.util.List;

@Service
public class DeliberationEngineService {

    /**
     * Applies deliberation decision rules for a list of students and calculates class statistics.
     */
    public ClassePreviewDTO processClasse(String nomClasse, List<EtudiantDTO> etudiants, String modeCalcul) {
        int nbAdmis = 0;
        int nbAdmisEcts = 0;
        int nbRachats = 0;
        int nbConseilEcole = 0;
        int nbAjournes = 0;

        for (EtudiantDTO e : etudiants) {
            e.setClasse(nomClasse);

            if ("AUTO".equalsIgnoreCase(modeCalcul) || e.getDecision() == null || e.getDecision().isBlank()) {
                evaluateStudent(e);
            } else {
                // Pre-calculated mode, calculate mentions if missing
                if (e.getMention() == null || e.getMention().isBlank()) {
                    e.setMention(calculateMention(e.getMoyenneGenerale()));
                }
            }

            // Categorize for class synthesis statistics
            String d = e.getDecision() != null ? e.getDecision().toUpperCase() : "";
            if (d.contains("CONSEIL")) {
                nbConseilEcole++;
            } else if (d.contains("RACHAT")) {
                nbRachats++;
                nbAdmis++;
            } else if (d.contains("ECTS") || d.contains("RÉSERVE")) {
                nbAdmisEcts++;
                nbAdmis++;
            } else if (d.contains("ADMIS")) {
                nbAdmis++;
            } else {
                nbAjournes++;
            }
        }

        // Sort students: First by status (Admis > Rachat > Conseil > Ajourné), then by Moyenne Générale descending
        etudiants.sort((a, b) -> {
            Double moyA = a.getMoyenneGenerale() != null ? a.getMoyenneGenerale() : 0.0;
            Double moyB = b.getMoyenneGenerale() != null ? b.getMoyenneGenerale() : 0.0;
            return Double.compare(moyB, moyA);
        });

        // Reassign consecutive clean 1..N order numbers
        for (int i = 0; i < etudiants.size(); i++) {
            etudiants.get(i).setNumOrder(i + 1);
        }

        int total = etudiants.size();
        double tauxReussite = total > 0 ? Math.round(((double) nbAdmis / total) * 100.0 * 100.0) / 100.0 : 0.0;

        return ClassePreviewDTO.builder()
                .nomClasse(nomClasse)
                .etudiants(etudiants)
                .totalEtudiants(total)
                .nbAdmis(nbAdmis)
                .nbAdmisEcts(nbAdmisEcts)
                .nbRachats(nbRachats)
                .nbConseilEcole(nbConseilEcole)
                .nbAjournes(nbAjournes)
                .tauxReussite(tauxReussite)
                .build();
    }

    /**
     * Evaluates a single student according to custom institution rules:
     * 
     * 1. Cas 1: MG >= 10
     *    - ECTS non valides <= 15 -> Admis
     *    - ECTS non valides entre 15 et 22 -> Admis avec nbr ECTS non validé
     * 
     * 2. Cas 2: MG < 10 (hors rachat)
     *    - ECTS non valides > 22 -> Redouble / Exclu
     *    - ECTS non valides < 22 -> Conseil École
     * 
     * 3. Cas 3: Rachat
     *    - Rachat par MG:
     *      - MG >= 9.7 et statut == 'ancien' -> Admis (Rachat)
     *      - MG >= 9.5 et statut == 'nouveau' -> Admis (Rachat)
     *    - Rachat par UE:
     *      - MG >= 10 et statut == 'ancien' -> Admis (Rachat UE)
     *      - Moyenne UE >= 7 et statut == 'nouveau' -> Admis (Rachat UE)
     */
    public void evaluateStudent(EtudiantDTO e) {
        double mg = e.getMoyenneGenerale() != null ? e.getMoyenneGenerale() : 0.0;
        int ects = e.getEctsNonValides() != null ? e.getEctsNonValides() : 0;
        String statut = e.getStatutEtudiant() != null ? e.getStatutEtudiant().trim().toLowerCase() : "nouveau";
        Double moyUe = e.getMoyenneUe();

        boolean isAncien = "ancien".equals(statut);

        // --- CAS 1: MG >= 10 ---
        if (mg >= 10.0) {
            if (ects <= 15) {
                e.setDecision("Admis");
                e.setMention(calculateMention(mg));
            } else if (ects > 15 && ects <= 22) {
                e.setDecision("Admis avec nbr ECTS non validé");
                e.setMention(calculateMention(mg));
            } else {
                // ECTS > 22 while MG >= 10 -> Conseil Ecole or Admis ECTS
                e.setDecision("Admis avec nbr ECTS non validé");
                e.setMention(calculateMention(mg));
            }
            return;
        }

        // --- CAS 3: RACHAT (MG < 10) ---
        // Sub-case 3.1: Rachat avec Moyenne Generale
        if (isAncien && mg >= 9.7) {
            e.setDecision("Admis (Rachat Ancien)");
            e.setMention("Passable (Rachat)");
            return;
        }

        if (!isAncien && mg >= 9.5) {
            e.setDecision("Admis (Rachat Nouveau)");
            e.setMention("Passable (Rachat)");
            return;
        }

        // Sub-case 3.2: Rachat avec UE
        if (isAncien && mg >= 10.0) {
            e.setDecision("Admis (Rachat UE Ancien)");
            e.setMention("Passable (Rachat)");
            return;
        }

        if (!isAncien && moyUe != null && moyUe >= 7.0) {
            e.setDecision("Admis (Rachat UE Nouveau)");
            e.setMention("Passable (Rachat)");
            return;
        }

        // --- CAS 2: MG < 10 (Hors Rachat) ---
        if (ects > 22) {
            e.setDecision("Redouble / Exclu");
            e.setMention("Ajourné");
        } else {
            e.setDecision("Conseil École");
            e.setMention("Conseil d'École");
        }
    }

    public String calculateMention(double moyenne) {
        if (moyenne >= 16.0) {
            return "Très Bien";
        } else if (moyenne >= 14.0) {
            return "Bien";
        } else if (moyenne >= 12.0) {
            return "Assez Bien";
        } else if (moyenne >= 10.0) {
            return "Passable";
        } else {
            return "Ajourné";
        }
    }
}
