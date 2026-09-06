package com.amna.project.services;

import com.amna.project.dto.*;
import org.apache.poi.xwpf.model.XWPFHeaderFooterPolicy;
import org.apache.poi.xwpf.usermodel.*;
import org.openxmlformats.schemas.wordprocessingml.x2006.main.*;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.stereotype.Service;

import java.io.ByteArrayOutputStream;
import java.math.BigInteger;
import java.time.LocalDateTime;
import java.time.format.DateTimeFormatter;
import java.util.List;

@Service
public class WordGeneratorService {

    @Autowired(required = false)
    private MachineLearningService machineLearningService;

    public byte[] generatePvWordDocument(ClassePreviewDTO classeData, SessionDeliberationDTO sessionInfo) throws Exception {
        try (XWPFDocument document = new XWPFDocument(); ByteArrayOutputStream out = new ByteArrayOutputStream()) {

            // Set Page Margins
            CTSectPr sectPr = document.getDocument().getBody().addNewSectPr();
            CTPageMar pageMar = sectPr.addNewPgMar();
            pageMar.setLeft(BigInteger.valueOf(1200));
            pageMar.setRight(BigInteger.valueOf(1200));
            pageMar.setTop(BigInteger.valueOf(1000));
            pageMar.setBottom(BigInteger.valueOf(1000));

            // 1. INSTITUTIONAL HEADER
            XWPFTable headerTable = document.createTable(1, 2);
            headerTable.setWidth("100%");
            removeTableBorders(headerTable);

            XWPFTableCell cellLeft = headerTable.getRow(0).getCell(0);
            XWPFParagraph pLeft = cellLeft.getParagraphs().get(0);
            pLeft.setAlignment(ParagraphAlignment.LEFT);
            XWPFRun rEtab = pLeft.createRun();
            rEtab.setBold(true);
            rEtab.setFontSize(11);
            rEtab.setFontFamily("Calibri");
            rEtab.setColor("1E3A8A");
            rEtab.setText(sessionInfo != null && sessionInfo.getEtablissement() != null ? sessionInfo.getEtablissement() : "REPUBLIQUE TUNISIENNE - MINISTERE DE L'ENSEIGNEMENT SUPERIEUR");
            rEtab.addBreak();

            XWPFRun rDept = pLeft.createRun();
            rDept.setFontSize(10);
            rDept.setFontFamily("Calibri");
            rDept.setColor("475569");
            rDept.setText(sessionInfo != null && sessionInfo.getFaculteDepartement() != null ? sessionInfo.getFaculteDepartement() : "Faculté des Sciences & Technologies");

            XWPFTableCell cellRight = headerTable.getRow(0).getCell(1);
            XWPFParagraph pRight = cellRight.getParagraphs().get(0);
            pRight.setAlignment(ParagraphAlignment.RIGHT);
            XWPFRun rDate = pRight.createRun();
            rDate.setFontSize(9);
            rDate.setFontFamily("Calibri");
            rDate.setColor("64748B");
            rDate.setText("Session : " + (sessionInfo != null && sessionInfo.getTypeSession() != null ? sessionInfo.getTypeSession() : "Principale"));
            rDate.addBreak();
            rDate.setText("Année Universitaire : " + (sessionInfo != null && sessionInfo.getAnneeUniversitaire() != null ? sessionInfo.getAnneeUniversitaire() : "2025-2026"));

            addEmptyParagraph(document, 1);

            // 2. MAIN DOCUMENT TITLE
            XWPFParagraph pTitle = document.createParagraph();
            pTitle.setAlignment(ParagraphAlignment.CENTER);
            pTitle.setSpacingBefore(200);
            pTitle.setSpacingAfter(200);
            XWPFRun rTitle = pTitle.createRun();
            rTitle.setText("PROCÈS-VERBAL DE DÉLIBÉRATION");
            rTitle.setBold(true);
            rTitle.setFontSize(18);
            rTitle.setFontFamily("Calibri");
            rTitle.setColor("1E3A8A");

            // 3. SESSION & CLASS INFO BOX
            XWPFTable infoTable = document.createTable(2, 2);
            infoTable.setWidth("100%");
            styleInfoTable(infoTable);

            String classeName = classeData != null ? classeData.getNomClasse() : "N/A";
            setCellText(infoTable.getRow(0).getCell(0), "Classe / Groupe : ", classeName, true);
            setCellText(infoTable.getRow(0).getCell(1), "Date de Délibération : ", (sessionInfo != null && sessionInfo.getDateSession() != null ? sessionInfo.getDateSession() : "Juillet 2026"), false);
            setCellText(infoTable.getRow(1).getCell(0), "Lieu : ", (sessionInfo != null && sessionInfo.getLieuDeliberation() != null ? sessionInfo.getLieuDeliberation() : "Salle des Conseils"), false);
            setCellText(infoTable.getRow(1).getCell(1), "Président du Jury : ", (sessionInfo != null && sessionInfo.getPresidentJury() != null ? sessionInfo.getPresidentJury() : "Prof. Président du Jury"), true);

            addEmptyParagraph(document, 1);

            // 4. JURY COMPOSITION TABLE
            if (sessionInfo != null && sessionInfo.getMembresJury() != null && !sessionInfo.getMembresJury().isEmpty()) {
                XWPFParagraph pJuryHead = document.createParagraph();
                XWPFRun rJuryHead = pJuryHead.createRun();
                rJuryHead.setText("Composition du Jury de Délibération :");
                rJuryHead.setBold(true);
                rJuryHead.setFontSize(11);
                rJuryHead.setFontFamily("Calibri");
                rJuryHead.setColor("1E3A8A");

                XWPFTable juryTable = document.createTable(1, 2);
                juryTable.setWidth("100%");
                setRowHeaderStyle(juryTable.getRow(0), "Membre du Jury (Nom & Prénom)", "Fonction / Rôle");

                for (MembreJuryDTO m : sessionInfo.getMembresJury()) {
                    XWPFTableRow row = juryTable.createRow();
                    setCellContent(row.getCell(0), m.getNom(), false, "000000");
                    setCellContent(row.getCell(1), m.getFonction(), false, "475569");
                }
                addEmptyParagraph(document, 1);
            }

            // 5. RESULTS TABLE
            XWPFParagraph pResHead = document.createParagraph();
            XWPFRun rResHead = pResHead.createRun();
            rResHead.setText("Tableau Récapitulatif des Résultats des Étudiants :");
            rResHead.setBold(true);
            rResHead.setFontSize(12);
            rResHead.setFontFamily("Calibri");
            rResHead.setColor("1E3A8A");

            XWPFTable resultsTable = document.createTable(1, 6);
            resultsTable.setWidth("100%");

            // Header Row
            XWPFTableRow headerRow = resultsTable.getRow(0);
            setRowHeaderStyle(headerRow, "N°", "Nom et Prénom", "Matricule / CIN", "Moyenne", "Décision du Jury", "Mention / Obs.");

            // Data Rows
            if (classeData != null && classeData.getEtudiants() != null) {
                int rowIdx = 0;
                for (EtudiantDTO e : classeData.getEtudiants()) {
                    XWPFTableRow row = resultsTable.createRow();
                    String bgColor = (rowIdx % 2 == 1) ? "F8FAFC" : "FFFFFF";

                    setCellContentWithBg(row.getCell(0), String.valueOf(e.getNumOrder() != null ? e.getNumOrder() : (rowIdx + 1)), false, "000000", bgColor);
                    setCellContentWithBg(row.getCell(1), e.getNomPrenom() != null ? e.getNomPrenom() : "", true, "0F172A", bgColor);
                    setCellContentWithBg(row.getCell(2), e.getMatriculeCin() != null ? e.getMatriculeCin() : "", false, "475569", bgColor);

                    String moyStr = e.getMoyenneGenerale() != null ? String.format("%.2f", e.getMoyenneGenerale()) : "0.00";
                    setCellContentWithBg(row.getCell(3), moyStr, true, "1E3A8A", bgColor);

                    // Decision styling badge
                    String decText = e.getDecision() != null ? e.getDecision() : "Ajourné";
                    String decBg = getDecisionBgColor(decText);
                    String decTextColor = getDecisionTextColor(decText);
                    setCellContentWithBg(row.getCell(4), decText, true, decTextColor, decBg);

                    String mentionText = e.getMention() != null ? e.getMention() : "";
                    if (e.getObservation() != null && !e.getObservation().isBlank()) {
                        mentionText += (mentionText.isBlank() ? "" : " - ") + e.getObservation();
                    }
                    setCellContentWithBg(row.getCell(5), mentionText, false, "334155", bgColor);

                    rowIdx++;
                }
            }

            addEmptyParagraph(document, 1);

            // 6. SYNTHESIS STATISTICS BOX
            XWPFParagraph pStatHead = document.createParagraph();
            XWPFRun rStatHead = pStatHead.createRun();
            rStatHead.setText("Statistiques de Synthèse de la Session :");
            rStatHead.setBold(true);
            rStatHead.setFontSize(11);
            rStatHead.setFontFamily("Calibri");
            rStatHead.setColor("1E3A8A");

            XWPFTable statTable = document.createTable(2, 5);
            statTable.setWidth("100%");

            XWPFTableRow sHeader = statTable.getRow(0);
            setRowHeaderStyle(sHeader, "Effectif Total", "Admis", "Rachats / ECTS", "Conseil École", "Taux de Réussite");

            XWPFTableRow sData = statTable.getRow(1);
            int tot = classeData != null ? classeData.getTotalEtudiants() : 0;
            int adm = classeData != null ? classeData.getNbAdmis() : 0;
            int rach = classeData != null ? (classeData.getNbRachats() + classeData.getNbAdmisEcts()) : 0;
            int cons = classeData != null ? classeData.getNbConseilEcole() : 0;
            double tx = classeData != null ? classeData.getTauxReussite() : 0.0;

            setCellContentWithBg(sData.getCell(0), String.valueOf(tot), true, "0F172A", "F1F5F9");
            setCellContentWithBg(sData.getCell(1), String.valueOf(adm), true, "065F46", "D1FAE5");
            setCellContentWithBg(sData.getCell(2), String.valueOf(rach), true, "1E40AF", "DBEAFE");
            setCellContentWithBg(sData.getCell(3), String.valueOf(cons), true, "92400E", "FEF3C7");
            setCellContentWithBg(sData.getCell(4), String.format("%.1f %%", tx), true, "1E3A8A", "E0E7FF");

            // 6.5 MACHINE LEARNING AI DECISION SUPPORT SECTION
            if (machineLearningService != null && classeData != null && classeData.getEtudiants() != null && !classeData.getEtudiants().isEmpty()) {
                addEmptyParagraph(document, 1);
                XWPFParagraph pMlHead = document.createParagraph();
                XWPFRun rMlHead = pMlHead.createRun();
                rMlHead.setText("ANNEXE : AIDE À LA DÉCISION PAR MACHINE LEARNING (CONSENSUS 3 MODÈLES)");
                rMlHead.setBold(true);
                rMlHead.setFontSize(11);
                rMlHead.setFontFamily("Calibri");
                rMlHead.setColor("1E3A8A");

                XWPFParagraph pMlSub = document.createParagraph();
                XWPFRun rMlSub = pMlSub.createRun();
                rMlSub.setText("Analyse prédictive multi-modèles (Arbre de Décision, KNN, Random Forest) générée pour assister le Jury :");
                rMlSub.setItalic(true);
                rMlSub.setFontSize(9);
                rMlSub.setFontFamily("Calibri");
                rMlSub.setColor("475569");

                List<EtudiantDTO> etus = classeData.getEtudiants();
                XWPFTable mlTable = document.createTable(etus.size() + 1, 6);
                mlTable.setWidth("100%");

                setRowHeaderStyle(mlTable.getRow(0), "N°", "Étudiant (CIN)", "Moyenne", "Random Forest", "Consensus IA", "Confiance");

                for (int i = 0; i < etus.size(); i++) {
                    EtudiantDTO e = etus.get(i);
                    MlStudentFeaturesDTO f = MlStudentFeaturesDTO.builder()
                            .matriculeCin(e.getMatriculeCin())
                            .nomPrenom(e.getNomPrenom())
                            .moyenneGenerale(e.getMoyenneGenerale() != null ? e.getMoyenneGenerale() : 0.0)
                            .ectsNonValides(e.getEctsNonValides() != null ? e.getEctsNonValides() : 0)
                            .statutEtudiant(e.getStatutEtudiant() != null ? e.getStatutEtudiant() : "nouveau")
                            .moyenneUe(e.getMoyenneUe() != null ? e.getMoyenneUe() : 10.0)
                            .noteExam(e.getMoyenneGenerale())
                            .noteCc(e.getMoyenneGenerale())
                            .noteTp(e.getMoyenneGenerale())
                            .build();

                    MlModelComparisonDTO comp = machineLearningService.compareAllModels(f);
                    XWPFTableRow row = mlTable.getRow(i + 1);

                    setCellContent(row.getCell(0), String.valueOf(i + 1), false, "1E293B");
                    setCellContent(row.getCell(1), e.getNomPrenom() + " (" + (e.getMatriculeCin() != null ? e.getMatriculeCin() : "-") + ")", true, "1E293B");
                    setCellContent(row.getCell(2), String.format("%.2f", e.getMoyenneGenerale() != null ? e.getMoyenneGenerale() : 0.0), true, "1E3A8A");
                    setCellContent(row.getCell(3), comp.getRandomForestResult() != null ? comp.getRandomForestResult().getDecisionPredite() : "-", false, "2563EB");
                    setCellContent(row.getCell(4), comp.getConsensusDecision() != null ? comp.getConsensusDecision() : "-", true, "0F172A");
                    
                    double conf = comp.getRandomForestResult() != null && comp.getRandomForestResult().getConfidenceScore() != null ? comp.getRandomForestResult().getConfidenceScore() : 95.0;
                    setCellContent(row.getCell(5), String.format("%.1f %% (Consensus)", conf), false, "065F46");
                }
            }

            addEmptyParagraph(document, 2);

            // 7. SIGNATURE BLOCK
            XWPFTable sigTable = document.createTable(1, 2);
            sigTable.setWidth("100%");
            removeTableBorders(sigTable);

            XWPFTableCell sigLeft = sigTable.getRow(0).getCell(0);
            XWPFParagraph pSigLeft = sigLeft.getParagraphs().get(0);
            pSigLeft.setAlignment(ParagraphAlignment.LEFT);
            XWPFRun rSigL = pSigLeft.createRun();
            rSigL.setBold(true);
            rSigL.setFontSize(10);
            rSigL.setText("Membres du Jury de Délibération :");
            rSigL.addBreak();
            rSigL.addBreak();
            rSigL.setText("Signature : _______________________");

            XWPFTableCell sigRight = sigTable.getRow(0).getCell(1);
            XWPFParagraph pSigRight = sigRight.getParagraphs().get(0);
            pSigRight.setAlignment(ParagraphAlignment.RIGHT);
            XWPFRun rSigR = pSigRight.createRun();
            rSigR.setBold(true);
            rSigR.setFontSize(10);
            rSigR.setText("Le Président du Jury :");
            rSigR.addBreak();
            rSigR.addBreak();
            rSigR.setText((sessionInfo != null && sessionInfo.getPresidentJury() != null ? sessionInfo.getPresidentJury() : "Signature & Cachet"));

            // 8. FOOTER WITH TIMESTAMP
            XWPFHeaderFooterPolicy policy = new XWPFHeaderFooterPolicy(document, sectPr);
            XWPFFooter footer = policy.createFooter(XWPFHeaderFooterPolicy.DEFAULT);
            XWPFParagraph pFooter = footer.createParagraph();
            pFooter.setAlignment(ParagraphAlignment.RIGHT);
            XWPFRun rFoot = pFooter.createRun();
            rFoot.setFontSize(8);
            rFoot.setColor("94A3B8");
            String timestamp = LocalDateTime.now().format(DateTimeFormatter.ofPattern("dd/MM/yyyy HH:mm"));
            rFoot.setText("Document généré automatiquement le " + timestamp + " - Page 1");

            document.write(out);
            return out.toByteArray();
        }
    }

