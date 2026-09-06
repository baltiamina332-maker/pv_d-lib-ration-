package com.amna.project.services;

import com.amna.project.dto.ClassePreviewDTO;
import com.amna.project.dto.EtudiantDTO;
import com.amna.project.dto.SessionDeliberationDTO;
import com.amna.project.dto.ValidationReportDTO;
import org.apache.poi.ss.usermodel.*;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.stereotype.Service;
import org.springframework.web.multipart.MultipartFile;

import java.io.InputStream;
import java.text.DecimalFormat;
import java.util.*;

@Service
public class ExcelParserService {

    @Autowired
    private DeliberationEngineService deliberationEngine;

    public ValidationReportDTO parseExcelFile(MultipartFile file, String modeCalcul) throws Exception {
        ValidationReportDTO report = new ValidationReportDTO();
        List<String> errors = new ArrayList<>();
        List<String> warnings = new ArrayList<>();
        List<ClassePreviewDTO> classes = new ArrayList<>();

        if (file.isEmpty()) {
            errors.add("Le fichier fourni est vide.");
            report.setValid(false);
            report.setErrors(errors);
            return report;
        }

        try (InputStream is = file.getInputStream(); Workbook workbook = WorkbookFactory.create(is)) {
            int numberOfSheets = workbook.getNumberOfSheets();
            Map<String, List<EtudiantDTO>> classeStudentsMap = new LinkedHashMap<>();

            // Check if there is a header/metadata sheet
            Sheet headerSheet = workbook.getSheet("En-tete");
            if (headerSheet == null) headerSheet = workbook.getSheet("En-tête");
            if (headerSheet == null) headerSheet = workbook.getSheet("Header");

            // Process sheets
            for (int i = 0; i < numberOfSheets; i++) {
                Sheet sheet = workbook.getSheetAt(i);
                String sheetName = sheet.getSheetName().trim();

                // Skip header sheet when looking for student data
                if (sheetName.equalsIgnoreCase("En-tete") || sheetName.equalsIgnoreCase("En-tête") 
                        || sheetName.equalsIgnoreCase("Header") || sheetName.equalsIgnoreCase("Config")) {
                    continue;
                }

                // Check header row (Row 0)
                Row headerRow = sheet.getRow(0);
                if (headerRow == null) {
                    warnings.add("La feuille '" + sheetName + "' est vide ou ne contient aucun en-tête.");
                    continue;
                }

                Map<String, Integer> colIndexMap = mapColumns(headerRow);
                if (!colIndexMap.containsKey("nom") && !colIndexMap.containsKey("matricule")) {
                    warnings.add("La feuille '" + sheetName + "' ne possède pas les colonnes d'étudiants requises.");
                    continue;
                }

                // Parse rows
                List<EtudiantDTO> studentList = parseSheetRows(sheet, colIndexMap, sheetName, errors, warnings);

                // Group by class if single-sheet format with a Classe column
                if (colIndexMap.containsKey("classe")) {
                    for (EtudiantDTO e : studentList) {
                        String cName = (e.getClasse() != null && !e.getClasse().isBlank()) ? e.getClasse().trim() : sheetName.trim();
                        classeStudentsMap.computeIfAbsent(cName, k -> new ArrayList<>()).add(e);
                    }
                } else {
                    classeStudentsMap.put(sheetName.trim(), studentList);
                }
            }

            // Sort classes alphabetically / naturally
            List<String> sortedClassNames = new ArrayList<>(classeStudentsMap.keySet());
            Collections.sort(sortedClassNames, String.CASE_INSENSITIVE_ORDER);

            // Apply deliberation engine rules per class
            for (String className : sortedClassNames) {
                List<EtudiantDTO> students = classeStudentsMap.get(className);

                // Fix ordering numbers
                for (int idx = 0; idx < students.size(); idx++) {
                    students.get(idx).setNumOrder(idx + 1);
                }

                ClassePreviewDTO preview = deliberationEngine.processClasse(className, students, modeCalcul);
                classes.add(preview);
            }

            report.setTotalClasses(classes.size());
            report.setClasses(classes);
            report.setErrors(errors);
            report.setWarnings(warnings);
            report.setValid(errors.isEmpty());

        } catch (Exception e) {
            errors.add("Erreur lors de la lecture du fichier Excel : " + e.getMessage());
            report.setValid(false);
            report.setErrors(errors);
        }

        return report;
    }

    private Map<String, Integer> mapColumns(Row headerRow) {
        Map<String, Integer> map = new HashMap<>();
        for (Cell cell : headerRow) {
            if (cell == null) continue;
            String headerText = cell.getStringCellValue().trim().toLowerCase();

            if (headerText.contains("n°") || headerText.contains("num") || headerText.equals("no") || headerText.equals("id")) {
                map.put("num", cell.getColumnIndex());
            } else if (headerText.contains("nom") || headerText.contains("prenom") || headerText.contains("prénom") || headerText.contains("etudiant") || headerText.contains("étudiant") || headerText.contains("student")) {
                map.put("nom", cell.getColumnIndex());
            } else if (headerText.contains("matricule") || headerText.contains("cin") || headerText.contains("identifiant") || headerText.contains("code")) {
                map.put("matricule", cell.getColumnIndex());
            } else if (headerText.contains("classe") || headerText.contains("groupe") || headerText.contains("promotion") || headerText.contains("section") || headerText.contains("filiere") || headerText.contains("filière") || headerText.contains("specialite") || headerText.contains("spécialité") || headerText.contains("parcours")) {
                map.put("classe", cell.getColumnIndex());
            } else if (headerText.contains("moyenne g") || headerText.contains("moyenne generale") || headerText.contains("moyenne générale") || headerText.equals("moyenne") || headerText.equals("mg") || headerText.contains("moy_gen") || headerText.contains("moy")) {
                map.put("moyenne", cell.getColumnIndex());
            } else if (headerText.contains("ects") || headerText.contains("credit") || headerText.contains("crédit")) {
                map.put("ects", cell.getColumnIndex());
            } else if (headerText.contains("statut") || headerText.contains("etat") || headerText.contains("état")) {
                map.put("statut", cell.getColumnIndex());
            } else if (headerText.contains("ue") || headerText.contains("moyenne ue") || headerText.contains("moy_ue")) {
                map.put("moyue", cell.getColumnIndex());
            } else if (headerText.contains("décision") || headerText.contains("decision") || headerText.contains("resultat") || headerText.contains("résultat")) {
                map.put("decision", cell.getColumnIndex());
            } else if (headerText.contains("mention") || headerText.contains("distinction")) {
                map.put("mention", cell.getColumnIndex());
            } else if (headerText.contains("observation") || headerText.contains("remarque") || headerText.contains("note")) {
                map.put("observation", cell.getColumnIndex());
            }
        }
        return map;
    }

