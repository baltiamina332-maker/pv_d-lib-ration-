package com.amna.project.controllers;

import com.amna.project.dto.ClassePreviewDTO;
import com.amna.project.dto.PvGenerateRequestDTO;
import com.amna.project.entities.HistoriqueGenerationEntity;
import com.amna.project.repositories.HistoriqueGenerationRepository;
import com.amna.project.services.WordGeneratorService;
import com.amna.project.services.ZipExporterService;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.http.HttpHeaders;
import org.springframework.http.MediaType;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.*;

import java.time.LocalDateTime;
import java.util.HashMap;
import java.util.List;
import java.util.Map;

@RestController
@RequestMapping("/api/pv")
public class PvGeneratorController {

    @Autowired
    private WordGeneratorService wordGeneratorService;

    @Autowired
    private ZipExporterService zipExporterService;

    @Autowired
    private com.amna.project.repositories.ClasseRepository classeRepository;

    @Autowired
    private com.amna.project.repositories.EtudiantRepository etudiantRepository;

    @Autowired
    private com.amna.project.services.CalculNotesService calculNotesService;

    @Autowired
    private HistoriqueGenerationRepository historiqueRepository;

    @GetMapping("/classes-from-db")
    public ResponseEntity<List<ClassePreviewDTO>> getClassesFromDatabase() {
        try {
            List<com.amna.project.entities.Classe> classes = classeRepository.findAll();
            List<ClassePreviewDTO> list = new java.util.ArrayList<>();

            for (com.amna.project.entities.Classe c : classes) {
                List<com.amna.project.entities.Etudiant> etus = etudiantRepository.findByClasseId(c.getId());
                List<com.amna.project.dto.EtudiantDTO> dtos = new java.util.ArrayList<>();

                int idx = 1;
                int nbAdmis = 0;
                int nbAdmisEcts = 0;
                int nbRachats = 0;
                int nbConseilEcole = 0;
                int nbAjournes = 0;

                for (com.amna.project.entities.Etudiant etu : etus) {
                    com.amna.project.services.CalculNotesService.EtudiantResultat res = calculNotesService.calculerResultat(etu);
                    
                    com.amna.project.dto.EtudiantDTO eDto = com.amna.project.dto.EtudiantDTO.builder()
                            .numOrder(idx++)
                            .nomPrenom(etu.getNom() + " " + etu.getPrenom())
                            .matriculeCin(String.valueOf(etu.getIdEtudiant()))
                            .classe(c.getNomClasse())
                            .moyenneGenerale(res.moyenneGenerale)
                            .ectsNonValides(res.ectsNonValides)
                            .statutEtudiant("nouveau")
                            .decision(res.decision)
                            .mention(res.mention)
                            .build();
                    dtos.add(eDto);

                    String dec = res.decision != null ? res.decision.toUpperCase() : "";
                    if (dec.contains("CONSEIL")) nbConseilEcole++;
                    else if (dec.contains("RACHAT")) { nbRachats++; nbAdmis++; }
                    else if (dec.contains("ECTS")) { nbAdmisEcts++; nbAdmis++; }
                    else if (dec.contains("ADMIS")) nbAdmis++;
                    else nbAjournes++;
                }

                // Sort by average descending
                dtos.sort((a, b) -> Double.compare(
                        b.getMoyenneGenerale() != null ? b.getMoyenneGenerale() : 0.0,
                        a.getMoyenneGenerale() != null ? a.getMoyenneGenerale() : 0.0
                ));

                // Re-assign order numbers
                for (int i = 0; i < dtos.size(); i++) {
                    dtos.get(i).setNumOrder(i + 1);
                }

                int total = dtos.size();
                double taux = total > 0 ? Math.round(((double) nbAdmis / total) * 100.0 * 100.0) / 100.0 : 0.0;

                list.add(ClassePreviewDTO.builder()
                        .nomClasse(c.getNomClasse())
                        .etudiants(dtos)
                        .totalEtudiants(total)
                        .nbAdmis(nbAdmis)
                        .nbAdmisEcts(nbAdmisEcts)
                        .nbRachats(nbRachats)
                        .nbConseilEcole(nbConseilEcole)
                        .nbAjournes(nbAjournes)
                        .tauxReussite(taux)
                        .build());
            }

            return ResponseEntity.ok(list);
        } catch (Exception e) {
            return ResponseEntity.internalServerError().build();
        }
    }