    private void styleInfoTable(XWPFTable table) {
        table.setCellMargins(100, 150, 100, 150);
        for (XWPFTableRow row : table.getRows()) {
            for (XWPFTableCell cell : row.getTableCells()) {
                cell.setColor("F8FAFC");
            }
        }
    }

    private void setRowHeaderStyle(XWPFTableRow row, String... titles) {
        for (int i = 0; i < titles.length && i < row.getTableCells().size(); i++) {
            XWPFTableCell cell = row.getCell(i);
            cell.setColor("1E3A8A"); // Institutional Blue
            XWPFParagraph p = cell.getParagraphs().get(0);
            p.setAlignment(ParagraphAlignment.CENTER);
            XWPFRun r = p.createRun();
            r.setText(titles[i]);
            r.setBold(true);
            r.setFontSize(9);
            r.setFontFamily("Calibri");
            r.setColor("FFFFFF");
        }
    }

    private void setCellContent(XWPFTableCell cell, String text, boolean bold, String textColorHex) {
        setCellContentWithBg(cell, text, bold, textColorHex, "FFFFFF");
    }

    private void setCellContentWithBg(XWPFTableCell cell, String text, boolean bold, String textColorHex, String bgHex) {
        if (cell == null) return;
        cell.setColor(bgHex);
        XWPFParagraph p = cell.getParagraphs().isEmpty() ? cell.addParagraph() : cell.getParagraphs().get(0);
        p.setSpacingBefore(40);
        p.setSpacingAfter(40);
        p.setAlignment(ParagraphAlignment.LEFT);
        XWPFRun r = p.getRuns().isEmpty() ? p.createRun() : p.getRuns().get(0);
        r.setText(text != null ? text : "");
        r.setBold(bold);
        r.setFontSize(9);
        r.setFontFamily("Calibri");
        r.setColor(textColorHex);
    }

