# Design Document: Complex Decision Rules and PV Generation System

**Phase**: Design Phase  
**Date**: 27 July 2026  
**Status**: ✅ Complete - Ready for Implementation

---

## 1. System Architecture Overview

### 1.1 High-Level Architecture

```
┌─────────────────────────────────────────────────────────────────────┐
│                       User Interface Layer                          │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐              │
│  │ Import Panel │  │ DataGrid View│  │ Export Panel │              │
│  └──────────────┘  └──────────────┘  └──────────────┘              │
└─────────────────────────────────────────────────────────────────────┘
                              ↕
┌─────────────────────────────────────────────────────────────────────┐
│                    Business Logic Layer                             │
│  ┌──────────────────────────────────────────────────────────────┐  │
│  │  DecisionEngine (Decision Rule Evaluation)                   │  │
│  │  ├─ CalculateDecision()                                      │  │
│  │  ├─ EvaluateRescueConditions()                               │  │
│  │  └─ AssignMention()                                          │  │
│  └──────────────────────────────────────────────────────────────┘  │
│  ┌──────────────────────────────────────────────────────────────┐  │
│  │  DocumentGenerator (Word PV & Excel Export)                  │  │
│  │  ├─ GenerateWordPV()                                         │  │
│  │  ├─ GenerateExcelExport()                                    │  │
│  │  └─ ArchiveDocument()                                        │  │
│  └──────────────────────────────────────────────────────────────┘  │
│  ┌──────────────────────────────────────────────────────────────┐  │
│  │  StatisticsCalculator                                        │  │
│  │  ├─ CalculateDistribution()                                  │  │
│  │  ├─ CalculateAverages()                                      │  │
│  │  └─ GenerateStatisticsSummary()                              │  │
│  └──────────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────────┘
                              ↕
┌─────────────────────────────────────────────────────────────────────┐
│                      Data Access Layer                              │
│  ┌──────────────────────────────────────────────────────────────┐  │
│  │  ExcelImportService (Enhanced with CSV support)              │  │
│  │  ├─ LireDonneesClassique() - reads .xlsx                     │  │
│  │  ├─ LireDoonneesCsv() - NEW: reads .csv                      │  │
│  │  └─ ValidateImportData()                                     │  │
│  └──────────────────────────────────────────────────────────────┘  │
│  ┌──────────────────────────────────────────────────────────────┐  │
│  │  ConfigurationService                                        │  │
│  │  ├─ LoadRulesConfiguration()                                 │  │
│  │  ├─ ValidateConfiguration()                                  │  │
│  │  └─ GetActiveRules()                                         │  │
│  └──────────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────────┘
                              ↕
┌─────────────────────────────────────────────────────────────────────┐
│                   Model/Entity Layer                                │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐              │
│  │  Etudiant    │  │ DecisionRule │  │ ArchiveRecord│              │
│  └──────────────┘  └──────────────┘  └──────────────┘              │
└─────────────────────────────────────────────────────────────────────┘
```

### 1.2 Technology Stack

- **Language**: C# (.NET Framework)
- **UI Framework**: WPF (Windows Presentation Foundation)
- **Excel Import/Export**: ClosedXML library
- **Word Document Generation**: OpenXML SDK (System.IO.Packaging, DocumentFormat.OpenXml)
- **CSV Parsing**: CsvHelper library (to be added)
- **Data Storage**: In-memory collections + optional SQLite for archive logging
- **Configuration Format**: JSON or YAML (for future extensibility per Req 11)

---

## 2. Data Models

### 2.1 Enhanced Etudiant Model

**File**: `Models/Etudiant.cs`

```csharp
public class Etudiant
{
    // Existing properties
    public int ID { get; set; }
    public string Nom { get; set; }
    public string Prenom { get; set; }
    public string Matricule { get; set; }
    public string Classe { get; set; }
    
    // NEW properties for complex decision logic
    public decimal MoyenneGenerale { get; set; }          // MG value
    public int EctsValides { get; set; }                   // ECTS credits
    public string Statut { get; set; }                     // "Ancien" or "Nouveau"
    public decimal MoyenneUE { get; set; }                 // Average per Disciplinary Unit
    
    // Decision calculation results
    public string Decision { get; set; }                   // Decision type (enum-like)
    public string Mention { get; set; }                    // Honor grade
    public string Observation { get; set; }                // Reasoning text
    
    // Metadata
    public DateTime DateCalcul { get; set; }              // When decision was calculated
    public string RescueType { get; set; }                 // Which rescue condition applied (if any)
    
    // Methods
    public bool EstAncienEtudiant => Statut == "Ancien";
    
    public void CalculerDecisionEtMention()
    {
        // Implemented according to requirements
        // Returns void, updates Decision, Mention, Observation properties
    }
}
```