    @PostMapping("/generate")
    public ResponseEntity<byte[]> generateSinglePv(@RequestBody PvGenerateRequestDTO request) {
        try {
            if (request.getClassesData() == null || request.getClassesData().isEmpty()) {
                return ResponseEntity.badRequest().build();
            }

            ClassePreviewDTO classeData = request.getClassesData().get(0);
            byte[] docxBytes = wordGeneratorService.generatePvWordDocument(classeData, request.getSessionInfo());

            String filename = "PV_" + sanitizeFilename(classeData.getNomClasse()) + ".docx";

            // Save Audit record
            saveAuditRecord(classeData, filename);

            return ResponseEntity.ok()
                    .header(HttpHeaders.CONTENT_DISPOSITION, "attachment; filename=" + filename)
                    .contentType(MediaType.parseMediaType("application/vnd.openxmlformats-officedocument.wordprocessingml.document"))
                    .body(docxBytes);

        } catch (Exception e) {
            return ResponseEntity.internalServerError().build();
        }
    }

    @PostMapping("/generate-batch")
    public ResponseEntity<byte[]> generateBatchPv(@RequestBody PvGenerateRequestDTO request) {
        try {
            if (request.getClassesData() == null || request.getClassesData().isEmpty()) {
                return ResponseEntity.badRequest().build();
            }

            Map<String, byte[]> filesMap = new HashMap<>();

            for (ClassePreviewDTO classeData : request.getClassesData()) {
                // If specific classes were selected, filter
                if (request.getSelectedClasses() != null && !request.getSelectedClasses().isEmpty()) {
                    if (!request.getSelectedClasses().contains(classeData.getNomClasse())) {
                        continue;
                    }
                }

                byte[] docxBytes = wordGeneratorService.generatePvWordDocument(classeData, request.getSessionInfo());
                String filename = "PV_" + sanitizeFilename(classeData.getNomClasse()) + ".docx";
                filesMap.put(filename, docxBytes);

                // Save Audit record
                saveAuditRecord(classeData, filename);
            }

            byte[] zipBytes = zipExporterService.createZipArchive(filesMap);
            String zipFilename = "PV_Deliberations_Batch.zip";

            return ResponseEntity.ok()
                    .header(HttpHeaders.CONTENT_DISPOSITION, "attachment; filename=" + zipFilename)
                    .contentType(MediaType.parseMediaType("application/zip"))
                    .body(zipBytes);

        } catch (Exception e) {
            return ResponseEntity.internalServerError().build();
        }
    }

    @GetMapping("/history")
    public ResponseEntity<List<HistoriqueGenerationEntity>> getGenerationHistory() {
        List<HistoriqueGenerationEntity> history = historiqueRepository.findAllByOrderByDateGenerationDesc();
        return ResponseEntity.ok(history);
    }

    @GetMapping("/history/my")
    public ResponseEntity<List<HistoriqueGenerationEntity>> getMyGenerationHistory() {
        String currentUser = "unknown";
        org.springframework.security.core.Authentication auth = org.springframework.security.core.context.SecurityContextHolder.getContext().getAuthentication();
        if (auth != null && auth.isAuthenticated()) {
            currentUser = auth.getName();
        }
        
        List<HistoriqueGenerationEntity> history = historiqueRepository.findAllByOrderByDateGenerationDesc();
        String finalCurrentUser = currentUser;
        List<HistoriqueGenerationEntity> myHistory = history.stream()
            .filter(h -> finalCurrentUser.equals(h.getCreatedBy()))
            .toList();
            
        return ResponseEntity.ok(myHistory);
    }

    private void saveAuditRecord(ClassePreviewDTO classeData, String filename) {
        try {
            String currentUser = "unknown";
            org.springframework.security.core.Authentication auth = org.springframework.security.core.context.SecurityContextHolder.getContext().getAuthentication();
            if (auth != null && auth.isAuthenticated()) {
                currentUser = auth.getName();
            }

            HistoriqueGenerationEntity record = HistoriqueGenerationEntity.builder()
                    .nomClasse(classeData.getNomClasse())
                    .filename(filename)
                    .dateGeneration(LocalDateTime.now())
                    .totalEtudiants(classeData.getTotalEtudiants())
                    .nbAdmis(classeData.getNbAdmis())
                    .nbRachats(classeData.getNbRachats() + classeData.getNbAdmisEcts())
                    .nbAjournes(classeData.getNbAjournes())
                    .tauxReussite(classeData.getTauxReussite())
                    .createdBy(currentUser)
                    .build();
            historiqueRepository.save(record);
        } catch (Exception ignored) {}
    }

    private String sanitizeFilename(String name) {
        if (name == null) return "Classe";
        return name.replaceAll("[^a-zA-Z0-9._-]", "_");
    }
}
