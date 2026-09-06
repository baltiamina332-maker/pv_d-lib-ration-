package com.amna.project.services;

import org.apache.poi.ss.usermodel.*;
import org.apache.poi.xssf.usermodel.XSSFWorkbook;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.stereotype.Service;

import java.io.ByteArrayOutputStream;

@Service
public class GabaritService {
    @Autowired
    private com.amna.project.repositories.ClasseRepository classeRepository;
    
    @Autowired
    private CalculNotesService calculNotesService;

    @Autowired
    private com.amna.project.repositories.EtudiantRepository etudiantRepository;

    public byte[] generateSampleGabarit() throws Exception {
        try (Workbook workbook = new XSSFWorkbook(); ByteArrayOutputStream out = new ByteArrayOutputStream()) {

            // Header Style
            CellStyle headerStyle = workbook.createCellStyle();
            Font font = workbook.createFont();
            font.setBold(true);
            font.setColor(IndexedColors.WHITE.getIndex());
            headerStyle.setFont(font);
            headerStyle.setFillForegroundColor(IndexedColors.DARK_BLUE.getIndex());
            headerStyle.setFillPattern(FillPatternType.SOLID_FOREGROUND);
            headerStyle.setAlignment(HorizontalAlignment.CENTER);

            String[] headers = {"N°", "Nom et Prénom", "Matricule / CIN", "Moyenne générale", "ECTS non validés", "Statut", "Moyenne UE", "Décision", "Mention / Observation"};

            java.util.List<com.amna.project.entities.Classe> classes = classeRepository.findAll();

            if (classes.isEmpty()) {
                Sheet sheet = workbook.createSheet("Modèle vide");
                createHeaderRow(sheet, headers, headerStyle);
                for (int col = 0; col < headers.length; col++) {
                    sheet.autoSizeColumn(col);
                }
            } else {
                for (com.amna.project.entities.Classe classe : classes) {
                    String sheetName = classe.getNomClasse();
                    if (sheetName.length() > 31) {
                        sheetName = sheetName.substring(0, 31);
                    }
                    Sheet sheet = workbook.createSheet(sheetName);
                    createHeaderRow(sheet, headers, headerStyle);

                    java.util.List<com.amna.project.entities.Etudiant> etudiants = etudiantRepository.findByClasseId(classe.getId());
                    int r = 1;
                    int index = 1;
                    for (com.amna.project.entities.Etudiant etu : etudiants) {
                        CalculNotesService.EtudiantResultat result = calculNotesService.calculerResultat(etu);
                        
                        Row row = sheet.createRow(r++);
                        row.createCell(0).setCellValue(index++);
                        row.createCell(1).setCellValue(etu.getNom() + " " + etu.getPrenom());
                        row.createCell(2).setCellValue(String.valueOf(etu.getIdEtudiant()));
                        row.createCell(3).setCellValue(result.moyenneGenerale);
                        row.createCell(4).setCellValue(result.ectsNonValides);
                        row.createCell(5).setCellValue(""); // Statut
                        row.createCell(6).setCellValue(""); // Moyenne UE
                        row.createCell(7).setCellValue(result.decision);
                        row.createCell(8).setCellValue(result.mention);
                    }

                    for (int col = 0; col < headers.length; col++) {
                        sheet.autoSizeColumn(col);
                    }
                }
            }

            workbook.write(out);
            return out.toByteArray();
        }
    }

    private void createHeaderRow(Sheet sheet, String[] headers, CellStyle headerStyle) {
        Row headerRow = sheet.createRow(0);
        for (int i = 0; i < headers.length; i++) {
            Cell cell = headerRow.createCell(i);
            cell.setCellValue(headers[i]);
            cell.setCellStyle(headerStyle);
        }
    }
}