    private void setCellText(XWPFTableCell cell, String label, String value, boolean boldValue) {
        if (cell == null) return;
        XWPFParagraph p = cell.getParagraphs().get(0);
        XWPFRun rLabel = p.createRun();
        rLabel.setText(label);
        rLabel.setBold(true);
        rLabel.setFontSize(9);
        rLabel.setColor("475569");

        XWPFRun rVal = p.createRun();
        rVal.setText(value);
        rVal.setBold(boldValue);
        rVal.setFontSize(9);
        rVal.setColor("0F172A");
    }

    private String getDecisionBgColor(String decision) {
        if (decision == null) return "F1F5F9";
        String d = decision.toUpperCase();
        if (d.contains("CONSEIL")) return "FEF3C7";
        if (d.contains("RACHAT")) return "DBEAFE";
        if (d.contains("ECTS")) return "CFFAFE";
        if (d.contains("ADMIS")) return "D1FAE5";
        return "FEE2E2";
    }

    private String getDecisionTextColor(String decision) {
        if (decision == null) return "475569";
        String d = decision.toUpperCase();
        if (d.contains("CONSEIL")) return "92400E";
        if (d.contains("RACHAT")) return "1E40AF";
        if (d.contains("ECTS")) return "155E75";
        if (d.contains("ADMIS")) return "065F46";
        return "991B1B";
    }

    private void removeTableBorders(XWPFTable table) {
        CTTblPr tblPr = table.getCTTbl().getTblPr();
        if (tblPr == null) tblPr = table.getCTTbl().addNewTblPr();
        CTTblBorders borders = tblPr.addNewTblBorders();
        borders.addNewBottom().setVal(STBorder.NONE);
        borders.addNewTop().setVal(STBorder.NONE);
        borders.addNewLeft().setVal(STBorder.NONE);
        borders.addNewRight().setVal(STBorder.NONE);
        borders.addNewInsideH().setVal(STBorder.NONE);
        borders.addNewInsideV().setVal(STBorder.NONE);
    }

    private void addEmptyParagraph(XWPFDocument doc, int count) {
        for (int i = 0; i < count; i++) {
            XWPFParagraph p = doc.createParagraph();
            p.setSpacingAfter(60);
        }
    }
}