**Key Changes**:
- Added ECTS, Statut, MoyenneUE properties for decision rule evaluation
- Added Decision, Mention, Observation fields to store calculation results
- Added CalculerDecisionEtMention() method (already implemented from previous session)
- Added RescueType tracking for auditability

### 2.2 DecisionRule Configuration Model

**File**: `Models/DecisionRule.cs` (NEW)

```csharp
public class DecisionRuleSet
{
    public decimal MG_Threshold_Case1 { get; set; } = 10.0m;
    public decimal MG_Threshold_Case2 { get; set; } = 10.0m;
    public int ECTS_Threshold_1 { get; set; } = 15;        // Case 1a/1b boundary
    public int ECTS_Threshold_2 { get; set; } = 22;        // Case 1b/1c boundary
    
    // Rescue thresholds
    public decimal MG_Rescue_Ancien { get; set; } = 9.7m;
    public decimal MG_Rescue_Nouveau { get; set; } = 9.5m;
    public decimal MG_Rescue_UEBased { get; set; } = 8.0m;
    public decimal MoyenneUE_Rescue { get; set; } = 7.0m;
    
    // Mention thresholds
    public decimal MentionThreshold_TresBien { get; set; } = 16.0m;
    public decimal MentionThreshold_Bien { get; set; } = 14.0m;
    public decimal MentionThreshold_AssezBien { get; set; } = 12.0m;
    public decimal MentionThreshold_Passable { get; set; } = 10.0m;
    
    // Rescue priority strategy
    public RescuePriorityStrategy PriorityStrategy { get; set; } = RescuePriorityStrategy.Hierarchical;
}

public enum RescuePriorityStrategy
{
    Hierarchical,        // First matching condition applies
    ShowAll,             // All applicable conditions in Observation
    SeparateTracking     // Multiple rescue reasons tracked distinctly
}
```

### 2.3 Archive Record Model

**File**: `Models/ArchiveRecord.cs` (NEW)

```csharp
public class ArchiveRecord
{
    public int ID { get; set; }
    public string DocumentFileName { get; set; }
    public string ClasseGroupe { get; set; }
    public int StudentCount { get; set; }
    public int CountAdmis { get; set; }
    public int CountConseil { get; set; }
    public int CountRedouble { get; set; }
    public DateTime ArchivedDate { get; set; }
    public string ArchivedByUser { get; set; }
    public string ArchivePath { get; set; }
    public DateTime CreatedDate { get; set; }
}
```

---

## 3. Service Layer Architecture

### 3.1 DecisionEngine Service

**File**: `Services/DecisionEngine.cs` (NEW)

**Responsibilities**:
- Evaluate complex decision rules for students
- Apply rescue conditions with configured priority
- Calculate mentions based on MG thresholds
- Generate observation/rationale text

**Key Methods**:
```csharp
public class DecisionEngine
{
    private DecisionRuleSet _rules;
    
    public void CalculateDecisionsForBatch(List<Etudiant> students)
    {
        foreach (var student in students)
        {
            CalculateDecisionForStudent(student);
        }
    }
    
    public void CalculateDecisionForStudent(Etudiant student)
    {
        // Implementation flow:
        // 1. Validate input data
        // 2. Determine decision case (MG ≥ 10 vs < 10)
        // 3. Apply ECTS-based rules
        // 4. Check rescue conditions if applicable
        // 5. Calculate mention
        // 6. Generate observation text
        // 7. Set timestamp
    }
    
    private DecisionResult EvaluateCase1(Etudiant student)
    {
        // MG ≥ 10 logic
    }
    
    private DecisionResult EvaluateCase2(Etudiant student)
    {
        // MG < 10 logic
    }
    
    private bool TryApplyRescueCondition(Etudiant student, out RescueResult rescue)
    {
        // Check rescue conditions in priority order
    }
    
    private string CalculateMention(decimal mg, string decision)
    {
        // Apply thresholds from _rules
    }
    
    private string GenerateObservation(Etudiant student, DecisionResult result)
    {
        // Create rationale text
    }
}
```

