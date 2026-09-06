package com.amna.project.services;

import com.amna.project.dto.EtudiantDTO;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;

import static org.junit.jupiter.api.Assertions.*;

public class DeliberationEngineTest {

    private DeliberationEngineService engine;

    @BeforeEach
    public void setUp() {
        engine = new DeliberationEngineService();
    }

    @Test
    public void testCas1_AdmisStandard() {
        EtudiantDTO e = EtudiantDTO.builder()
                .nomPrenom("Sami Ben Ali")
                .moyenneGenerale(14.5)
                .ectsNonValides(0)
                .statutEtudiant("nouveau")
                .build();

        engine.evaluateStudent(e);

        assertEquals("Admis", e.getDecision());
        assertEquals("Bien", e.getMention());
    }

    @Test
    public void testCas1_AdmisAvecEctsReserves() {
        EtudiantDTO e = EtudiantDTO.builder()
                .nomPrenom("Amira Trabelsi")
                .moyenneGenerale(11.2)
                .ectsNonValides(18)
                .statutEtudiant("nouveau")
                .build();

        engine.evaluateStudent(e);

        assertEquals("Admis avec nbr ECTS non validé", e.getDecision());
        assertEquals("Passable", e.getMention());
    }

    @Test
    public void testCas3_RachatAncienMG() {
        EtudiantDTO e = EtudiantDTO.builder()
                .nomPrenom("Yassine Sassi")
                .moyenneGenerale(9.8)
                .ectsNonValides(8)
                .statutEtudiant("ancien")
                .build();

        engine.evaluateStudent(e);

        assertEquals("Admis (Rachat Ancien)", e.getDecision());
        assertEquals("Passable (Rachat)", e.getMention());
    }

    @Test
    public void testCas3_RachatNouveauMG() {
        EtudiantDTO e = EtudiantDTO.builder()
                .nomPrenom("Karim Bouazizi")
                .moyenneGenerale(9.6)
                .ectsNonValides(10)
                .statutEtudiant("nouveau")
                .build();

        engine.evaluateStudent(e);

        assertEquals("Admis (Rachat Nouveau)", e.getDecision());
        assertEquals("Passable (Rachat)", e.getMention());
    }

    @Test
    public void testCas3_RachatUeNouveau() {
        EtudiantDTO e = EtudiantDTO.builder()
                .nomPrenom("Omar Kacem")
                .moyenneGenerale(8.8)
                .moyenneUe(7.5)
                .ectsNonValides(12)
                .statutEtudiant("nouveau")
                .build();

        engine.evaluateStudent(e);

        assertEquals("Admis (Rachat UE Nouveau)", e.getDecision());
    }

    @Test
    public void testCas2_ConseilEcole() {
        EtudiantDTO e = EtudiantDTO.builder()
                .nomPrenom("Mohamed Gharbi")
                .moyenneGenerale(8.5)
                .ectsNonValides(18)
                .statutEtudiant("nouveau")
                .build();

        engine.evaluateStudent(e);

        assertEquals("Conseil École", e.getDecision());
        assertEquals("Conseil d'École", e.getMention());
    }

    @Test
    public void testCas2_RedoubleExclu() {
        EtudiantDTO e = EtudiantDTO.builder()
                .nomPrenom("Fatma Mejri")
                .moyenneGenerale(7.0)
                .ectsNonValides(24)
                .statutEtudiant("nouveau")
                .build();

        engine.evaluateStudent(e);

        assertEquals("Redouble / Exclu", e.getDecision());
        assertEquals("Ajourné", e.getMention());
    }
}
