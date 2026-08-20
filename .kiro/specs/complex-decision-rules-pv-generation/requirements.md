# Requirements Document: Complex Decision Rules and PV Generation

## Introduction

The PV Délibération application needs enhanced decision logic to support complex deliberation rules based on multiple student academic factors (Grade Point Average, validated ECTS credits, student status, and disciplinary unit averages). Additionally, the application must generate formal Procès-Verbal (minutes) documents in Microsoft Word format that consolidate student decisions, jury composition, and deliberation statistics.

This feature replaces the simple binary decision system (MG ≥ 12 = Admit) with a sophisticated rule engine that evaluates seven distinct decision scenarios. The generated Word documents must comply with institutional standards for official record-keeping and archival requirements.

---

## Glossary

- **System**: The PV Délibération Application - a Windows desktop application for managing student deliberations
- **MG (Moyenne Générale)**: The student's overall Grade Point Average, calculated as a weighted average of module grades
- **ECTS**: European Credit Transfer System - numeric credits representing academic workload, ranging from 0-30 per semester
- **Statut**: Student classification indicating whether they are an "Ancien" (returning/senior) or "Nouveau" (first-year/new) student
- **Moyenne_UE**: The average grade across courses within a single Disciplinary Unit (UE - Unité d'Enseignement)
- **Décision**: The formal admission decision assigned to a student based on decision rules (e.g., Admis, Conseil d'École, Redouble)
- **Mention**: An honor designation awarded based on GPA performance (Très Bien, Bien, Assez Bien, Passable)
- **Observation**: A text field providing context or reasoning for non-standard decisions
- **PV (Procès-Verbal)**: An official minutes document recording deliberation outcomes, jury composition, signatures, and institutional metadata
- **Deliberation Session**: A scheduled meeting of faculty jury members to review and formally approve student decisions
- **Rescue/Rachat**: Special admission conditions that may apply when a student's MG is below the standard threshold but meets alternative criteria

---

## Requirements

### Requirement 1: Complex Decision Logic - Case 1 (MG ≥ 10)

**User Story:** As a deliberation jury member, I want the system to apply different decisions based on both MG and ECTS when a student achieves MG ≥ 10, so that students with complete coursework are admitted unconditionally while those with exceptional ECTS loads require council review.

**⚠️ IMPORTANT CLARIFICATION (User Review Session - 27 July 2026):**
- The original handwritten source document shows two explicit conditions for MG ≥ 10: ECTS ≤ 15 and 15 < ECTS ≤ 22
- The case for ECTS > 22 when MG ≥ 10 is ambiguous in the source document and requires validation with the responsible pedagogical authority/jury before final implementation
- The specification below implements the third case as currently understood, but final code deployment MUST be preceded by explicit sign-off from the institution's decision-making authority confirming the handling of ECTS > 22 scenarios

#### Acceptance Criteria

1. WHEN a student record has MG ≥ 10.00 AND ECTS_Valides ≤ 15, THE System SHALL assign Decision = "Admis" AND calculate Mention from MG using thresholds (Très Bien ≥ 16, Bien ≥ 14, Assez Bien ≥ 12, Passable ≥ 10)

2. WHEN a student record has MG ≥ 10.00 AND ECTS_Valides ∈ (15, 22], THE System SHALL assign Decision = "Décision Conseil" AND set Mention = "À valider par le Conseil" AND populate Observation field with count of non-validated ECTS (example: "3 ECTS non validés")

3. ⚠️ **PENDING VALIDATION**: WHEN a student record has MG ≥ 10.00 AND ECTS_Valides > 22, THE System SHALL assign Decision = "Admis avec modération" AND set Mention = "ECTS non complets" AND populate Observation field with statement "Modération requise - ECTS supérieurs aux normes" — **Status: Implementation contingent on explicit approval from institution's decision authority**

#### Property-Based Testing Strategy

- **Round-Trip Property**: For any student with valid MG ≥ 10.00, parsing decision criteria then reconstructing the decision parameters SHALL yield equivalent ECTS boundaries (15 ≤ threshold_1 ≤ 22 ≤ threshold_2)
- **Invariant Property**: After decision calculation, students with MG ≥ 10 SHALL always have Decision in set {Admis, Décision Conseil, Admis avec modération} regardless of other factors
- **Metamorphic Property**: IF Decision = "Décision Conseil", THEN ECTS_Valides ∈ (15, 22] AND Mention MUST contain "Conseil"

---

### Requirement 2: Complex Decision Logic - Case 2 (MG < 10, excluding rescue conditions)

**User Story:** As a deliberation coordinator, I want the system to automatically distinguish between students requiring external council review versus those facing potential exclusion when MG < 10, so that appropriate escalation paths are triggered.

#### Acceptance Criteria

1. WHEN a student record has MG < 10.00 AND ECTS_Valides > 22, THE System SHALL assign Decision = "Redouble / Exclu" AND set Mention = "À valider avec le Conseil d'École" AND populate Observation field with text "ECTS non validés élevés - risque d'exclusion"

2. WHEN a student record has MG < 10.00 AND ECTS_Valides ≤ 22 AND rescue conditions fail (see Requirement 3), THE System SHALL assign Decision = "Conseil d'École" AND set Mention = "Nécessite approbation du Conseil" AND leave Observation field available for council notes

#### Property-Based Testing Strategy

- **Invariant Property**: After decision calculation, students with MG < 10 AND failed rescue SHALL always have Decision in set {Redouble / Exclu, Conseil d'École}
- **Metamorphic Property**: IF Decision = "Redouble / Exclu", THEN ECTS_Valides > 22 AND MG < 10 (necessary but not sufficient conditions hold)
- **Error Condition Property**: FOR all students with Decision = "Redouble / Exclu", Observation field SHALL NOT be null or empty string

---

### Requirement 3: Rescue/Rachat Conditions (Special Admission Rules)

**User Story:** As an academic administrator, I want the system to apply rescue conditions that may elevate a student below the standard MG threshold to "Admis avec modération" status when specific criteria are met, so that talented students with minor deficiencies can progress.

**⚠️ CRITICAL ISSUE IDENTIFIED (User Review Session - 27 July 2026):**
- **Problem**: The current specification uses "first matching condition" prioritization which is arbitrary and implementation-order dependent — not a true business rule
- **Example Case**: An "Ancien" student with MG = 9.8, ECTS ≤ 22 qualifies for both Condition 1 (Ancien, MG ≥ 9.7 ✓) AND potentially Condition 3 (UE-based if Moyenne_UE ≥ 7)
- **Risk**: Depending on code evaluation order, the same student receives different Mention texts (either "Rachat validé (Ancien)" or "Rachat UE validé"), which is inconsistent and arbitrary
- **Recommendation**: Before implementation, institution MUST clarify the priority/combination logic:
  - Option A: All applicable rescue conditions are listed in Observation (most complete, but complex)
  - Option B: Explicit hierarchical priority defined by institution (e.g., "Always apply MG-based rescue if eligible, ignore UE-based")
  - Option C: Separate tracking of multiple rescue reasons with distinct decision status
- **Current Implementation**: Will use Option B (hierarchical priority) with explicit codified ordering, but REQUIRES confirmation from institution before final deployment

#### Acceptance Criteria

1. WHEN a student record has MG < 10.00 AND ECTS_Valides ≤ 22 AND EstAncienEtudiant = true AND MG ≥ 9.7, THE System SHALL assign Decision = "Admis avec modération" AND set Mention = "Rachat validé (Ancien)" AND populate Observation field with text "Ancien étudiant - rachat par MG ≥ 9.7"

2. WHEN a student record has MG < 10.00 AND ECTS_Valides ≤ 22 AND EstAncienEtudiant = false AND MG ≥ 9.5, THE System SHALL assign Decision = "Admis avec modération" AND set Mention = "Rachat validé (Nouveau)" AND populate Observation field with text "Nouvel étudiant - rachat par MG ≥ 9.5"

3. WHEN a student record has MG ≥ 8.0 AND Moyenne_UE ≥ 7.0 AND MG ≥ 10.0 AND ECTS_Valides ≤ 22, THE System SHALL assign Decision = "Admis avec modération" AND set Mention = "Rachat UE validé" AND populate Observation field with text "Moyenne UE suffisante - rachat approuvé"

4. **⚠️ PENDING CLARIFICATION**: WHEN multiple rescue conditions apply to a student record, THE System SHALL evaluate conditions in order: (1) MG-based rescue (Ancien at 9.7 threshold, then Nouveau at 9.5 threshold), (2) UE-based rescue — and SHALL apply the FIRST condition that matches, recording the matched condition type in Observation. **Status: Requires explicit institutional decision on priority logic before final deployment. If institution specifies different priority or combination logic, specification will be updated accordingly.**

#### Property-Based Testing Strategy

- **Idempotence Property**: FOR a given student record, calling the rescue evaluation function multiple times on identical input data SHALL yield the same Decision and Mention values each time
- **Confluence Property**: IF a student qualifies for multiple rescue conditions, the system SHALL select exactly one rescue type and the chosen type's Observation text SHALL appear in the Observation field (order of application does not matter to consistency)
- **Round-Trip Property**: IF Decision = "Admis avec modération" DUE TO rescue, THEN reversing the input parameters (e.g., swapping EstAncienEtudiant value) SHALL result in a different Decision or Mention value

---

### Requirement 4: Mention (Honor Grade) Assignment - Using Standard Institutional Thresholds

**User Story:** As an academic registrar, I want the system to automatically assign honor designations (Mention) based on MG performance when students are admitted, so that academic transcripts reflect student achievement levels.

**📋 INSTITUTIONAL DATA CONFIRMATION (User Review Session - 27 July 2026):**
- The thresholds specified below (16/14/12/10) represent the standard most widely adopted in Tunisian/French universities
- These thresholds are institutional-specific administrative data, NOT universal rules
- **RECOMMENDATION**: Verify these thresholds against ESPRIT institution's official academic regulations (Règlement des Études) BEFORE implementing this feature
- If thresholds differ from those specified here, Requirement 4 MUST be updated with corrected values

#### Acceptance Criteria

1. WHEN a student is assigned Decision = "Admis" (any variant) AND MG ≥ 16.0, THE System SHALL assign Mention = "Très Bien" **[Subject to institutional validation]**

2. WHEN a student is assigned Decision = "Admis" (any variant) AND 14.0 ≤ MG < 16.0, THE System SHALL assign Mention = "Bien" **[Subject to institutional validation]**

3. WHEN a student is assigned Decision = "Admis" (any variant) AND 12.0 ≤ MG < 14.0, THE System SHALL assign Mention = "Assez Bien" **[Subject to institutional validation]**

4. WHEN a student is assigned Decision = "Admis" (any variant) AND 10.0 ≤ MG < 12.0, THE System SHALL assign Mention = "Passable" **[Subject to institutional validation]**

5. WHEN a student is NOT assigned Decision = "Admis" (including "Décision Conseil", "Redouble / Exclu", "Conseil d'École"), THE System SHALL NOT calculate honor Mention and Mention field SHALL be populated by decision-specific text (see other requirements)

#### Property-Based Testing Strategy

- **Invariant Property**: After mention assignment, FOR any two students with equal MG values AND equal Decision = "Admis", their Mention values SHALL be identical
- **Partition Property**: MG thresholds (10, 12, 14, 16) SHALL partition mention categories such that no student MG value produces ambiguous mention assignment (i.e., each MG maps to exactly one mention)

---

### Requirement 5: Student Data Input and ECTS Acquisition

**User Story:** As a data entry clerk, I want to import student records from Excel files that contain ECTS values, student status, and UE averages, so that the decision engine has complete information for complex rule evaluation.

**📋 CLARIFICATION NEEDED (User Review Session - 27 July 2026):**
- Previous implementation required columns: `id_session` and `type_session` (Principale/Rattrapage) to distinguish session types
- Current implementation omits these columns — need confirmation:
  - **Option A**: Sessions are imported separately (one import per session type), so session info is not in the Excel file
  - **Option B**: Session info MUST be added to Excel file structure to properly distinguish Principale vs Rattrapage sessions
  - **Recommendation**: Clarify institution's data structure and import workflow before finalizing column requirements

#### Acceptance Criteria

1. WHEN a user imports an Excel file via the Import Excel interface, THE System SHALL read the following columns: N°, Nom et Prénom, Matricule, Classe, Année, MG, ECTS, Statut, Moyenne_UE. **Note**: If `id_session` and `type_session` columns are required by institution, specification will be updated accordingly.

2. WHEN reading an Excel file, IF column "ECTS" contains numeric values ≥ 0 AND ≤ 60, THE System SHALL populate Etudiant.EctsValides with the value; IF column is missing or contains non-numeric data, THE System SHALL log an error and set EctsValides = 0

3. WHEN reading an Excel file, IF column "Statut" contains value "Ancien", THE System SHALL set EstAncienEtudiant = true; IF contains value "Nouveau" or "New", THE System SHALL set EstAncienEtudiant = false; IF column is missing or contains unrecognized value, THE System SHALL log a warning and set EstAncienEtudiant = false (default)

4. WHEN reading an Excel file, IF column "Moyenne_UE" contains numeric values ≥ 0 AND ≤ 20, THE System SHALL populate Etudiant.MoyenneUE with the value; IF column is missing or contains non-numeric data, THE System SHALL set MoyenneUE = 0.0

5. WHEN an Excel file cannot be parsed or contains validation errors, THE System SHALL display a user-friendly error message listing specific problem rows and columns (example: "Row 5: ECTS column contains non-numeric value 'ABC'")

#### Property-Based Testing Strategy

- **Round-Trip Property**: FOR any valid student record imported from Excel, exporting that record back to Excel AND re-importing it SHALL preserve ECTS_Valides, Statut, and Moyenne_UE values within rounding tolerance (±0.01 for decimals)
- **Invariant Property**: After Excel import, the set of students in memory SHALL have cardinality equal to the count of non-header rows in the source file (excluding empty rows)

---

### Requirement 6: DataGrid Display - Decision Results

**User Story:** As a deliberation jury member, I want the DataGrid to display imported student records with calculated decisions, mentions, and observations in a formatted table, so that all deliberation outcomes are visible for review and approval.

#### Acceptance Criteria

1. THE DataGrid SHALL display the following columns in order: N°, Nom et Prénom, Matricule, Classe, MG, ECTS, Statut, Decision, Mention, Observation

2. WHEN decision calculations complete, THE DataGrid cells corresponding to Decision column SHALL be color-coded: Admis = green background, Décision Conseil = yellow background, Redouble / Exclu = red background, Conseil d'École = yellow background

3. WHEN a user views the DataGrid, ALL rows SHALL be visible without horizontal scrolling on displays ≥ 1280 pixels wide, OR columns SHALL be resizable and horizontally scrollable on narrower displays

4. WHEN a DataGrid row is selected, THE System SHALL enable additional action buttons (e.g., Edit, View Details, Export Row) in the UI; WHEN no row is selected, these buttons SHALL be disabled

5. THE DataGrid SHALL support sorting by clicking column headers (ascending/descending alphabetical or numeric order)

#### Property-Based Testing Strategy

- **Invariant Property**: The number of visible DataGrid rows SHALL equal the number of successfully imported student records (with no duplicate rows)
- **Model-Based Testing**: FOR any decision calculated in the business logic layer, the corresponding DataGrid cell Mention value SHALL match the calculated Mention value from the Decision logic layer

---

### Requirement 7: Excel Export with Decision Results

**User Story:** As an administrator, I want to export the deliberation results (including decisions, mentions, and observations) back to Excel format, so that institutional records can be archived and shared with other departments.

#### Acceptance Criteria

1. WHEN a user clicks the "Export to Excel" button, THE System SHALL create a new Excel file with filename format: `PV_Results_[ClasseGroupe]_[YYYY-MM-DD_HHMMSS].xlsx`

2. THE Excel export file SHALL contain the same columns as the DataGrid: N°, Nom et Prénom, Matricule, Classe, MG, ECTS, Statut, Decision, Mention, Observation

3. WHEN exporting, THE System SHALL preserve decimal precision: MG values SHALL display with 2 decimal places (e.g., 12.50), ECTS and other integers SHALL display without decimals

4. THE first row of the Excel export file SHALL be a formatted header row with bold font, blue background color, and white text

5. IF a filename collision occurs (file already exists), THE System SHALL automatically append a numeric suffix (e.g., `PV_Results_[...] (2).xlsx`) rather than overwriting

#### Property-Based Testing Strategy

- **Round-Trip Property**: FOR any exported Excel file, re-importing it via the Import Excel function SHALL reconstruct a student list with Decision, Mention, and Observation values identical to the originally exported data (within string matching tolerance)

---

### Requirement 8: Word Document (PV) Generation - Structure

**User Story:** As an institutional administrator, I want the system to generate formal Procès-Verbal documents in Microsoft Word format that contain deliberation records, so that official institutional records comply with archival and audit requirements.

#### Acceptance Criteria

1. WHEN a user clicks "Generate PV Word" button, THE System SHALL create a Microsoft Word (.docx) document with filename format: `PV_[ClasseGroupe]_[YYYY-MM-DD_HHMMSS].docx`

2. THE Word document SHALL include, in order: (a) Institution header with institution name, department, and deliberation title; (b) Session metadata (Academic Year, Class/Group, Deliberation Date); (c) Results table containing all student records; (d) Summary statistics; (e) Signature section; (f) Footer with document control information

3. THE Results table in the Word document SHALL display columns: N°, Nom et Prénom, Matricule, MG, ECTS, Decision, Mention with row striping (alternating white/light gray background) for readability

4. THE Summary Statistics section SHALL display: Total Student Count, Count of "Admis" decisions (all variants), Count of "Conseil d'École" decisions, Count of "Redouble / Exclu" decisions

5. THE Signature section SHALL reserve space for signatures with pre-printed labels: "Président du Jury:", "Secrétaire du Jury:", "Date:", with blank lines for handwritten signatures

#### Property-Based Testing Strategy

- **Invariant Property**: FOR any generated Word document, the count of student rows in the Results table SHALL equal the count of imported student records in the application
- **Round-Trip Property (Serialization)**: WHEN exporting to Word, IF a document is subsequently read and parsed for table content, the student records extracted from the table SHALL match the application's in-memory student list (name, decision, mention matching with allowable formatting differences)

---

### Requirement 9: Word Document (PV) Generation - Formatting and Styling

**User Story:** As an institutional administrator, I want the generated Word documents to follow consistent formatting standards with professional styling, so that official documents maintain institutional appearance and readability standards.

**📋 ENHANCEMENT RECOMMENDED (User Review Session - 27 July 2026):**
- Current specification does NOT include ECTS column in Word results table
- Recommendation: Add ECTS_Valides column to Word document Results table (alongside N°, Nom et Prénom, Matricule, MG, Decision, Mention)
- Benefit: Jury members can see ECTS data directly in the official PV document without referring back to the application or separate Excel export
- Rationale: ECTS is a key determinant of decisions (especially "Décision Conseil" cases), so including it in the official record improves transparency and auditability

#### Acceptance Criteria

1. THE Word document header (top margin area) SHALL display: institution logo (if available) OR institution name in bold 14pt font, followed by "PV DE DÉLIBÉRATION" in bold 16pt font

2. ALL table headers in the Word document SHALL use bold 11pt font with light blue background color (RGB: 192, 220, 255) or equivalent institutional color

3. ALL body text and table content in the Word document SHALL use 11pt Arial or Calibri font with 1.0 line spacing

4. THE Summary Statistics section SHALL use a separate table or formatted list with labels in bold 11pt and values in regular 11pt font

5. THE Signature section labels and blank lines SHALL display with 1.5 line spacing to accommodate handwritten signatures

6. THE document footer (bottom margin) SHALL display: "Generated: [YYYY-MM-DD HH:MM:SS]" and "Page [X] of [Y]"

7. **ENHANCEMENT**: THE Results table in Word SHALL include ECTS_Valides column (in addition to existing N°, Nom et Prénom, Matricule, MG, Decision, Mention columns) so that jury members can understand the basis for ECTS-dependent decisions directly in the official PV document

#### Property-Based Testing Strategy

- **Invariant Property**: FOR any generated Word document, ALL tables SHALL contain the same number of columns as specified in Requirement 8 (no missing or extra columns in actual output)

---

### Requirement 10: Word Document Archive and Storage

**User Story:** As an administrator, I want the system to automatically archive generated PV documents with proper folder organization and database logging, so that historical deliberation records are preserved and retrievable.

#### Acceptance Criteria

1. WHEN a Word document is successfully generated, THE System SHALL automatically create a backup copy in a dedicated archive folder: `[UserDocuments]/PV_Archives/[YYYY]/[MM]/`

2. WHEN archiving a document, THE System SHALL log the deliberation in a database record containing: Archive Date, Document Filename, Class/Group, Student Count, Count per Decision Type, Generated By User

3. WHEN a new archive folder path is created, THE System SHALL create all required parent directories if they do not exist (automatic folder creation)

4. IF a filename collision occurs in the archive folder, THE System SHALL automatically append a numeric suffix (e.g., `PV_[...] (2).docx`) rather than overwriting

5. THE System SHALL provide a user interface button "Open Archive Folder" that opens the archive directory in the system file explorer when clicked

#### Property-Based Testing Strategy

- **Invariant Property**: FOR each generated PV document, exactly one copy SHALL exist in the archive folder at the expected path location
- **Confluence Property**: The order in which multiple PV documents are archived SHALL NOT affect which document is successfully archived (archive operation is order-independent for distinct documents)

---

### Requirement 11: Parser and Pretty-Printer for Decision Rules Configuration (Future Extensibility)

**User Story:** As a system administrator, I want the decision rules to be stored in a human-readable configuration format, so that institutional rules can be modified without code changes.

#### Acceptance Criteria

1. THE System SHALL support parsing decision rules from a configuration file in YAML or JSON format containing thresholds for MG boundaries, ECTS thresholds, and rescue condition parameters

2. WHEN a configuration file is loaded, THE Parser SHALL validate that all required fields are present and numeric values fall within acceptable ranges (e.g., MG thresholds 0.0-20.0, ECTS thresholds 0-60)

3. WHEN a configuration is loaded successfully, THE System SHALL display the active rules in a read-only configuration view in the UI

4. THE Pretty-Printer (output formatter) SHALL serialize the current decision rules back to configuration file format preserving formatting and comments

5. FOR ALL valid configuration objects, parsing then printing then parsing again SHALL produce a semantically equivalent configuration (round-trip property for config serialization)

#### Property-Based Testing Strategy

- **Round-Trip Property**: FOR any valid decision rules configuration, parse(config_text) then pretty_print(parsed_config) then parse(printed_text) SHALL yield configuration data structures with identical thresholds and parameters (within floating-point precision tolerance)

---

### Requirement 12: Error Handling and User Notifications

**User Story:** As an application user, I want clear error messages and notifications when operations fail, so that I understand what went wrong and how to correct it.

#### Acceptance Criteria

1. WHEN an Excel import fails, THE System SHALL display a dialog with: Error title, specific problem description (e.g., "Row 5: ECTS contains non-numeric value"), problematic data sample, and suggested remediation

2. WHEN a Word document generation fails, THE System SHALL display a dialog with: Error message, reason for failure (e.g., insufficient permissions, disk space), and a "Retry" or "Cancel" button

3. WHEN an operation succeeds (Excel import complete, Word generated, Archive successful), THE System SHALL display a success notification with: Operation type, file path/name if applicable, and timestamp

4. ALL error messages SHALL be in the application's configured language (French for this installation) and use plain, non-technical terminology when possible

5. THE System SHALL log all errors to a application log file with timestamp, error type, user account, and full exception details for administrator diagnostics

#### Property-Based Testing Strategy

- **Error Condition Property**: FOR any operation that fails, an error notification SHALL be displayed to the user AND a log entry SHALL be written (error is never silently ignored)

---

### Requirement 13: Decision Statistics and Reporting

**User Story:** As a deliberation coordinator, I want the system to display summary statistics about decision distribution, so that I can quickly assess the outcomes of a deliberation session.

#### Acceptance Criteria

1. WHEN deliberation data is loaded, THE System SHALL calculate and display: Total student count, Count per decision type (Admis variants, Conseil d'École, Redouble/Exclu), Percentage per decision type

2. WHEN displaying statistics, THE System SHALL show average MG across all students AND average MG by decision type (e.g., "Average MG for Admis: 12.75")

3. THE statistics display panel SHALL update in real-time if student records are modified or added

4. THE System SHALL provide an option to export statistics summary to a separate document or email report

#### Property-Based Testing Strategy

- **Invariant Property**: Sum of decision type counts SHALL equal total student count
- **Metamorphic Property**: IF a student's decision changes from "Admis" to "Conseil d'École", the Admis count SHALL decrease by 1 AND the Conseil count SHALL increase by 1

---

## Acceptance Criteria Testing Strategy Summary

### Common Testing Patterns Applied

1. **Invariants**: Decision boundaries are mutually exclusive and collectively exhaustive (a student must match exactly one decision category)

2. **Round-Trip Properties**: Excel data → Database/Memory → Excel export maintains data integrity; Configuration → Parser → Pretty-Printer → Parser produces identical rules

3. **Idempotence**: Repeated decision calculations on unchanged data produce identical results

4. **Error Conditions**: Invalid Excel data, missing columns, non-numeric values are handled gracefully with specific error messages

5. **Metamorphic Relations**: When student MG increases, decision type should only improve (not regress); when ECTS increases, moderation effects should increase

### Decision Test Coverage

- **Case 1 (MG ≥ 10)**: Test all three ECTS subcases (≤15, 15<x≤22, >22)
- **Case 2 (MG < 10)**: Test both ECTS scenarios (>22, ≤22) with rescue conditions failing
- **Case 3 (Rescue)**: Test ancient student (≥9.7), new student (≥9.5), UE-based rescue combinations
- **Edge Cases**: Exact boundary values (MG=10.0, ECTS=15, ECTS=22, MG=9.5, MG=9.7)
- **Multiple Conditions**: Students qualifying for multiple rescue conditions to verify priority rules

### Non-Testable Items

- Visual appearance and exact pixel positioning (subjective; manual verification required)
- User experience flow (subjective; usability testing required)
- Performance under load with thousands of students (benchmark testing; not PBT suitable)

---

## Summary

These requirements define a comprehensive system for managing complex academic deliberations with sophisticated decision logic, professional document generation, and archival capabilities. The feature integrates with existing Excel import functionality while adding substantial new capabilities for rule evaluation, Word document generation, and institutional record-keeping.

---

## 📋 Clarifications from User Review (27 July 2026)

This section documents critical clarifications, validations, and pending decisions identified during requirements review by the institutional stakeholder.

### ✅ CONFIRMED ITEMS

1. **Case 2 Rules (MG < 10)**: ECTS > 22 → Redouble/Exclu; ECTS ≤ 22 → Conseil d'École ✅ Confirmed as matching handwritten source document exactly
2. **Case 3 Rules (Rachat)**: 
   - Ancien: MG ≥ 9.7 ✅ Confirmed
   - Nouveau: MG ≥ 9.5 ✅ Confirmed  
   - UE-based: MG ≥ 8.0 AND Moyenne_UE ≥ 7.0 ✅ Confirmed
3. **Column Structure**: Excel import columns (N°, Nom, Matricule, Classe, Année, MG, ECTS, Statut, Moyenne_UE) ✅ Confirmed

### ⚠️ PENDING VALIDATION BY INSTITUTION

| Item | Current Specification | Issue | Recommendation | Status |
|------|----------------------|-------|-----------------|--------|
| **Case 1c: ECTS > 22 when MG ≥ 10** | Decision = "Admis avec modération" | Not explicitly shown on handwritten source document; only two arrows shown leaving "MG ≥ 10" condition | Reconfirm with pedagogical authority/jury before code deployment | PENDING |
| **Multiple Rescue Conditions** | Evaluate in order: (1) MG-based, (2) UE-based; apply first match | Risk of arbitrary order-dependent outcomes; same student could receive different decisions based on implementation order | Institution must clarify priority logic or combination strategy (Options: A=all listed in Observation, B=explicit hierarchy defined, C=separate tracking) | PENDING |
| **Mention Thresholds** | 16/14/12/10 for Très Bien/Bien/Assez Bien/Passable | Standard thresholds for Tunisian/French universities but institution-specific | Verify against ESPRIT's official Règlement des Études | PENDING |
| **Session Info Columns** | Current spec omits `id_session` and `type_session` | Previous implementation referenced these columns; current version assumes separate session imports | Clarify: (A) Sessions imported separately, OR (B) Session columns must be added to Excel structure | PENDING |
| **Observation Field Wording** | "Décision soumise au Conseil" (institution-neutral) vs "Nécessite approbation du Conseil" (jury must approve) | Wording affects stakeholder expectations about decision authority | Recommend neutral "Décision soumise au Conseil d'École" wording | PENDING |

### 🔧 RECOMMENDED ENHANCEMENTS

1. **ECTS Column in Word PV**: Add ECTS_Valides to Word document Results table to improve transparency of ECTS-based decisions and provide complete audit trail in official documentation
2. **Observation Field Enhancement**: Include calculated rationale (e.g., "3 ECTS non validés" for Case 1b) to help jury members understand decision basis

---

## Next Steps

**BEFORE Implementation Phase Begins**:

1. Institutional stakeholder MUST confirm pending validation items (above)
2. If any pending items differ from current specification, requirements document MUST be updated accordingly
3. Design phase will incorporate confirmed requirements and any specification changes
4. Implementation will proceed only after Design approval based on final confirmed requirements
