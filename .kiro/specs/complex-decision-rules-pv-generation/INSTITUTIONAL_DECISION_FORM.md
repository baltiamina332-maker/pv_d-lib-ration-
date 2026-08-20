# 📋 Institutional Decision Form

**Project**: PV Délibération - Complex Decision Rules Feature  
**Date**: 27 July 2026  
**Purpose**: Collect institutional decisions needed before Implementation can proceed

---

## Instructions

Each section below presents a decision item that requires institutional approval. 

**For Each Item**:
1. Read the description and options
2. Consult with appropriate stakeholders
3. Mark your decision (✅ YES or ❌ NO) 
4. Sign and return to development team

---

## 📋 DECISION 1: Case 1c Rule (ECTS > 22 when MG ≥ 10)

**What Is This?**

The handwritten requirements document explicitly shows two decisions for students with MG ≥ 10:
- ECTS ≤ 15 → Admis ✅
- 15 < ECTS ≤ 22 → Décision Conseil ✅

BUT what about students with ECTS > 22 when MG ≥ 10? The source document is ambiguous.

**Current Specification**:
```
IF MG ≥ 10 AND ECTS > 22
THEN Decision = "Admis avec modération"
     Mention = "ECTS non complets"
```

**Options**:

**Option A: ✅ CONFIRM** — Keep current spec (Admis avec modération)
```
Rationale: [space for institution explanation]
```

**Option B: ❌ ALTERNATIVE** — Different decision type
```
Specify what decision should be used instead:
_____________________________________________

Rationale: [space for institution explanation]
```

**Option C: ❌ ALTERNATIVE** — This case never occurs in practice
```
Explanation: [space for institution explanation]
```

**Your Decision** (Mark one):

- [ ] **Option A**: Confirm "Admis avec modération" ✅
- [ ] **Option B**: Use different decision: ________________
- [ ] **Option C**: Case doesn't apply: ________________

**Approved By**: ________________  
**Title**: ________________  
**Date**: ________________  
**Signature**: ________________

---

## 📋 DECISION 2: Session Management Columns

**What Is This?**

The system needs to distinguish between:
- Principale session (regular session)
- Rattrapage session (makeup/resit session)

**Current Specification**:
```
Sessions are imported separately (one Excel import = one session)
Session info NOT included in Excel file columns
```

**Options**:

**Option A: ✅ CONFIRM** — Current approach (separate imports)
```
Procedure:
1. Export student data for Principale session → Import into PV app
2. Export student data for Rattrapage session → Import into PV app separately
Session type is managed externally (not in Excel file)
```

**Option B: ❌ ALTERNATIVE** — Add session columns to Excel
```
Excel should include NEW columns:
- id_session (e.g., "S1_2026", "S2_2026")
- type_session (e.g., "Principale", "Rattrapage")

These columns would be included in the import process
```

**Your Decision** (Mark one):

- [ ] **Option A**: Keep separate session imports (current approach) ✅
- [ ] **Option B**: Add session columns to Excel file

**If Option B**, provide Excel column structure:
```
Current columns:
N°, Nom et Prénom, Matricule, Classe, Année, MG, ECTS, Statut, Moyenne_UE

Add to: ________________________
        ________________________
```

**Approved By**: ________________  
**Title**: ________________  
**Date**: ________________  
**Signature**: ________________

---

## 📋 DECISION 3: Mention Thresholds (Honor Grades)

**What Is This?**

When students are admitted, they receive an honor designation (Mention) based on their GPA (MG).

**Current Specification**:
| Decision | Mention | MG Threshold |
|----------|---------|--------------|
| Admis | Très Bien | MG ≥ 16.0 |
| Admis | Bien | 14.0 ≤ MG < 16.0 |
| Admis | Assez Bien | 12.0 ≤ MG < 14.0 |
| Admis | Passable | 10.0 ≤ MG < 12.0 |

**Thresholds Used**: 16 / 14 / 12 / 10 (standard Tunisian/French university thresholds)

**Options**:

**Option A: ✅ CONFIRM** — Keep current thresholds
```
Rationale: These are ESPRIT's official thresholds
Source: [ESPRIT Règlement des Études, page ___]
```

**Option B: ❌ ALTERNATIVE** — Use different thresholds
```
Proposed thresholds:
- Très Bien: MG ≥ ______
- Bien: ______ ≤ MG < ______
- Assez Bien: ______ ≤ MG < ______
- Passable: ______ ≤ MG < ______

Rationale: [space for institution explanation]

Source: [ESPRIT Règlement des Études, page ___]
```

**Your Decision** (Mark one):

- [ ] **Option A**: Confirm 16/14/12/10 thresholds ✅
- [ ] **Option B**: Use different thresholds (specify above)

**Approved By**: ________________  
**Title**: ________________  
**Date**: ________________  
**Signature**: ________________

---

## 📋 DECISION 4: 🔴 CRITICAL - Multiple Rescue Condition Priority

**What Is This?**

When a student qualifies for MULTIPLE rescue conditions simultaneously, which one takes priority?