### 3.2 DocumentGenerator Service

**File**: `Services/DocumentGenerator.cs` (NEW)

**Responsibilities**:
- Generate Word PV documents
- Create Excel exports
- Archive documents with proper folder structure
- Log archive records

**Key Methods**:
```csharp
public class DocumentGenerator
{
    public string GenerateWordPV(List<Etudiant> students, SessionMetadata session)
    {
        // 1. Create Word document structure
        // 2. Add header with institution info
        // 3. Add session metadata
        // 4. Generate results table (N°, Nom, Matricule, MG, ECTS, Decision, Mention)
        // 5. Calculate and add statistics
        // 6. Add signature section
        // 7. Add footer with generation timestamp
        // 8. Save to [UserDocuments]/[generated_filename].docx
        // 9. Archive to [UserDocuments]/PV_Archives/[YYYY]/[MM]/
        // 10. Return file path
    }
    
    public string GenerateExcelExport(List<Etudiant> students, string classeGroupe)
    {
        // Export results maintaining data integrity
        // Format: PV_Results_[ClasseGroupe]_[YYYY-MM-DD_HHMMSS].xlsx
    }
    
    private void CreateWordHeader(Document doc, string institution)
    {
        // Add institution logo or name
        // Add "PV DE DÉLIBÉRATION" title
    }
    
    private void CreateResultsTable(Document doc, List<Etudiant> students)
    {
        // Create table with proper formatting
        // Add row striping for readability
        // Set column widths
    }
    
    private void CreateStatisticsSection(Document doc, List<Etudiant> students)
    {
        // Display: Total, per decision type, percentages, averages
    }
    
    private void ArchiveDocument(string filePath, ArchiveRecord record)
    {
        // Create archive folder structure if needed
        // Copy document
        // Log to database
    }
}
```

### 3.3 ExcelImportService Enhancement

**File**: `Services/ExcelImportService.cs` (MODIFIED)

**New Responsibilities**:
- Support CSV import in addition to Excel
- Route file to appropriate parser based on extension
- Validate imported data comprehensively

**Key Changes**:
```csharp
public class ExcelImportService
{
    // EXISTING method
    public List<Etudiant> LireDonneesClassique(string filePath)
    {
        // Existing Excel (.xlsx) import logic
    }
    
    // NEW method for CSV support
    public List<Etudiant> LireDoonneesCsv(string filePath)
    {
        // 1. Open CSV file
        // 2. Parse columns: id_etudiant, nom, prenom, matricule, classe_groupe, 
        //    id_session, type_session, annee_universitaire, moyenne_generale, 
        //    ects, statut, moyenne_ue
        // 3. Create Etudiant objects
        // 4. Validate data
        // 5. Return list
    }
    
    // MODIFIED method to route based on extension
    public List<Etudiant> LireDonnees(string filePath)
    {
        string extension = Path.GetExtension(filePath).ToLower();
        
        return extension switch
        {
            ".xlsx" or ".xlsm" => LireDonneesClassique(filePath),
            ".csv" => LireDoonneesCsv(filePath),
            _ => throw new NotSupportedException($"Format {extension} not supported")
        };
    }
    
    private void ValidateImportData(List<Etudiant> students)
    {
        // Check for non-numeric ECTS
        // Check for invalid Statut values
        // Check for missing required columns
        // Log specific errors per row
    }
}
```

### 3.4 StatisticsCalculator Service

**File**: `Services/StatisticsCalculator.cs` (NEW)

```csharp
public class StatisticsCalculator
{
    public DeliberationStatistics CalculateStatistics(List<Etudiant> students)
    {
        return new DeliberationStatistics
        {
            TotalStudents = students.Count,
            CountAdmis = students.Count(s => s.Decision == "Admis"),
            CountDecisionConseil = students.Count(s => s.Decision == "Décision Conseil"),
            CountConseilEcole = students.Count(s => s.Decision == "Conseil d'École"),
            CountRedouble = students.Count(s => s.Decision == "Redouble / Exclu"),
            AverageMG = students.Average(s => s.MoyenneGenerale),
            AverageMGByDecision = CalculateAverageByDecision(students),
            // ... more statistics
        };
    }
}

public class DeliberationStatistics
{
    public int TotalStudents { get; set; }
    public int CountAdmis { get; set; }
    public int CountDecisionConseil { get; set; }
    public int CountConseilEcole { get; set; }
    public int CountRedouble { get; set; }
    public decimal AverageMG { get; set; }
    public Dictionary<string, decimal> AverageMGByDecision { get; set; }
    // ... more properties
}
```

