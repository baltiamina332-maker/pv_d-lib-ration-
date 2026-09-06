package com.amna.project.controllers;

import com.amna.project.dto.EnseignantStatsDTO;
import com.amna.project.dto.EtudiantRemarqueDTO;
import com.amna.project.dto.MesClassesResponseDTO;
import com.amna.project.dto.SaisieNotesRequestDTO;
import com.amna.project.entities.*;
import com.amna.project.entities.Module;
import com.amna.project.repositories.*;
import org.apache.poi.ss.usermodel.*;
import org.apache.poi.xssf.usermodel.XSSFWorkbook;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.http.HttpHeaders;
import org.springframework.http.MediaType;
import org.springframework.http.ResponseEntity;
import org.springframework.security.core.Authentication;
import org.springframework.transaction.annotation.Transactional;
import org.springframework.web.bind.annotation.*;

import java.io.ByteArrayOutputStream;
import java.io.IOException;
import java.util.ArrayList;
import java.util.HashMap;
import java.util.List;
import java.util.Map;
import java.util.Optional;

@RestController
@RequestMapping("/api")
public class EnseignantController {

    @Autowired
    private AffectationRepository affectationRepository;

    @Autowired
    private EtudiantRepository etudiantRepository;

    @Autowired
    private ClasseRepository classeRepository;

    @Autowired
    private ModuleRepository moduleRepository;

    @Autowired
    private SessionExamRepository sessionExamRepository;

    @Autowired
    private NoteCcRepository noteCcRepository;

    @Autowired
    private NoteTpRepository noteTpRepository;

    @Autowired
    private NoteExamenRepository noteExamenRepository;

    @Autowired
    private EtudiantObservationRepository etudiantObservationRepository;

    // 1. Affichage des classes & étudiants affectés (GET /api/mes-classes)
    @GetMapping("/mes-classes")
    public ResponseEntity<MesClassesResponseDTO> getMesClasses(Authentication authentication) {
        if (authentication == null || !authentication.isAuthenticated()) {
            return ResponseEntity.status(401).build();
        }

        String username = authentication.getName();
        List<Affectation> affectations = affectationRepository.findByEnseignantUsername(username);

        List<MesClassesResponseDTO.ClasseAffecteeDTO> classesDTO = new ArrayList<>();

        for (Affectation aff : affectations) {
            List<Etudiant> etudiants = etudiantRepository.findByClasseId(aff.getClasse().getId());
            if (etudiants.isEmpty()) {
                etudiants = etudiantRepository.findAll();
            }

            classesDTO.add(MesClassesResponseDTO.ClasseAffecteeDTO.builder()
                    .affectationId(aff.getId())
                    .classeId(aff.getClasse().getId())
                    .nomClasse(aff.getClasse().getNomClasse())
                    .niveau(aff.getClasse().getNiveau())
                    .matiere(aff.getMatiere())
                    .anneeUniversitaire(aff.getAnneeUniversitaire())
                    .totalEtudiants(etudiants.size())
                    .etudiants(etudiants)
                    .build());
        }

        MesClassesResponseDTO response = MesClassesResponseDTO.builder()
                .enseignantUsername(username)
                .classesAffectees(classesDTO)
                .build();

        return ResponseEntity.ok(response);
    }