**Example Situation**:
```
Student: 
  Name: Ancien Étudiant
  Status: Ancien (returning student)
  MG: 9.8
  ECTS: 18 (≤ 22, so eligible for rescue)
  Moyenne_UE: 8.0

Rescue Conditions This Student Meets:
  ✅ Rescue Rule 1 (Ancien): Ancien AND MG ≥ 9.7 → YES
  ✅ Rescue Rule 3 (UE-based): MG ≥ 8.0 AND Moyenne_UE ≥ 7.0 → YES

QUESTION: Which reason should be recorded in the official decision?
```

**Current Specification (Risky)**:
```
Apply "first matching condition" in code
Problem: Depends on implementation order, not true business rule
Risk: Identical students could get different decisions
```

**Three Solutions**:

### **Option A: All Applicable Reasons in Observation** (Most Transparent)

```
Decision: "Admis avec modération"
Mention: "Rachat validé (Critères multiples)"
Observation: "Ancien étudiant (MG ≥ 9.7) + Moyenne UE suffisante (≥ 7.0)"

✅ Pros:
  • Complete transparency — jury sees all reasons
  • Fair to student — all qualifying criteria documented
  • Auditable — clear record of decision justification
  
❌ Cons:
  • More complex display in UI
  • Observation field can get long
  • May require DataGrid column width adjustment
```

### **Option B: Explicit Hierarchical Priority** (Simplest)

Institution defines priority order. Example:

```
Priority Rules (Institution defines):
1. If MG-based rescue applies → Use it (ignore UE-based)
2. If MG-based rescue doesn't apply → Check UE-based

For Above Example:
  Rule 1 applies (Ancien, MG ≥ 9.7) → Decision: "Admis avec modération"
  Mention: "Rachat validé (Ancien)"
  Observation: "Ancien étudiant - rachat par MG ≥ 9.7"
  (UE-based criterion is ignored, not mentioned)

✅ Pros:
  • Simple, deterministic logic
  • Same student always gets same decision
  • Easy to code and maintain
  
❌ Cons:
  • Some information is discarded (UE criterion not mentioned)
  • Institution must formally define hierarchy
  • Could feel arbitrary to jury members
```

### **Option C: Separate Decision Type per Rescue** (Complete Tracking)

```
Introduce new decision types:
  • "Admis (Rachat Ancien)"
  • "Admis (Rachat Nouveau)"
  • "Admis (Rachat UE)"

For Above Example:
  Meets multiple criteria:
  Could show as "Admis (Rachat Ancien)" OR "Admis (Rachat UE)" 
  OR track BOTH with separate decision types

✅ Pros:
  • No information loss
  • Full audit trail
  • Transparent about rescue type
  
❌ Cons:
  • More complex decision model
  • UI may need redesign
  • More database fields/complexity
```

---

**Which Option Does Institution Prefer?**

- [ ] **Option A**: List all applicable rescue reasons in Observation (Most transparent)
  
- [ ] **Option B**: Use explicit priority hierarchy (Simplest)
  
  If Option B: Define priority order:
  ```
  Priority 1: ________________
  Priority 2: ________________
  Priority 3: ________________
  ```
  
- [ ] **Option C**: Separate decision type per rescue (Complete tracking)

**Explanation/Rationale**:
```
[Space for institution to explain their choice]




```

**Approved By**: ________________  
**Title**: ________________  
**Date**: ________________  
**Signature**: ________________

---

## ✅ SUMMARY CHECKLIST

Before returning form, ensure all items completed:

- [ ] **Decision 1 (Case 1c)**: Option selected and signed
- [ ] **Decision 2 (Session Management)**: Option selected and signed
- [ ] **Decision 3 (Mention Thresholds)**: Option selected and signed
- [ ] **Decision 4 (Rescue Priority)**: Option selected, rationale provided, and signed

---

## 📝 RETURN INSTRUCTIONS

**Please return this form to**: [Development Team Contact]  
**Deadline**: ASAP (blocks Design phase start)  
**Format**: 
- [ ] Original signed PDF
- [ ] Digital form with e-signatures
- [ ] Scanned document
- [ ] Email with typed responses

---

## 📞 QUESTIONS DURING DECISION PROCESS

If any item requires clarification:

**For Decision 1 (Case 1c)**: Contact pedagogical authority  
**For Decision 2 (Session Management)**: Contact data manager  
**For Decision 3 (Mention Thresholds)**: Contact academic affairs/registrar  
**For Decision 4 (Rescue Priority)**: Contact jury president/institution leadership  

**For General Project Questions**: Contact development team

---

## 📋 ONCE DECISIONS ARE RECEIVED

Development team will:
1. ✅ Update specification with institutional decisions
2. ✅ Proceed to Design Phase
3. ✅ Create technical architecture and data models
4. ✅ Schedule Design review with stakeholders

**Timeline After Receipt**:
- Design Phase: 3-5 business days
- Design Review: 1-2 business days
- Implementation Phase: 2-3 weeks
- Testing & Deployment: 1 week

---

**Form Status**: Ready for Institutional Completion  
**Date Created**: 27 July 2026  
**Last Updated**: 27 July 2026