    private List<EtudiantDTO> parseSheetRows(Sheet sheet, Map<String, Integer> colMap, String sheetName, List<String> errors, List<String> warnings) {
        List<EtudiantDTO> list = new ArrayList<>();
        int lastRowIndex = sheet.getLastRowNum();

        for (int r = 1; r <= lastRowIndex; r++) {
            Row row = sheet.getRow(r);
            if (row == null || isRowEmpty(row)) continue;

            EtudiantDTO.EtudiantDTOBuilder builder = EtudiantDTO.builder();
            List<String> rowAnomalies = new ArrayList<>();

            // Nom & Prenom
            String nom = getCellStringValue(row, colMap.get("nom"));
            if (nom.isBlank()) {
                warnings.add("Ligne " + (r + 1) + " dans '" + sheetName + "' : Le nom de l'étudiant est vide.");
                rowAnomalies.add("Nom manquant");
            }
            builder.nomPrenom(nom);

            // Matricule / CIN
            String matricule = getCellStringValue(row, colMap.get("matricule"));
            builder.matriculeCin(matricule);

            // Classe
            String classe = getCellStringValue(row, colMap.get("classe"));
            builder.classe(classe.isBlank() ? sheetName : classe);

            // Moyenne Générale
            Double moyenne = getCellDoubleValue(row, colMap.get("moyenne"));
            if (moyenne == null) {
                warnings.add("Ligne " + (r + 1) + " dans '" + sheetName + "' : Moyenne générale non numérique ou manquante.");
                rowAnomalies.add("Moyenne non numérique");
                moyenne = 0.0;
            } else if (moyenne < 0.0 || moyenne > 20.0) {
                warnings.add("Ligne " + (r + 1) + " dans '" + sheetName + "' : Moyenne hors plage (0-20) : " + moyenne);
                rowAnomalies.add("Moyenne hors norme (" + moyenne + ")");
            }
            builder.moyenneGenerale(moyenne);

            // ECTS non validés
            Double ectsVal = getCellDoubleValue(row, colMap.get("ects"));
            builder.ectsNonValides(ectsVal != null ? ectsVal.intValue() : 0);

            // Statut Étudiant ("ancien" / "nouveau")
            String statut = getCellStringValue(row, colMap.get("statut"));
            builder.statutEtudiant(statut.isBlank() ? "nouveau" : statut.trim().toLowerCase());

            // Moyenne UE
            Double moyUe = getCellDoubleValue(row, colMap.get("moyue"));
            builder.moyenneUe(moyUe);

            // Décision, Mention, Observation
            builder.decision(getCellStringValue(row, colMap.get("decision")));
            builder.mention(getCellStringValue(row, colMap.get("mention")));
            builder.observation(getCellStringValue(row, colMap.get("observation")));
            builder.anomalies(rowAnomalies);

            list.add(builder.build());
        }
        return list;
    }

    private String getCellStringValue(Row row, Integer colIdx) {
        if (colIdx == null) return "";
        Cell cell = row.getCell(colIdx);
        if (cell == null) return "";

        switch (cell.getCellType()) {
            case STRING:
                return cell.getStringCellValue().trim();
            case NUMERIC:
                DecimalFormat df = new DecimalFormat("#.##");
                return df.format(cell.getNumericCellValue());
            case BOOLEAN:
                return String.valueOf(cell.getBooleanCellValue());
            case FORMULA:
                try {
                    return cell.getStringCellValue().trim();
                } catch (Exception e) {
                    return String.valueOf(cell.getNumericCellValue());
                }
            default:
                return "";
        }
    }

    private Double getCellDoubleValue(Row row, Integer colIdx) {
        if (colIdx == null) return null;
        Cell cell = row.getCell(colIdx);
        if (cell == null) return null;

        try {
            if (cell.getCellType() == CellType.NUMERIC) {
                return cell.getNumericCellValue();
            } else if (cell.getCellType() == CellType.STRING) {
                String val = cell.getStringCellValue().trim().replace(",", ".");
                if (val.contains("/")) val = val.split("/")[0].trim();
                return Double.parseDouble(val);
            } else if (cell.getCellType() == CellType.FORMULA) {
                return cell.getNumericCellValue();
            }
        } catch (Exception ignored) {}
        return null;
    }

    private boolean isRowEmpty(Row row) {
        for (int c = row.getFirstCellNum(); c < row.getLastCellNum(); c++) {
            Cell cell = row.getCell(c);
            if (cell != null && cell.getCellType() != CellType.BLANK) {
                return false;
            }
        }
        return true;
    }
}