    // 2. Sauvegarde des notes saisies par l'enseignant (POST /api/saisir-notes)
    @PostMapping("/saisir-notes")
    @Transactional
    public ResponseEntity<?> saveNotes(@RequestBody SaisieNotesRequestDTO requestDTO, Authentication authentication) {
        if (authentication == null || !authentication.isAuthenticated()) {
            return ResponseEntity.status(401).build();
        }

        SessionExam sessionExam = sessionExamRepository.findById(1).orElseGet(() -> 
            sessionExamRepository.save(new SessionExam(1, "Session Principale"))
        );

        if (requestDTO.getNotes() != null) {
            for (SaisieNotesRequestDTO.EtudiantNoteItemDTO item : requestDTO.getNotes()) {
                if (item.getEtudiantId() == null) continue;

                Etudiant etudiant = etudiantRepository.findById(item.getEtudiantId()).orElse(null);
                if (etudiant == null) continue;

                Module module = null;
                if (requestDTO.getModuleId() != null) {
                    module = moduleRepository.findById(requestDTO.getModuleId()).orElse(null);
                } else if (requestDTO.getMatiere() != null) {
                    module = moduleRepository.findByNomModule(requestDTO.getMatiere()).orElse(null);
                }

                if (module == null) {
                    module = moduleRepository.findAll().stream().findFirst().orElse(null);
                }

                Integer moduleId = (module != null) ? module.getIdModule() : 1;
                NoteId noteId = new NoteId(item.getEtudiantId(), moduleId, sessionExam.getIdSession());

                if (item.getNoteCc() != null) {
                    NoteCc cc = new NoteCc();
                    cc.setId(noteId);
                    cc.setEtudiant(etudiant);
                    cc.setModule(module);
                    cc.setSessionExam(sessionExam);
                    cc.setNoteCc(item.getNoteCc());
                    noteCcRepository.save(cc);
                }

                if (item.getNoteTp() != null) {
                    NoteTp tp = new NoteTp();
                    tp.setId(noteId);
                    tp.setEtudiant(etudiant);
                    tp.setModule(module);
                    tp.setSessionExam(sessionExam);
                    tp.setNoteTp(item.getNoteTp());
                    noteTpRepository.save(tp);
                }

                if (item.getNoteExam() != null) {
                    NoteExamen exam = new NoteExamen();
                    exam.setId(noteId);
                    exam.setEtudiant(etudiant);
                    exam.setModule(module);
                    exam.setSessionExam(sessionExam);
                    exam.setNoteExam(item.getNoteExam());
                    noteExamenRepository.save(exam);
                }
            }
        }

        return ResponseEntity.ok(Map.of("message", "Les notes ont été enregistrées avec succès dans la base de données MySQL !"));
    }

    // 3. Récupération des notes déjà enregistrées (GET /api/mes-classes/notes)
    @GetMapping("/mes-classes/notes")
    public ResponseEntity<List<Map<String, Object>>> getSavedNotes(@RequestParam(required = false) Long classeId,
                                                                   @RequestParam(required = false) String matiere) {
        List<Etudiant> etudiants = (classeId != null) ? etudiantRepository.findByClasseId(classeId) : etudiantRepository.findAll();
        if (etudiants.isEmpty()) {
            etudiants = etudiantRepository.findAll();
        }

        Module module = (matiere != null) ? moduleRepository.findByNomModule(matiere).orElse(null) : null;
        if (module == null) {
            module = moduleRepository.findAll().stream().findFirst().orElse(null);
        }
        Integer moduleId = (module != null) ? module.getIdModule() : 1;

        List<Map<String, Object>> result = new ArrayList<>();

        for (Etudiant e : etudiants) {
            NoteId noteId = new NoteId(e.getIdEtudiant(), moduleId, 1);

            NoteCc cc = noteCcRepository.findById(noteId).orElse(null);
            NoteTp tp = noteTpRepository.findById(noteId).orElse(null);
            NoteExamen exam = noteExamenRepository.findById(noteId).orElse(null);

            Map<String, Object> map = new HashMap<>();
            map.put("etudiantId", e.getIdEtudiant());
            map.put("nom", e.getNom());
            map.put("prenom", e.getPrenom());
            map.put("noteCc", cc != null ? cc.getNoteCc() : null);
            map.put("noteTp", tp != null ? tp.getNoteTp() : null);
            map.put("noteExam", exam != null ? exam.getNoteExam() : null);

            result.add(map);
        }

        return ResponseEntity.ok(result);
    }