### 3.5 ConfigurationService

**File**: `Services/ConfigurationService.cs` (NEW)

```csharp
public class ConfigurationService
{
    private string _configPath;
    private DecisionRuleSet _currentRules;
    
    public void LoadConfiguration(string configPath)
    {
        // Parse JSON/YAML configuration
        // Validate thresholds are in acceptable ranges
        // Set _currentRules
    }
    
    public DecisionRuleSet GetActiveRules()
    {
        return _currentRules ?? GetDefaultRules();
    }
    
    public void SaveConfiguration(DecisionRuleSet rules)
    {
        // Serialize to JSON/YAML
        // Write to file
    }
    
    private DecisionRuleSet GetDefaultRules()
    {
        return new DecisionRuleSet();  // Returns default thresholds
    }
}
```

---

## 4. UI Layer Updates

### 4.1 Import Panel Enhancement

**File**: `Views/ImportPanel.xaml.cs` (MODIFIED)

**Changes**:
- Update file dialog to accept `.xlsx` and `.csv` files
- Display appropriate error messages for unsupported formats
- Show import progress
- Display validation errors with specific row/column information

### 4.2 DataGrid Display Updates

**File**: `Views/MainWindow.xaml` (MODIFIED)

**Changes**:
- Add new columns: ECTS, Statut, Decision, Mention, Observation
- Implement color coding for Decision column (green/yellow/red)
- Enable column sorting and resizing
- Show statistics panel below grid (Total students, decision distribution, averages)

### 4.3 Export/Generate Panel

**File**: `Views/ExportPanel.xaml.cs` (NEW)

**Features**:
- "Export to Excel" button → calls DocumentGenerator.GenerateExcelExport()
- "Generate PV (Word)" button → calls DocumentGenerator.GenerateWordPV()
- "Open Archive Folder" button → opens file explorer to archive directory
- Status messages showing file paths and success/failure

---

## 5. Workflow and Integration Points

### 5.1 Import Workflow

```
User selects file (Excel or CSV)
    ↓
ExcelImportService.LireDonnees() routes to appropriate parser
    ↓
Parser creates Etudiant objects from file data
    ↓
ValidateImportData() checks for errors
    ↓
IF errors → Display error dialog with specific issues
ELSE → Load students into memory
    ↓
DecisionEngine.CalculateDecisionsForBatch() evaluates all students
    ↓
Update DataGrid with results
    ↓
Display statistics in statistics panel
```

### 5.2 Export Workflow

```
User clicks "Export to Excel" or "Generate PV"
    ↓
Collect active student list from DataGrid
    ↓
DocumentGenerator creates appropriate document type
    ↓
Document saved to [UserDocuments]/[generated_filename]
    ↓
Archive copy created in [UserDocuments]/PV_Archives/[YYYY]/[MM]/
    ↓
Archive record logged to database
    ↓
Display success message with file path
```

---

## 6. Database Schema (for Archive Logging)

**Table**: ArchiveRecords

```sql
CREATE TABLE ArchiveRecords (
    ID INT PRIMARY KEY IDENTITY(1,1),
    DocumentFileName NVARCHAR(255) NOT NULL,
    ClasseGroupe NVARCHAR(50),
    StudentCount INT,
    CountAdmis INT,
    CountConseil INT,
    CountRedouble INT,
    ArchivedDate DATETIME,
    ArchivedByUser NVARCHAR(100),
    ArchivePath NVARCHAR(500),
    CreatedDate DATETIME DEFAULT GETDATE()
);
```

---

## 7. Error Handling Strategy

### 7.1 Import Errors
- Missing columns → List column names with specific rows
- Non-numeric ECTS → Show row number and problematic value
- Invalid Statut → Show expected values and actual value
- File access errors → Show file path and permission issue

