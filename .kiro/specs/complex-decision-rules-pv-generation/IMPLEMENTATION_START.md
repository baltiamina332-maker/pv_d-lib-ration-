# Implementation Quick Start Guide

**Status**: ✅ Design Complete - Ready to Begin Phase 4  
**Date**: 27 July 2026

---

## Where We Are

✅ **Requirements**: Fully specified with 13 detailed requirements  
✅ **Design**: Complete with architecture, data models, services, and workflow diagrams  
✅ **Institutional Decisions**: Proceeding with hierarchical rescue priority strategy  

## Your Next Steps

### 1️⃣ Start Phase 4.1 - Data Model Updates (1-2 days)

**Primary Files to Modify**:
- `Models/Etudiant.cs` - Add new properties for complex decision logic
- `Models/DecisionRule.cs` - CREATE NEW
- `Models/ArchiveRecord.cs` - CREATE NEW

**Tasks**:
1. Open `Models/Etudiant.cs`
2. Add these properties to the Etudiant class:
   ```csharp
   public int EctsValides { get; set; }          // ECTS credits
   public string Statut { get; set; }            // "Ancien" or "Nouveau"
   public decimal MoyenneUE { get; set; }        // UE average
   public string Decision { get; set; }          // Decision result
   public string Mention { get; set; }           // Honor grade
   public string Observation { get; set; }       // Reasoning
   public string RescueType { get; set; }        // Which rescue applied
   ```

3. Create two new model files (see design.md Section 2.2 and 2.3 for full implementations)

4. Verify build compiles with no errors

### 2️⃣ Proceed to Phase 4.2 - Decision Engine (2-3 days)

**Primary File to Create**:
- `Services/DecisionEngine.cs` - Core decision logic

**Key Method**:
```csharp
public void CalculateDecisionForStudent(Etudiant student)
{
    // Implement all 7 decision cases:
    // Case 1a: MG ≥ 10, ECTS ≤ 15 → Admis
    // Case 1b: MG ≥ 10, 15 < ECTS ≤ 22 → Décision Conseil
    // Case 1c: MG ≥ 10, ECTS > 22 → Admis avec modération
    // Case 2a: MG < 10, ECTS > 22 → Redouble/Exclu
    // Case 2b: MG < 10, ECTS ≤ 22 (no rescue) → Conseil d'École
    // Case 3a: Rescue Ancien (MG ≥ 9.7) → Admis avec modération
    // Case 3b: Rescue Nouveau (MG ≥ 9.5) → Admis avec modération
}
```

### 3️⃣ Continue to Phase 4.3 - CSV Import (1-2 days)

**File to Modify**:
- `Services/ExcelImportService.cs`

**Tasks**:
1. Add CsvHelper NuGet package
2. Implement `LireDoonneesCsv()` method to parse CSV files
3. Update `LireDonnees()` to route by file extension (.xlsx → existing, .csv → new)
4. Add comprehensive data validation

### 4️⃣ Phase 4.4 - Document Generation (3-4 days)

**Primary File to Create**:
- `Services/DocumentGenerator.cs`

**Key Methods**:
- `GenerateWordPV()` - Create formal PV documents
- `GenerateExcelExport()` - Export results to Excel
- `ArchiveDocument()` - Save and log archive

### 5️⃣ Phase 4.5 - UI Integration (2-3 days)

**Files to Modify**:
- `Views/MainWindow.xaml` - Add new DataGrid columns
- `Views/ImportPanel.xaml.cs` - Support CSV files
- `Views/ExportPanel.xaml.cs` (create if needed) - Add export buttons

### 6️⃣ Phase 4.6 - Configuration (1 day)

**File to Create**:
- `Services/ConfigurationService.cs`

### 7️⃣ Phase 4.7 - Testing (2-3 days)

Comprehensive testing of all workflows

---

## Key Resources

📖 **Full Design Document**: `design.md` (Sections 1-11)  
📋 **Requirements**: `requirements.md` (13 detailed requirements)  
✅ **Tasks Checklist**: `tasks.md` (detailed implementation breakdown)  
🎯 **Acceptance Criteria**: `requirements.md` (for verification)  

---

## Important Implementation Notes

### Decision Logic Priority (Hierarchical Rescue)

When a student qualifies for multiple rescue conditions:

1. **First**: Check MG-based rescue (Ancien at 9.7, then Nouveau at 9.5)
2. **Then**: Check UE-based rescue (if no MG-based match)
3. **Result**: Apply FIRST matching condition

Example: An "Ancien" student with MG=9.8 matches both:
- Condition 1 (Ancien, MG ≥ 9.7) ✅ APPLIED
- Condition 3 (UE-based) ❌ Skipped

### Edge Cases to Handle

- Boundary values: MG=10.0, ECTS=15, ECTS=22, MG=9.7, MG=9.5
- Missing data: Set defaults (ECTS=0, Statut="Nouveau", MoyenneUE=0.0)
- Invalid data: Log specific errors with row numbers
- CSV vs Excel: Consistent parsing for both formats

### File Paths and Naming

- **Word PV**: `PV_[ClasseGroupe]_[YYYY-MM-DD_HHMMSS].docx`
- **Excel Export**: `PV_Results_[ClasseGroupe]_[YYYY-MM-DD_HHMMSS].xlsx`
- **Archive**: `[UserDocuments]/PV_Archives/[YYYY]/[MM]/[filename]`

### Error Handling

- Display specific error messages (row/column information)
- Log all errors with timestamp and context
- Never silently fail - always notify user

---

## Success Checklist

✅ All data models created with required properties  
✅ DecisionEngine calculates decisions correctly for all 7 cases  
✅ CSV import working alongside Excel import  
✅ DataGrid displays decisions with proper formatting  
✅ Word PV documents generate successfully  
✅ Statistics calculated and displayed  
✅ Archive system functional  
✅ Error handling comprehensive  
✅ All tests passing  
✅ Build verified - no errors  

---

## Questions?

Refer to:
- **Architecture questions**: See design.md Section 1
- **Data model questions**: See design.md Section 2
- **Service implementation**: See design.md Section 3-4
- **Workflow questions**: See design.md Section 5
- **Requirement clarifications**: See requirements.md

---

**Ready to begin? Start with Phase 4.1 - Data Model Updates**

Next message: I'll be ready to help you implement any of these phases. Just ask! 🚀