    // 4. Statistiques & Diagnostic de la classe pour l'enseignant (GET /api/enseignant/stats/{classeId})
    @GetMapping("/enseignant/stats/{classeId}")
    public ResponseEntity<EnseignantStatsDTO> getEnseignantStats(@PathVariable Long classeId,
                                                                 @RequestParam(required = false) String matiere) {
        Classe classe = classeRepository.findById(classeId).orElse(null);
        String nomClasse = classe != null ? classe.getNomClasse() : "Classe #" + classeId;

        List<Etudiant> etudiants = etudiantRepository.findByClasseId(classeId);
        if (etudiants.isEmpty()) {
            etudiants = etudiantRepository.findAll();
        }

        Module module = (matiere != null) ? moduleRepository.findByNomModule(matiere).orElse(null) : null;
        if (module == null) {
            module = moduleRepository.findAll().stream().findFirst().orElse(null);
        }
        Integer moduleId = (module != null) ? module.getIdModule() : 1;

        double sumMoyenne = 0;
        int evaluesCount = 0;
        double min = 20.0;
        double max = 0.0;
        int successCount = 0;

        int countMoins8 = 0;
        int count8A10 = 0;
        int count10A14 = 0;
        int countPlus14 = 0;

        for (Etudiant e : etudiants) {
            NoteId noteId = new NoteId(e.getIdEtudiant(), moduleId, 1);
            NoteCc cc = noteCcRepository.findById(noteId).orElse(null);
            NoteTp tp = noteTpRepository.findById(noteId).orElse(null);
            NoteExamen exam = noteExamenRepository.findById(noteId).orElse(null);

            if (exam != null && exam.getNoteExam() != null) {
                double examVal = exam.getNoteExam().doubleValue();
                double ccVal = cc != null && cc.getNoteCc() != null ? cc.getNoteCc().doubleValue() : examVal;
                double tpVal = tp != null && tp.getNoteTp() != null ? tp.getNoteTp().doubleValue() : ccVal;
                double moyMatiere = (ccVal * 0.2) + (tpVal * 0.2) + (examVal * 0.6);

                evaluesCount++;
                sumMoyenne += moyMatiere;
                if (moyMatiere < min) min = moyMatiere;
                if (moyMatiere > max) max = moyMatiere;
                if (moyMatiere >= 10.0) successCount++;

                if (moyMatiere < 8.0) countMoins8++;
                else if (moyMatiere < 10.0) count8A10++;
                else if (moyMatiere < 14.0) count10A14++;
                else countPlus14++;
            }
        }

        if (evaluesCount == 0) {
            min = 0.0;
            max = 0.0;
        }

        double average = evaluesCount > 0 ? (sumMoyenne / evaluesCount) : 0.0;
        double successRate = evaluesCount > 0 ? ((double) successCount / evaluesCount * 100.0) : 0.0;

        EnseignantStatsDTO stats = EnseignantStatsDTO.builder()
                .nomClasse(nomClasse)
                .matiere(matiere != null ? matiere : "Toutes Matières")
                .totalEtudiants(etudiants.size())
                .etudiantsEvaluesCount(evaluesCount)
                .moyenneClasse(Math.round(average * 100.0) / 100.0)
                .noteMin(Math.round(min * 100.0) / 100.0)
                .noteMax(Math.round(max * 100.0) / 100.0)
                .tauxReussite(Math.round(successRate * 10.0) / 10.0)
                .countMoins8(countMoins8)
                .count8A10(count8A10)
                .count10A14(count10A14)
                .countPlus14(countPlus14)
                .build();

        return ResponseEntity.ok(stats);
    }