### 7.2 Decision Calculation Errors
- Missing required data → Show which field (ECTS, Statut, etc.)
- Out-of-range values → Show acceptable range and actual value
- Edge case handling → Explicit rounding rules for boundary values

### 7.3 Document Generation Errors
- Insufficient disk space → Suggest cleanup
- Permission denied → Suggest running as admin
- Filename collision → Automatically append numeric suffix

### 7.4 Logging
- All errors logged to `[AppData]/PV_Deliberation/logs/app_[YYYY-MM-DD].log`
- Log format: `[TIMESTAMP] [LEVEL] [MODULE] [MESSAGE]`

---

## 8. Configuration File Format (Future Use)

**File**: `config/decision_rules.json`

```json
{
  "version": "1.0",
  "decisionRules": {
    "case1": {
      "mgThreshold": 10.0,
      "ectsThresholds": [15, 22]
    },
    "rescue": {
      "ancien": { "mgThreshold": 9.7 },
      "nouveau": { "mgThreshold": 9.5 },
      "ueBase": {
        "mgThreshold": 8.0,
        "moyenneUEThreshold": 7.0
      }
    },
    "mentions": {
      "tresBien": 16.0,
      "bien": 14.0,
      "assezBien": 12.0,
      "passable": 10.0
    },
    "rescuePriorityStrategy": "Hierarchical"
  }
}
```

---

## 9. Implementation Phases Breakdown

### Phase 4.1: Data Model Updates (1-2 days)
- [ ] Enhance Etudiant.cs with new properties
- [ ] Create DecisionRule.cs model
- [ ] Create ArchiveRecord.cs model
- [ ] Update existing code that instantiates Etudiant

### Phase 4.2: Core Decision Engine (2-3 days)
- [ ] Implement DecisionEngine.cs
- [ ] Implement CalculateDecisionForStudent() with all cases
- [ ] Implement rescue condition evaluation
- [ ] Implement mention calculation
- [ ] Add unit tests for decision logic

### Phase 4.3: Import Service Enhancement (1-2 days)
- [ ] Add CSV parsing to ExcelImportService
- [ ] Add routing logic to LireDonnees()
- [ ] Add CsvHelper NuGet package
- [ ] Add data validation
- [ ] Test with sample CSV files

### Phase 4.4: Document Generation (3-4 days)
- [ ] Implement DocumentGenerator.cs
- [ ] Word document structure and formatting
- [ ] Excel export with proper formatting
- [ ] Archive folder creation and logging
- [ ] Test document generation

### Phase 4.5: UI Integration (2-3 days)
- [ ] Update DataGrid to display new columns
- [ ] Add color coding for Decision column
- [ ] Add statistics panel
- [ ] Add export/generate buttons
- [ ] Add error dialogs

### Phase 4.6: Configuration Service (1 day)
- [ ] Implement ConfigurationService.cs
- [ ] Create default configuration
- [ ] Test configuration loading

### Phase 4.7: Testing & Validation (2-3 days)
- [ ] Unit tests for DecisionEngine
- [ ] Integration tests for import → decision → export workflow
- [ ] Edge case testing (boundary values, data validation)
- [ ] Manual UAT with stakeholders

---

## 10. Success Criteria

✅ All 13 requirements fully implemented  
✅ Complex decision logic working correctly for all 7 cases  
✅ CSV import supported alongside Excel  
✅ Word PV documents generated with proper formatting  
✅ Statistics calculated and displayed  
✅ Archive system functional with database logging  
✅ Error handling comprehensive and user-friendly  
✅ Configuration extensible for future rule changes  
✅ All edge cases handled (boundary values, missing data, etc.)  
✅ Code follows project conventions and is maintainable  

---

## 11. Design Review Checklist

- [x] Architecture supports all requirements
- [x] Data models accommodate all needed fields
- [x] Service layer properly separated from UI
- [x] Error handling strategy comprehensive
- [x] Extensibility considered (configuration service, future enhancements)
- [x] Performance acceptable (batch decision calculations, UI responsiveness)
- [x] Integration points clear (import → calculate → display → export)
- [x] Database schema supports archive logging
- [x] File naming and path conventions consistent
- [x] Logging and diagnostics infrastructure planned

---

**Design Status**: ✅ COMPLETE - Ready for Implementation Phase

**Next Step**: Begin Phase 4.1 (Data Model Updates) following the implementation phases breakdown above.
