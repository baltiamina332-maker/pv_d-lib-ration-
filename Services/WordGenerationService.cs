using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using DesktopApp.Models;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace DesktopApp.Services
{
    /// <summary>
    /// Informations du jury pour le PV
    /// </summary>
    public class InfosJury
    {
        public string NomEtablissement { get; set; } = "";
        public string TypeSession { get; set; } = "Principale";
        public string AnneeUniversitaire { get; set; } = "";
        public string Filiere { get; set; } = "";
        public string PresidentJury { get; set; } = "";
        public string MembreJury1 { get; set; } = "";
        public string MembreJury2 { get; set; } = "";
        public string Secretaire { get; set; } = "";
        public DateTime DateDeliberation { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// Service pour générer des documents Word (PV de Délibération) conformes au modèle institutionnel officiel
    /// </summary>
    public class WordGenerationService
    {
        private const string PrimaryNavy = "1B365D";   // Bleu Marine institutionnel principal
        private const string SubHeaderBg = "D9E1F2";   // Bleu-Gris clair pour les sous-en-têtes du jury
        private const string BorderColor = "1B365D";   // Bordure de tableau

        /// <summary>
        /// Générer un PV de délibération au format Word (.docx)
        /// </summary>
        public bool GenererPV(List<Etudiant> etudiants, string cheminSortie, string nomFichier, string classeGroupe, InfosJury jury = null)
        {
            try
            {
                if (etudiants == null || etudiants.Count == 0) return false;

                if (jury == null)
                {
                    jury = new InfosJury
                    {
                        Filiere = classeGroupe,
                        AnneeUniversitaire = !string.IsNullOrWhiteSpace(etudiants[0].AnneeUniversitaire) 
                                            ? etudiants[0].AnneeUniversitaire 
                                            : $"{DateTime.Now.Year - 1}-{DateTime.Now.Year}",
                        TypeSession = "Principale"
                    };
                }

                // Définir des valeurs par défaut élégantes pour le jury si non renseigné
                if (string.IsNullOrWhiteSpace(jury.PresidentJury)) jury.PresidentJury = "Pr. Salah Kamoun";
                if (string.IsNullOrWhiteSpace(jury.MembreJury1)) jury.MembreJury1 = "Dr. Wided Sassi";
                if (string.IsNullOrWhiteSpace(jury.MembreJury2)) jury.MembreJury2 = "Dr. Amine Ferchichi";

                string cheminComplet = Path.Combine(cheminSortie, nomFichier);
                if (!Directory.Exists(cheminSortie)) Directory.CreateDirectory(cheminSortie);

                using (var fileStream = new FileStream(cheminComplet, FileMode.Create, FileAccess.ReadWrite))
                using (WordprocessingDocument wordDocument = WordprocessingDocument.Create(fileStream, WordprocessingDocumentType.Document))
                {
                    var mainPart = wordDocument.AddMainDocumentPart();
                    mainPart.Document = new Document();
                    var body = new Body();
                    mainPart.Document.AppendChild(body);

                    // Configuration page (A4 Portrait)
                    var sectPr = new SectionProperties();
                    var pgSz = new PageSize { Width = 11906, Height = 16838, Orient = PageOrientationValues.Portrait };
                    sectPr.AppendChild(pgSz);
                    var pgMar = new PageMargin { Top = 720, Bottom = 720, Left = 720, Right = 720 };
                    sectPr.AppendChild(pgMar);
                    body.AppendChild(sectPr);

                    // 1. En-tête (Logo Établissement + Classe & Ligne de séparation)
                    AjouterEnteteLogoClasse(body, jury.Filiere);

                    // 2. Titre principal & Établissement
                    AjouterTitreEtEtablissement(body);

                    // 3. Métadonnées (Classe, Session, Année, Date)
                    AjouterMetadonnees(body, jury);

                    // 4. Section 1 : Composition du Jury
                    AjouterCompositionJuryTable(body, jury);

                    // 5. Section 2 : Résultats de la Délibération
                    AjouterResultatsTable(body, etudiants);

                    // 6. Section 3 : Statistiques de la Session
                    AjouterStatistiquesTable(body, etudiants);

                    // 7. Section 4 : Signatures
                    AjouterZoneSignatures(body);
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WORD] Erreur génération PV: {ex.Message}");
                return false;
            }
        }

        // Surcharge pour compatibilité
        public bool GenererPV(List<Etudiant> etudiants, string cheminSortie, string nomFichier, string classeGroupe)
        {
            return GenererPV(etudiants, cheminSortie, nomFichier, classeGroupe, null);
        }

        // ── 1. En-tête Logo + Classe ─────────────────────────────────────────

        private void AjouterEnteteLogoClasse(Body body, string classe)
        {
            var table = new Table();
            var tblPr = new TableProperties();
            tblPr.AppendChild(new TableWidth { Type = TableWidthUnitValues.Pct, Width = "5000" });
            tblPr.AppendChild(new TableBorders
            {
                TopBorder = new TopBorder { Val = BorderValues.None },
                BottomBorder = new BottomBorder { Val = BorderValues.None },
                LeftBorder = new LeftBorder { Val = BorderValues.None },
                RightBorder = new RightBorder { Val = BorderValues.None },
                InsideHorizontalBorder = new InsideHorizontalBorder { Val = BorderValues.None },
                InsideVerticalBorder = new InsideVerticalBorder { Val = BorderValues.None }
            });
            table.AppendChild(tblPr);

            var row = new TableRow();

            // Gauche : [LOGO ÉTABLISSEMENT]
            var cellLeft = new TableCell();
            cellLeft.AppendChild(CreerParagraphe("[LOGO ÉTABLISSEMENT]", "20", true, JustificationValues.Left, "555555"));
            cellLeft.AppendChild(new TableCellProperties(new TableCellWidth { Type = TableWidthUnitValues.Pct, Width = "2500" }));
            row.AppendChild(cellLeft);

            // Droite : Classe : 4 SAE
            var cellRight = new TableCell();
            cellRight.AppendChild(CreerParagraphe($"Classe : {classe}", "20", false, JustificationValues.Right, "000000"));
            cellRight.AppendChild(new TableCellProperties(new TableCellWidth { Type = TableWidthUnitValues.Pct, Width = "2500" }));
            row.AppendChild(cellRight);

            table.AppendChild(row);
            body.AppendChild(table);

            // Ligne de séparation bleue horizontale sous l'en-tête
            var paraSep = new Paragraph();
            paraSep.ParagraphProperties = new ParagraphProperties
            {
                ParagraphBorders = new ParagraphBorders
                {
                    BottomBorder = new BottomBorder { Val = BorderValues.Single, Size = 12, Color = PrimaryNavy }
                },
                SpacingBetweenLines = new SpacingBetweenLines { After = "240" }
            };
            body.AppendChild(paraSep);
        }

        // ── 2. Titre & Établissement ──────────────────────────────────────────

        private void AjouterTitreEtEtablissement(Body body)
        {
            // Nom de l'Établissement (Bleu institutionnel)
            body.AppendChild(CreerParagraphe("", "24", true, JustificationValues.Center, PrimaryNavy, "120"));

            // Grand Titre PROCÈS-VERBAL DE DÉLIBÉRATION
            body.AppendChild(CreerParagraphe("PROCÈS-VERBAL DE DÉLIBÉRATION", "36", true, JustificationValues.Center, PrimaryNavy, "280"));
        }

        // ── 3. Métadonnées ───────────────────────────────────────────────────

        private void AjouterMetadonnees(Body body, InfosJury jury)
        {
            string dateStr = jury.DateDeliberation.ToString("dd MMMM yyyy", new CultureInfo("fr-FR"));
            if (string.IsNullOrWhiteSpace(dateStr)) dateStr = DateTime.Now.ToString("dd MMMM yyyy", new CultureInfo("fr-FR"));

            string anneeStr = !string.IsNullOrWhiteSpace(jury.AnneeUniversitaire) ? jury.AnneeUniversitaire : "2025-2026";
            string sessionStr = !string.IsNullOrWhiteSpace(jury.TypeSession) ? jury.TypeSession : "Principale";

            // Ligne 1 : Classe / Groupe & Session
            var p1 = new Paragraph();
            p1.ParagraphProperties = new ParagraphProperties { SpacingBetweenLines = new SpacingBetweenLines { After = "80" } };
            p1.AppendChild(CreerRun("Classe / Groupe : ", "22", true, "000000"));
            p1.AppendChild(CreerRun($"{jury.Filiere}    ", "22", false, "000000"));
            p1.AppendChild(CreerRun("Session : ", "22", true, "000000"));
            p1.AppendChild(CreerRun(sessionStr, "22", false, "000000"));
            body.AppendChild(p1);

            // Ligne 2 : Année Universitaire & Date de délibération
            var p2 = new Paragraph();
            p2.ParagraphProperties = new ParagraphProperties { SpacingBetweenLines = new SpacingBetweenLines { After = "240" } };
            p2.AppendChild(CreerRun("Année Universitaire : ", "22", true, "000000"));
            p2.AppendChild(CreerRun($"{anneeStr}    ", "22", false, "000000"));
            p2.AppendChild(CreerRun("Date de délibération : ", "22", true, "000000"));
            p2.AppendChild(CreerRun(dateStr, "22", false, "000000"));
            body.AppendChild(p2);
        }

        // ── 4. Composition du Jury ───────────────────────────────────────────

        private void AjouterCompositionJuryTable(Body body, InfosJury jury)
        {
            body.AppendChild(CreerParagraphe("Composition du Jury", "24", true, JustificationValues.Left, PrimaryNavy, "120"));

            var table = new Table();
            table.AppendChild(CreerProprietesTableau(true));

            // Ligne 1 : En-têtes (Rôle | Président du Jury | Membre 1 | Membre 2)
            var rowHeader = new TableRow();
            rowHeader.AppendChild(CellulePV("Rôle", true, "2000", PrimaryNavy, false, "FFFFFF", JustificationValues.Center));
            rowHeader.AppendChild(CellulePV("Président du Jury", true, "2800", SubHeaderBg, false, PrimaryNavy, JustificationValues.Center));
            rowHeader.AppendChild(CellulePV("Membre 1", true, "2800", SubHeaderBg, false, PrimaryNavy, JustificationValues.Center));
            rowHeader.AppendChild(CellulePV("Membre 2", true, "2800", SubHeaderBg, false, PrimaryNavy, JustificationValues.Center));
            table.AppendChild(rowHeader);

            // Ligne 2 : Nom
            var rowNom = new TableRow();
            rowNom.AppendChild(CellulePV("Nom", true, "2000", PrimaryNavy, false, "FFFFFF", JustificationValues.Center));
            rowNom.AppendChild(CellulePV(jury.PresidentJury, false, "2800", "FFFFFF", false, "000000", JustificationValues.Center));
            rowNom.AppendChild(CellulePV(jury.MembreJury1, false, "2800", "FFFFFF", false, "000000", JustificationValues.Center));
            rowNom.AppendChild(CellulePV(jury.MembreJury2, false, "2800", "FFFFFF", false, "000000", JustificationValues.Center));
            table.AppendChild(rowNom);

            // Ligne 3 : Signature (vide pour emargement)
            var rowSign = new TableRow();
            rowSign.AppendChild(CellulePV("Signature", true, "2000", PrimaryNavy, false, "FFFFFF", JustificationValues.Center));
            rowSign.AppendChild(CellulePV("", false, "2800", "FFFFFF", false, "000000", JustificationValues.Center, "500"));
            rowSign.AppendChild(CellulePV("", false, "2800", "FFFFFF", false, "000000", JustificationValues.Center, "500"));
            rowSign.AppendChild(CellulePV("", false, "2800", "FFFFFF", false, "000000", JustificationValues.Center, "500"));
            table.AppendChild(rowSign);

            body.AppendChild(table);
            body.AppendChild(CreerParagraphe("", "10", false, JustificationValues.Left, "000000", "200"));
        }

        // ── 5. Résultats de la Délibération ──────────────────────────────────

        private void AjouterResultatsTable(Body body, List<Etudiant> etudiants)
        {
            body.AppendChild(CreerParagraphe("Résultats de la Délibération", "24", true, JustificationValues.Left, PrimaryNavy, "120"));

            var table = new Table();
            table.AppendChild(CreerProprietesTableau(true));

            // En-tête conforme : N° | Nom et Prénom | Matricule/CIN | Moyenne Générale | Décision | Mention
            var headerRow = new TableRow();
            headerRow.AppendChild(CellulePV("N°", true, "800", PrimaryNavy, false, "FFFFFF", JustificationValues.Center));
            headerRow.AppendChild(CellulePV("Nom et Prénom", true, "3200", PrimaryNavy, false, "FFFFFF", JustificationValues.Center));
            headerRow.AppendChild(CellulePV("Matricule/CIN", true, "2000", PrimaryNavy, false, "FFFFFF", JustificationValues.Center));
            headerRow.AppendChild(CellulePV("Moyenne\nGénérale", true, "1800", PrimaryNavy, false, "FFFFFF", JustificationValues.Center));
            headerRow.AppendChild(CellulePV("Décision", true, "2400", PrimaryNavy, false, "FFFFFF", JustificationValues.Center));
            headerRow.AppendChild(CellulePV("Mention", true, "1800", PrimaryNavy, false, "FFFFFF", JustificationValues.Center));
            table.AppendChild(headerRow);

            // Remplissage des étudiants
            int index = 0;
            foreach (var etudiant in etudiants.OrderBy(e => e.NumeroOrdre))
            {
                index++;
                string cellBg = "FFFFFF";

                string mentionObs = !string.IsNullOrWhiteSpace(etudiant.Mention) ? etudiant.Mention : etudiant.Observation;
                if (string.IsNullOrWhiteSpace(mentionObs) || mentionObs == "Aucune") mentionObs = "—";

                string decisionFormatee = etudiant.Decision;
                if (decisionFormatee != null && decisionFormatee.StartsWith("Ajourné", StringComparison.OrdinalIgnoreCase) && !decisionFormatee.Contains("("))
                {
                    decisionFormatee = "Ajourné (Rattrapage)";
                }

                var dataRow = new TableRow();
                dataRow.AppendChild(CellulePV(index.ToString(), false, "800", cellBg, false, "000000", JustificationValues.Center));
                dataRow.AppendChild(CellulePV(etudiant.NomPrenom, false, "3200", cellBg, false, "000000", JustificationValues.Left));
                dataRow.AppendChild(CellulePV(etudiant.Matricule, false, "2000", cellBg, false, "000000", JustificationValues.Center));
                dataRow.AppendChild(CellulePV(etudiant.MoyenneGenerale.ToString("0.00", CultureInfo.InvariantCulture), false, "1800", cellBg, false, "000000", JustificationValues.Center));
                dataRow.AppendChild(CellulePV(decisionFormatee, false, "2400", cellBg, false, "000000", JustificationValues.Center));
                dataRow.AppendChild(CellulePV(mentionObs, false, "1800", cellBg, false, "000000", JustificationValues.Center));
                table.AppendChild(dataRow);
            }

            body.AppendChild(table);
            body.AppendChild(CreerParagraphe("", "10", false, JustificationValues.Left, "000000", "200"));
        }

        // ── 6. Statistiques de la Session ────────────────────────────────────

        private void AjouterStatistiquesTable(Body body, List<Etudiant> etudiants)
        {
            int total = etudiants.Count;
            int admis = etudiants.Count(e => e.Decision != null && e.Decision.StartsWith("Admis", StringComparison.OrdinalIgnoreCase));
            int ajournes = etudiants.Count(e => e.Decision != null && (e.Decision.StartsWith("Ajourné", StringComparison.OrdinalIgnoreCase) || e.Decision.StartsWith("Rattrapage", StringComparison.OrdinalIgnoreCase)));

            double moyenneSession = total > 0 ? etudiants.Average(e => (double)e.MoyenneGenerale) : 0;
            int tauxAdmission = total > 0 ? (int)Math.Round((double)admis / total * 100) : 0;

            body.AppendChild(CreerParagraphe("Statistiques de la Session", "24", true, JustificationValues.Left, PrimaryNavy, "120"));

            var table = new Table();
            table.AppendChild(CreerProprietesTableau(true));

            // En-têtes : Effectif | Moyenne Session | Taux d'Admission | Ajournés
            var headerRow = new TableRow();
            headerRow.AppendChild(CellulePV("Effectif", true, "2600", PrimaryNavy, false, "FFFFFF", JustificationValues.Center));
            headerRow.AppendChild(CellulePV("Moyenne Session", true, "2600", PrimaryNavy, false, "FFFFFF", JustificationValues.Center));
            headerRow.AppendChild(CellulePV("Taux d'Admission", true, "2600", PrimaryNavy, false, "FFFFFF", JustificationValues.Center));
            headerRow.AppendChild(CellulePV("Ajournés", true, "2600", PrimaryNavy, false, "FFFFFF", JustificationValues.Center));
            table.AppendChild(headerRow);

            // Ligne de valeurs
            var dataRow = new TableRow();
            dataRow.AppendChild(CellulePV($"{total} étudiants", false, "2600", "FFFFFF", false, "000000", JustificationValues.Center));
            dataRow.AppendChild(CellulePV($"{moyenneSession:0.00} / 20", false, "2600", "FFFFFF", false, "000000", JustificationValues.Center));
            dataRow.AppendChild(CellulePV($"{tauxAdmission}%", false, "2600", "FFFFFF", false, "000000", JustificationValues.Center));
            dataRow.AppendChild(CellulePV($"{ajournes} (rattrapage)", false, "2600", "FFFFFF", false, "000000", JustificationValues.Center));
            table.AppendChild(dataRow);

            body.AppendChild(table);
            body.AppendChild(CreerParagraphe("", "10", false, JustificationValues.Left, "000000", "240"));
        }

        // ── 7. Signatures ────────────────────────────────────────────────────

        private void AjouterZoneSignatures(Body body)
        {
            body.AppendChild(CreerParagraphe("Signatures", "24", true, JustificationValues.Left, PrimaryNavy, "160"));

            var table = new Table();
            var tblPr = new TableProperties();
            tblPr.AppendChild(new TableWidth { Type = TableWidthUnitValues.Pct, Width = "5000" });
            tblPr.AppendChild(new TableBorders
            {
                TopBorder = new TopBorder { Val = BorderValues.None },
                BottomBorder = new BottomBorder { Val = BorderValues.None },
                LeftBorder = new LeftBorder { Val = BorderValues.None },
                RightBorder = new RightBorder { Val = BorderValues.None },
                InsideHorizontalBorder = new InsideHorizontalBorder { Val = BorderValues.None },
                InsideVerticalBorder = new InsideVerticalBorder { Val = BorderValues.None }
            });
            table.AppendChild(tblPr);

            var row = new TableRow();

            // Col 1 : Le Président du Jury
            var c1 = new TableCell();
            c1.AppendChild(CreerParagraphe("Le Président du Jury", "20", true, JustificationValues.Left, "000000"));
            c1.AppendChild(new TableCellProperties(new TableCellWidth { Type = TableWidthUnitValues.Pct, Width = "1666" }));
            row.AppendChild(c1);

            // Col 2 : Le Secrétaire
            var c2 = new TableCell();
            c2.AppendChild(CreerParagraphe("Le Secrétaire", "20", true, JustificationValues.Center, "000000"));
            c2.AppendChild(new TableCellProperties(new TableCellWidth { Type = TableWidthUnitValues.Pct, Width = "1666" }));
            row.AppendChild(c2);

            // Col 3 : L'Administration
            var c3 = new TableCell();
            c3.AppendChild(CreerParagraphe("L'Administration", "20", true, JustificationValues.Right, "000000"));
            c3.AppendChild(new TableCellProperties(new TableCellWidth { Type = TableWidthUnitValues.Pct, Width = "1666" }));
            row.AppendChild(c3);

            table.AppendChild(row);
            body.AppendChild(table);
        }

        // ── Helpers OpenXml ─────────────────────────────────────────────────

        private Run CreerRun(string texte, string fontSize, bool bold, string couleur)
        {
            var run = new Run();
            run.AppendChild(new Text(texte) { Space = SpaceProcessingModeValues.Preserve });

            var props = new RunProperties();
            props.AppendChild(new FontSize { Val = fontSize });
            if (bold) props.AppendChild(new Bold());
            if (!string.IsNullOrEmpty(couleur) && couleur != "000000") props.AppendChild(new Color { Val = couleur });
            run.PrependChild(props);

            return run;
        }

        private Paragraph CreerParagraphe(string texte, string fontSize, bool bold,
            JustificationValues justification, string couleur = "000000", string spaceAfter = "100")
        {
            var para = new Paragraph();
            var run = CreerRun(texte, fontSize, bold, couleur);
            para.AppendChild(run);

            para.ParagraphProperties = new ParagraphProperties
            {
                Justification = new Justification { Val = justification },
                SpacingBetweenLines = new SpacingBetweenLines { After = spaceAfter }
            };
            return para;
        }

        private TableCell CellulePV(string texte, bool estEntete, string largeur,
            string couleurFond, bool italic = false, string fontColor = "000000",
            JustificationValues? alignement = null, string rowHeight = null)
        {
            var cell = new TableCell();
            var para = new Paragraph();
            var run = CreerRun(texte, estEntete ? "20" : "18", estEntete, fontColor);
            if (italic) run.RunProperties.AppendChild(new Italic());

            para.AppendChild(run);
            para.ParagraphProperties = new ParagraphProperties
            {
                Justification = new Justification { Val = alignement ?? JustificationValues.Left },
                SpacingBetweenLines = new SpacingBetweenLines { After = "40", Before = "40" }
            };
            cell.AppendChild(para);

            var cellProps = new TableCellProperties();
            cellProps.AppendChild(new TableCellWidth { Type = TableWidthUnitValues.Dxa, Width = largeur });
            cellProps.AppendChild(new Shading { Fill = couleurFond, Val = ShadingPatternValues.Clear });

            // Marges internes des cellules (Padding)
            cellProps.AppendChild(new TableCellMargin
            {
                TopMargin = new TopMargin { Width = "100", Type = TableWidthUnitValues.Dxa },
                BottomMargin = new BottomMargin { Width = "100", Type = TableWidthUnitValues.Dxa },
                LeftMargin = new LeftMargin { Width = "150", Type = TableWidthUnitValues.Dxa },
                RightMargin = new RightMargin { Width = "150", Type = TableWidthUnitValues.Dxa }
            });

            var borders = new TableCellBorders
            {
                TopBorder = new TopBorder { Val = BorderValues.Single, Size = 4, Color = BorderColor },
                BottomBorder = new BottomBorder { Val = BorderValues.Single, Size = 4, Color = BorderColor },
                LeftBorder = new LeftBorder { Val = BorderValues.Single, Size = 4, Color = BorderColor },
                RightBorder = new RightBorder { Val = BorderValues.Single, Size = 4, Color = BorderColor }
            };
            cellProps.AppendChild(borders);
            cell.PrependChild(cellProps);

            return cell;
        }

        private TableProperties CreerProprietesTableau(bool fullWidth)
        {
            var tblPr = new TableProperties();
            if (fullWidth)
                tblPr.AppendChild(new TableWidth { Type = TableWidthUnitValues.Pct, Width = "5000" });

            var tblBorders = new TableBorders
            {
                TopBorder = new TopBorder { Val = BorderValues.Single, Size = 6, Color = BorderColor },
                BottomBorder = new BottomBorder { Val = BorderValues.Single, Size = 6, Color = BorderColor },
                LeftBorder = new LeftBorder { Val = BorderValues.Single, Size = 6, Color = BorderColor },
                RightBorder = new RightBorder { Val = BorderValues.Single, Size = 6, Color = BorderColor },
                InsideHorizontalBorder = new InsideHorizontalBorder { Val = BorderValues.Single, Size = 4, Color = BorderColor },
                InsideVerticalBorder = new InsideVerticalBorder { Val = BorderValues.Single, Size = 4, Color = BorderColor }
            };
            tblPr.AppendChild(tblBorders);
            return tblPr;
        }
    }
}