    // 5. Exportation Excel du relevé de notes enseignant (GET /api/enseignant/export-notes/{classeId})
    @GetMapping("/enseignant/export-notes/{classeId}")
    public ResponseEntity<byte[]> exportNotesExcel(@PathVariable Long classeId,
                                                   @RequestParam(required = false) String matiere) throws IOException {
        Classe classe = classeRepository.findById(classeId).orElse(null);
        String nomClasse = classe != null ? classe.getNomClasse() : "Classe_" + classeId;

        List<Etudiant> etudiants = etudiantRepository.findByClasseId(classeId);
        if (etudiants.isEmpty()) {
            etudiants = etudiantRepository.findAll();
        }

        Module module = (matiere != null) ? moduleRepository.findByNomModule(matiere).orElse(null) : null;
        if (module == null) {
            module = moduleRepository.findAll().stream().findFirst().orElse(null);
        }
        Integer moduleId = (module != null) ? module.getIdModule() : 1;

        Workbook workbook = new XSSFWorkbook();
        Sheet sheet = workbook.createSheet("Notes " + nomClasse);

        // Header style
        CellStyle headerStyle = workbook.createCellStyle();
        headerStyle.setFillForegroundColor(IndexedColors.DARK_BLUE.getIndex());
        headerStyle.setFillPattern(FillPatternType.SOLID_FOREGROUND);
        Font font = workbook.createFont();
        font.setColor(IndexedColors.WHITE.getIndex());
        font.setBold(true);
        headerStyle.setFont(font);
        headerStyle.setAlignment(HorizontalAlignment.CENTER);

        // Title row
        Row titleRow = sheet.createRow(0);
        Cell titleCell = titleRow.createCell(0);
        titleCell.setCellValue("RELEVÉ DE NOTES - CLASSE : " + nomClasse + " | MATIÈRE : " + (matiere != null ? matiere : "Toutes"));

        // Header row
        String[] headers = {"ID Étudiant", "Nom", "Prénom", "Note CC", "Note TP", "Note Examen", "Moyenne Matière", "Remarque / Observation Enseignant"};
        Row headerRow = sheet.createRow(2);
        for (int i = 0; i < headers.length; i++) {
            Cell cell = headerRow.createCell(i);
            cell.setCellValue(headers[i]);
            cell.setCellStyle(headerStyle);
        }

        int rowNum = 3;
        for (Etudiant e : etudiants) {
            NoteId noteId = new NoteId(e.getIdEtudiant(), moduleId, 1);
            NoteCc cc = noteCcRepository.findById(noteId).orElse(null);
            NoteTp tp = noteTpRepository.findById(noteId).orElse(null);
            NoteExamen exam = noteExamenRepository.findById(noteId).orElse(null);

            Optional<EtudiantObservation> obsOpt = (matiere != null) 
                    ? etudiantObservationRepository.findByEtudiantIdAndMatiere(e.getIdEtudiant(), matiere)
                    : Optional.empty();

            Row row = sheet.createRow(rowNum++);
            row.createCell(0).setCellValue(e.getIdEtudiant());
            row.createCell(1).setCellValue(e.getNom() != null ? e.getNom() : "");
            row.createCell(2).setCellValue(e.getPrenom() != null ? e.getPrenom() : "");

            Double ccVal = cc != null && cc.getNoteCc() != null ? cc.getNoteCc().doubleValue() : null;
            Double tpVal = tp != null && tp.getNoteTp() != null ? tp.getNoteTp().doubleValue() : null;
            Double examVal = exam != null && exam.getNoteExam() != null ? exam.getNoteExam().doubleValue() : null;

            if (ccVal != null) row.createCell(3).setCellValue(ccVal); else row.createCell(3).setCellValue("-");
            if (tpVal != null) row.createCell(4).setCellValue(tpVal); else row.createCell(4).setCellValue("-");
            if (examVal != null) row.createCell(5).setCellValue(examVal); else row.createCell(5).setCellValue("-");

            if (examVal != null) {
                double ccCalc = ccVal != null ? ccVal : examVal;
                double tpCalc = tpVal != null ? tpVal : ccCalc;
                double moy = (ccCalc * 0.2) + (tpCalc * 0.2) + (examVal * 0.6);
                row.createCell(6).setCellValue(Math.round(moy * 100.0) / 100.0);
            } else {
                row.createCell(6).setCellValue("-");
            }

            row.createCell(7).setCellValue(obsOpt.isPresent() ? obsOpt.get().getRemarque() : "");
        }

        for (int i = 0; i < headers.length; i++) {
            sheet.autoSizeColumn(i);
        }

        ByteArrayOutputStream out = new ByteArrayOutputStream();
        workbook.write(out);
        workbook.close();

        byte[] excelBytes = out.toByteArray();

        return ResponseEntity.ok()
                .header(HttpHeaders.CONTENT_DISPOSITION, "attachment; filename=Releve_Notes_" + nomClasse + ".xlsx")
                .contentType(MediaType.parseMediaType("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"))
                .body(excelBytes);
    }

    // 6. Enregistrement d'une remarque enseignant sur un étudiant (POST /api/enseignant/remarque)
    @PostMapping("/enseignant/remarque")
    public ResponseEntity<?> saveRemarque(@RequestBody EtudiantRemarqueDTO dto, Authentication authentication) {
        if (dto.getEtudiantId() == null || dto.getMatiere() == null) {
            return ResponseEntity.badRequest().body(Map.of("message", "Paramètres invalides"));
        }

        String username = (authentication != null) ? authentication.getName() : "enseignant";

        EtudiantObservation obs = etudiantObservationRepository
                .findByEtudiantIdAndMatiere(dto.getEtudiantId(), dto.getMatiere())
                .orElseGet(() -> EtudiantObservation.builder()
                        .etudiantId(dto.getEtudiantId())
                        .matiere(dto.getMatiere())
                        .build());

        obs.setEnseignantUsername(username);
        obs.setRemarque(dto.getRemarque());
        etudiantObservationRepository.save(obs);

        return ResponseEntity.ok(Map.of("message", "Remarque enseignant enregistrée avec succès !"));
    }

    // 7. Récupération des remarques par matière (GET /api/enseignant/remarques)
    @GetMapping("/enseignant/remarques")
    public ResponseEntity<List<EtudiantObservation>> getRemarques(@RequestParam String matiere) {
        List<EtudiantObservation> list = etudiantObservationRepository.findByMatiere(matiere);
        return ResponseEntity.ok(list);
    }
}

