package com.amna.project.services;

import com.amna.project.entities.*;
import com.amna.project.entities.Module;
import com.amna.project.repositories.*;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.stereotype.Service;

import java.math.BigDecimal;
import java.math.RoundingMode;
import java.util.List;

@Service
public class CalculNotesService {

    @Autowired
    private NoteCcRepository noteCcRepository;
    
    @Autowired
    private NoteTpRepository noteTpRepository;
    
    @Autowired
    private NoteExamenRepository noteExamenRepository;

    @Autowired
    private ModuleRepository moduleRepository;

    public EtudiantResultat calculerResultat(Etudiant etudiant) {
        List<Module> modules = moduleRepository.findAll();
        
        double sommeNotesCoef = 0.0;
        double sommeCoefs = 0.0;
        int ectsNonValides = 0;

        for (Module module : modules) {
            // Find notes for this student and module (using current session if available, here we just find first or use 0)
            // Note: In a real system, you filter by session and class. We assume all notes exist or we find the latest.
            List<NoteCc> ccs = noteCcRepository.findAll(); // Optimization needed: fetch by student and module
            List<NoteTp> tps = noteTpRepository.findAll();
            List<NoteExamen> exams = noteExamenRepository.findAll();

            BigDecimal noteCc = findNoteCc(ccs, etudiant.getIdEtudiant(), module.getIdModule());
            BigDecimal noteTp = findNoteTp(tps, etudiant.getIdEtudiant(), module.getIdModule());
            BigDecimal noteExam = findNoteExamen(exams, etudiant.getIdEtudiant(), module.getIdModule());

            double cc = noteCc != null ? noteCc.doubleValue() : 0.0;
            double tp = noteTp != null ? noteTp.doubleValue() : 0.0;
            double exam = noteExam != null ? noteExam.doubleValue() : 0.0;

            // Default Formula: (CC*0.2) + (TP*0.2) + (Exam*0.6)
            // If no TP: (CC*0.3) + (Exam*0.7)
            double moyenneModule = 0.0;
            if (noteTp == null) {
                moyenneModule = (cc * 0.3) + (exam * 0.7);
            } else {
                moyenneModule = (cc * 0.2) + (tp * 0.2) + (exam * 0.6);
            }

            double coef = module.getCoef() != null ? module.getCoef().doubleValue() : 1.0;
            int ects = module.getEcts() != null ? module.getEcts() : 3;

            sommeNotesCoef += (moyenneModule * coef);
            sommeCoefs += coef;

            if (moyenneModule < 10.0) {
                ectsNonValides += ects;
            }
        }

        double moyenneGenerale = sommeCoefs > 0 ? sommeNotesCoef / sommeCoefs : 0.0;

        // Apply rules
        String decision = "";
        String mention = "";

        if (moyenneGenerale >= 10.0) {
            if (ectsNonValides <= 15) {
                decision = "Admis";
            } else {
                decision = "Admis avec nbr ECTS non validé";
            }
            if (moyenneGenerale >= 16) mention = "Très Bien";
            else if (moyenneGenerale >= 14) mention = "Bien";
            else if (moyenneGenerale >= 12) mention = "Assez Bien";
            else mention = "Passable";
        } else {
            if (ectsNonValides > 22) {
                decision = "Redouble / Exclu";
            } else {
                decision = "Conseil Ecole";
            }
            mention = "Ajourné";
        }

        EtudiantResultat res = new EtudiantResultat();
        res.moyenneGenerale = Math.round(moyenneGenerale * 100.0) / 100.0;
        res.ectsNonValides = ectsNonValides;
        res.decision = decision;
        res.mention = mention;
        return res;
    }

    private BigDecimal findNoteCc(List<NoteCc> notes, Integer etudiantId, Integer moduleId) {
        for (NoteCc n : notes) {
            if (n.getEtudiant().getIdEtudiant().equals(etudiantId) && n.getModule().getIdModule().equals(moduleId)) {
                return n.getNoteCc();
            }
        }
        return null;
    }

    private BigDecimal findNoteTp(List<NoteTp> notes, Integer etudiantId, Integer moduleId) {
        for (NoteTp n : notes) {
            if (n.getEtudiant().getIdEtudiant().equals(etudiantId) && n.getModule().getIdModule().equals(moduleId)) {
                return n.getNoteTp();
            }
        }
        return null;
    }

    private BigDecimal findNoteExamen(List<NoteExamen> notes, Integer etudiantId, Integer moduleId) {
        for (NoteExamen n : notes) {
            if (n.getEtudiant().getIdEtudiant().equals(etudiantId) && n.getModule().getIdModule().equals(moduleId)) {
                return n.getNoteExam();
            }
        }
        return null;
    }

    public static class EtudiantResultat {
        public double moyenneGenerale;
        public int ectsNonValides;
        public String decision;
        public String mention;
    }
}
