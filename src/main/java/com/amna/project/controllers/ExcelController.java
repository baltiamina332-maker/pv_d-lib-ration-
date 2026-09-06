package com.amna.project.controllers;

import com.amna.project.dto.ValidationReportDTO;
import com.amna.project.services.ExcelParserService;
import com.amna.project.services.GabaritService;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.http.HttpHeaders;
import org.springframework.http.MediaType;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.*;
import org.springframework.web.multipart.MultipartFile;

@RestController
@RequestMapping("/api/excel")
public class ExcelController {

    @Autowired
    private ExcelParserService excelParserService;

    @Autowired
    private GabaritService gabaritService;

    @PostMapping("/upload")
    public ResponseEntity<ValidationReportDTO> uploadExcelFile(
            @RequestParam("file") MultipartFile file,
            @RequestParam(value = "modeCalcul", defaultValue = "AUTO") String modeCalcul) {
        try {
            ValidationReportDTO report = excelParserService.parseExcelFile(file, modeCalcul);
            return ResponseEntity.ok(report);
        } catch (Exception e) {
            ValidationReportDTO errorReport = ValidationReportDTO.builder()
                    .valid(false)
                    .build();
            errorReport.getErrors().add("Erreur lors du traitement du fichier Excel : " + e.getMessage());
            return ResponseEntity.badRequest().body(errorReport);
        }
    }

    @GetMapping("/template")
    public ResponseEntity<byte[]> downloadGabaritTemplate() {
        try {
            byte[] excelContent = gabaritService.generateSampleGabarit();
            return ResponseEntity.ok()
                    .header(HttpHeaders.CONTENT_DISPOSITION, "attachment; filename=gabarit_pv_deliberation.xlsx")
                    .contentType(MediaType.parseMediaType("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"))
                    .body(excelContent);
        } catch (Exception e) {
            return ResponseEntity.internalServerError().build();
        }
    }
}
