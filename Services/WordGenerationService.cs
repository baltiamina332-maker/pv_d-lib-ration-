using System;
using System.Collections.Generic;
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
        public string NomEtablissement { get; set; } = "École Supérieure Privée d'Ingénierie et de Technologies";
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
    /// Service pour générer des documents Word (PV de Délibération) conformes au CDC
    /// </summary>
    public class WordGenerationService
    {
        /// <summary>
        /// Générer un PV de délibération au format Word
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
                        AnneeUniversitaire = etudiants[0].AnneeUniversitaire ?? DateTime.Now.Year.ToString(),
                        TypeSession = "Principale"
                    };
                }

                string cheminComplet = Path.Combine(cheminSortie, nomFichier);
                if (!Directory.Exists(cheminSortie)) Directory.CreateDirectory(cheminSortie);

                using (var fileStream = new FileStream(cheminComplet, FileMode.Create, FileAccess.ReadWrite))
                using (WordprocessingDocument wordDocument = WordprocessingDocument.Create(fileStream, WordprocessingDocumentType.Document))
                {
                    var mainPart = wordDocument.AddMainDocumentPart();
                    mainPart.Document = new Document();
                    var body = new Body();
                    mainPart.Document.AppendChild(body);

                    // Configuration page (A4 paysage pour le tableau)
                    var sectPr = new SectionProperties();
                    var pgSz = new PageSize { Width = 15840, Height = 12240, Orient = PageOrientationValues.Landscape };
                    sectPr.AppendChild(pgSz);
                    var pgMar = new PageMargin { Top = 720, Bottom = 720, Left = 720, Right = 720 };
                    sectPr.AppendChild(pgMar);
                    body.AppendChild(sectPr);

                    // 1. En-tête institutionnel
                    AjouterEntete(body, jury);

                    // 2. Titre PV
                    AjouterTitrePV(body, jury);

                    // 3. Composition du jury
                    AjouterCompositionJury(body, jury);

                    // 4. Tableau des résultats
                    AjouterTableauResultats(body, etudiants);

                    // 5. Statistiques de synthèse
                    AjouterStatistiques(body, etudiants);

                    // 6. Signatures
                    AjouterSignatures(body, jury);
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WORD] Erreur génération PV: {ex.Message}");
                return false;
            }
        }

        // Surcharge pour compatibilité avec l'ancien code
        public bool GenererPV(List<Etudiant> etudiants, string cheminSortie, string nomFichier, string classeGroupe)
        {
            return GenererPV(etudiants, cheminSortie, nomFichier, classeGroupe, null);
        }

        private void AjouterEntete(Body body, InfosJury jury)
        {
            // En-tête institutionnel
            body.AppendChild(CreerParagraphe(jury.NomEtablissement, "26", true, JustificationValues.Center, "003366"));
            body.AppendChild(CreerParagraphe("", "8", false, JustificationValues.Left));
        }

        private void AjouterTitrePV(Body body, InfosJury jury)
        {
            body.AppendChild(CreerParagraphe("PROCÈS-VERBAL DE DÉLIBÉRATION", "32", true, JustificationValues.Center, "003366"));
            body.AppendChild(CreerParagraphe("", "10", false, JustificationValues.Left));
            body.AppendChild(CreerParagraphe($"Année Universitaire : {jury.AnneeUniversitaire}    |    Classe / Groupe : {jury.Filiere}    |    Session : {jury.TypeSession}", "20", true, JustificationValues.Center, "333333"));
            body.AppendChild(CreerParagraphe($"Date de délibération : {jury.DateDeliberation:dd/MM/yyyy}", "18", false, JustificationValues.Center, "555555"));
            body.AppendChild(CreerParagraphe("", "12", false, JustificationValues.Left));
        }

        private void AjouterCompositionJury(Body body, InfosJury jury)
        {
            body.AppendChild(CreerParagraphe("COMPOSITION DU JURY", "22", true, JustificationValues.Left, "003366"));

            var membres = new List<(string Titre, string Nom)>();
            if (!string.IsNullOrWhiteSpace(jury.PresidentJury))
                membres.Add(("Président du Jury", jury.PresidentJury));
            if (!string.IsNullOrWhiteSpace(jury.MembreJury1))
                membres.Add(("Membre", jury.MembreJury1));
            if (!string.IsNullOrWhiteSpace(jury.MembreJury2))
                membres.Add(("Membre", jury.MembreJury2));
            if (!string.IsNullOrWhiteSpace(jury.Secretaire))
                membres.Add(("Secrétaire / Rapporteur", jury.Secretaire));

            if (membres.Any())
            {
                var table = new Table();
                table.AppendChild(CreerProprietesTableau(false));

                foreach (var m in membres)
                {
                    var row = new TableRow();
                    row.AppendChild(CellulePV(m.Titre, false, "3500", "F0F4F8", true, "003366"));
                    row.AppendChild(CellulePV(m.Nom, false, "5500", "FFFFFF", false, "000000"));
                    table.AppendChild(row);
                }
                body.AppendChild(table);
            }
            else
            {
                body.AppendChild(CreerParagraphe("(Non renseigné)", "18", false, JustificationValues.Left, "999999"));
            }

            body.AppendChild(CreerParagraphe("", "12", false, JustificationValues.Left));
        }

        private void AjouterTableauResultats(Body body, List<Etudiant> etudiants)
        {
            body.AppendChild(CreerParagraphe("TABLEAU DES RÉSULTATS DE LA DÉLIBÉRATION", "22", true, JustificationValues.Left, "003366"));

            var table = new Table();
            table.AppendChild(CreerProprietesTableau(true));

            // En-tête conforme CDC: N°, Nom et Prénom, Matricule, Moyenne générale, Décision, Mention / Observation
            var headerRow = new TableRow();
            headerRow.AppendChild(CellulePV("N°", true, "800", "003366", false, "FFFFFF"));
            headerRow.AppendChild(CellulePV("Nom et Prénom", true, "3800", "003366", false, "FFFFFF"));
            headerRow.AppendChild(CellulePV("Matricule / CIN", true, "2000", "003366", false, "FFFFFF"));
            headerRow.AppendChild(CellulePV("Moyenne générale", true, "2000", "003366", false, "FFFFFF"));
            headerRow.AppendChild(CellulePV("Décision", true, "2200", "003366", false, "FFFFFF"));
            headerRow.AppendChild(CellulePV("Mention / Observation", true, "3200", "003366", false, "FFFFFF"));
            table.AppendChild(headerRow);

            // Données avec lignes alternées (Annexe C)
            int index = 0;
            foreach (var etudiant in etudiants.OrderBy(e => e.NumeroOrdre))
            {
                index++;
                string couleurLigne = (index % 2 == 0) ? "F8F9FA" : "FFFFFF";

                string mentionObs = !string.IsNullOrWhiteSpace(etudiant.Mention) ? etudiant.Mention : etudiant.Observation;
                if (string.IsNullOrWhiteSpace(mentionObs)) mentionObs = "—";

                var dataRow = new TableRow();
                dataRow.AppendChild(CellulePV(etudiant.NumeroOrdre.ToString(), false, "800", couleurLigne));
                dataRow.AppendChild(CellulePV(etudiant.NomPrenom, false, "3800", couleurLigne));
                dataRow.AppendChild(CellulePV(etudiant.Matricule, false, "2000", couleurLigne));
                dataRow.AppendChild(CellulePV(etudiant.MoyenneGenerale.ToString("0.000"), false, "2000", couleurLigne));
                dataRow.AppendChild(CellulePV(etudiant.Decision, false, "2200", couleurLigne));
                dataRow.AppendChild(CellulePV(mentionObs, false, "3200", couleurLigne));
                table.AppendChild(dataRow);
            }

            body.AppendChild(table);
            body.AppendChild(CreerParagraphe("", "12", false, JustificationValues.Left));
        }

        private void AjouterStatistiques(Body body, List<Etudiant> etudiants)
        {
            int total = etudiants.Count;
            int admis = etudiants.Count(e => e.Decision != null && e.Decision.StartsWith("Admis", StringComparison.OrdinalIgnoreCase));
            int ajourne = etudiants.Count(e => e.Decision != null && (e.Decision.StartsWith("Ajourné", StringComparison.OrdinalIgnoreCase) || e.Decision.StartsWith("Rattrapage", StringComparison.OrdinalIgnoreCase)));
            int exclu = etudiants.Count(e => e.Decision != null && (e.Decision.StartsWith("Exclu", StringComparison.OrdinalIgnoreCase) || e.Decision.StartsWith("Refusé", StringComparison.OrdinalIgnoreCase)));

            decimal tauxReussite = total > 0 ? Math.Round((decimal)admis / total * 100, 1) : 0;
            decimal tauxAjourne = total > 0 ? Math.Round((decimal)ajourne / total * 100, 1) : 0;
            decimal tauxExclu = total > 0 ? Math.Round((decimal)exclu / total * 100, 1) : 0;

            decimal moyGenerale = total > 0 ? Math.Round((decimal)etudiants.Average(e => (double)e.MoyenneGenerale), 3) : 0;
            decimal moyAdmis = admis > 0 ? Math.Round((decimal)etudiants.Where(e => e.Decision != null && e.Decision.StartsWith("Admis", StringComparison.OrdinalIgnoreCase)).Average(e => (double)e.MoyenneGenerale), 3) : 0;

            // Mentions
            int tb = etudiants.Count(e => e.Mention == "Très Bien");
            int b = etudiants.Count(e => e.Mention == "Bien");
            int ab = etudiants.Count(e => e.Mention == "Assez Bien");
            int p = etudiants.Count(e => e.Mention == "Passable");

            body.AppendChild(CreerParagraphe("STATISTIQUES DE SYNTHÈSE", "22", true, JustificationValues.Left, "003366"));

            var table = new Table();
            table.AppendChild(CreerProprietesTableau(false));

            var lignes = new List<(string Label, string Valeur)>
            {
                ("Effectif total d'étudiants", total.ToString()),
                ("Nombre d'admis", $"{admis} ({tauxReussite}%)"),
                ("  • Mention Très Bien", tb.ToString()),
                ("  • Mention Bien", b.ToString()),
                ("  • Mention Assez Bien", ab.ToString()),
                ("  • Mention Passable", p.ToString()),
                ("Nombre d'ajournés (session de rattrapage)", $"{ajourne} ({tauxAjourne}%)"),
                ("Nombre d'exclus / non admis", $"{exclu} ({tauxExclu}%)"),
                ("Moyenne générale de la promotion", $"{moyGenerale:0.000} / 20"),
                ("Moyenne générale des admis", $"{moyAdmis:0.000} / 20"),
            };

            foreach (var (label, valeur) in lignes)
            {
                var row = new TableRow();
                row.AppendChild(CellulePV(label, false, "5000", "F8F9FA", label.StartsWith("  ")));
                row.AppendChild(CellulePV(valeur, false, "3000", "FFFFFF", true));
                table.AppendChild(row);
            }

            body.AppendChild(table);
            body.AppendChild(CreerParagraphe("", "12", false, JustificationValues.Left));
        }

        private void AjouterSignatures(Body body, InfosJury jury)
        {
            body.AppendChild(CreerParagraphe("ZONES DE SIGNATURES", "22", true, JustificationValues.Left, "003366"));
            body.AppendChild(CreerParagraphe("", "10", false, JustificationValues.Left));

            var table = new Table();
            table.AppendChild(CreerProprietesTableau(false));

            string president = string.IsNullOrWhiteSpace(jury.PresidentJury) ? "_______________________" : jury.PresidentJury;
            string secretaire = string.IsNullOrWhiteSpace(jury.Secretaire) ? "_______________________" : jury.Secretaire;
            string date = jury.DateDeliberation.ToString("dd/MM/yyyy");

            var row1 = new TableRow();
            row1.AppendChild(CellulePV($"Président du Jury\n{president}\n\nSignature :\n\nDate : {date}", false, "4500"));
            row1.AppendChild(CellulePV($"Secrétaire / Rapporteur\n{secretaire}\n\nSignature :\n\nDate : {date}", false, "4500"));
            table.AppendChild(row1);

            body.AppendChild(table);
        }

        // ── Helpers ─────────────────────────────────────────────────────

        private Paragraph CreerParagraphe(string texte, string fontSize, bool bold,
            JustificationValues justification, string couleur = "000000")
        {
            var para = new Paragraph();
            var run = new Run();
            run.AppendChild(new Text(texte) { Space = SpaceProcessingModeValues.Preserve });

            var props = new RunProperties();
            props.AppendChild(new FontSize { Val = fontSize });
            if (bold) props.AppendChild(new Bold());
            if (couleur != "000000") props.AppendChild(new Color { Val = couleur });
            run.PrependChild(props);

            para.AppendChild(run);
            para.ParagraphProperties = new ParagraphProperties
            {
                Justification = new Justification { Val = justification }
            };
            return para;
        }

        private TableCell CellulePV(string texte, bool estEntete, string largeur = "2000",
            string couleurFond = "FFFFFF", bool italic = false, string fontColor = "FFFFFF")
        {
            var cell = new TableCell();
            var para = new Paragraph();
            var run = new Run();
            run.AppendChild(new Text(texte) { Space = SpaceProcessingModeValues.Preserve });

            var props = new RunProperties();
            props.AppendChild(new FontSize { Val = estEntete ? "20" : "18" });
            if (estEntete) props.AppendChild(new Bold());
            if (italic && !estEntete) props.AppendChild(new Italic());
            if (estEntete) props.AppendChild(new Color { Val = fontColor });
            run.PrependChild(props);

            para.AppendChild(run);
            cell.AppendChild(para);

            var cellProps = new TableCellProperties();
            cellProps.AppendChild(new TableCellWidth { Type = TableWidthUnitValues.Dxa, Width = largeur });
            cellProps.AppendChild(new Shading { Fill = couleurFond, Val = ShadingPatternValues.Clear });

            var borders = new TableCellBorders
            {
                TopBorder = new TopBorder { Val = BorderValues.Single, Size = 4 },
                BottomBorder = new BottomBorder { Val = BorderValues.Single, Size = 4 },
                LeftBorder = new LeftBorder { Val = BorderValues.Single, Size = 4 },
                RightBorder = new RightBorder { Val = BorderValues.Single, Size = 4 }
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
                TopBorder = new TopBorder { Val = BorderValues.Single, Size = 6 },
                BottomBorder = new BottomBorder { Val = BorderValues.Single, Size = 6 },
                LeftBorder = new LeftBorder { Val = BorderValues.Single, Size = 6 },
                RightBorder = new RightBorder { Val = BorderValues.Single, Size = 6 },
                InsideHorizontalBorder = new InsideHorizontalBorder { Val = BorderValues.Single, Size = 4 },
                InsideVerticalBorder = new InsideVerticalBorder { Val = BorderValues.Single, Size = 4 }
            };
            tblPr.AppendChild(tblBorders);
            return tblPr;
        }
    }
}
