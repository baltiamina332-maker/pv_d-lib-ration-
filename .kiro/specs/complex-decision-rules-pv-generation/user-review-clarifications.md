# User Review & Clarifications - Complex Decision Rules Feature

**Date**: 27 July 2026  
**Reviewer**: Institution Stakeholder  
**Status**: Requirements Review Complete - Clarifications Documented - Pending Institutional Validation

---

## Executive Summary

Your detailed requirements review has confirmed most specifications while identifying **3 critical areas requiring institutional validation** before implementation begins. The review also revealed a significant logical risk with the current rescue condition priority system that needs resolution.

This document captures your review findings point-by-point and their integration into the final requirements.

---

## 1. Decision Rules - Detailed Review

### ✅ CONFIRMED: Case 2 (MG < 10)

**Your Statement**: "Cas 2 (MG < 10) ✅ Conforme à la feuille"

**Details Confirmed**:
- ECTS > 22 → Redouble/Exclu (à valider Conseil École) ✅
- ECTS ≤ 22 (hors rachat) → Conseil d'École ✅

**Current Spec**: Requirement 2 implements these rules exactly as confirmed

---

### ✅ CONFIRMED: Case 3 (Rachat)

**Your Statement**: "Cas 3 (Rachat) ✅ Conforme à la feuille, exactement comme noté"

**Details Confirmed**:
- Ancien: MG ≥ 9.7 ✅
- Nouveau: MG ≥ 9.5 ✅
- Par UE: MG ≥ 8 ET Moyenne_UE ≥ 7 ✅

**Current Spec**: Requirement 3 implements these rules exactly as confirmed

**Note**: Current spec uses corrected MG threshold for UE-based rescue (≥8.0, not ≥10.0)

---

### ⚠️ PENDING VALIDATION: Case 1c (ECTS > 22 when MG ≥ 10)

**Your Concern**:
> "Ce cas n'apparaît pas explicitement sur la feuille manuscrite d'origine. Sur la feuille, seules deux flèches partent de 'MG ≥ 10.00' (vers ECTS≤15 et vers ]15,22]) — rien n'est prévu au-delà de 22 ECTS quand MG≥10. La mention 'Décision = Admis avec modération (Nbre ECTS non validé)' notée séparément pourrait effectivement correspondre à ce cas, mais ce n'est pas certain à 100% vu l'écriture."

**Your Recommendation**:
> "Je recommande de reconfirmer ce point directement avec le responsable pédagogique/jury avant de coder cette règle, car c'est une zone ambiguë de la feuille originale."

**Current Spec Implementation**: 
- Requirement 1, Criterion 3: Decision = "Admis avec modération" when MG ≥ 10 AND ECTS > 22
- **Status**: Marked as ⚠️ PENDING VALIDATION
- **Note Added**: "Implementation contingent on explicit approval from institution's decision authority"

**ACTION REQUIRED**: Reconfirm with pedagogical authority/jury whether this rule should be:
- **Option A**: Admis avec modération (current spec)
- **Option B**: Some other decision type
- **Option C**: Not applicable (students never have ECTS > 22 when MG ≥ 10)

---

## 2. Excel Column Structure

### ✅ CONFIRMED: Current Column Set

**Your Statement**: "Vos colonnes couvrent bien l'essentiel."

**Confirmed Columns**:
- N° ✅
- Nom et Prénom ✅
- Matricule ✅
- Classe/Groupe ✅
- Année ✅
- Moyenne générale (MG) ✅
- ECTS ✅
- Statut ✅
- Moyenne_UE ✅

---

### ⚠️ PENDING CLARIFICATION: Session Information Columns

**Your Question**:
> "Un point à vérifier : dans nos échanges précédents, vous aviez aussi les colonnes id_session et type_session (Principale/Rattrapage) dans votre import initial — sont-elles encore nécessaires pour distinguer une session normale d'un rattrapage, ou cette distinction est-elle gérée autrement (ex: import séparé par session) ?"

**Current Spec**: Column list does NOT include `id_session` or `type_session`

**Assumptions Made**:
- Sessions are imported separately (one Excel import = one session)
- Session type (Principale/Rattrapage) is managed through separate import workflows
- No session distinguishing info is needed in the Excel file itself

**ACTION REQUIRED**: Clarify institution's session management approach:
- **Option A**: Confirm current assumption (separate imports per session) ✓
- **Option B**: Session columns MUST be added to Excel structure (current spec needs update)

---

## 3. Mention (Honor Grade) Thresholds

### ⚠️ PENDING INSTITUTIONAL VALIDATION: Mention Thresholds

**Your Statement**:
> "Ce barème (16/14/12/10) est le standard le plus répandu dans les universités tunisiennes/françaises, mais je ne peux pas le confirmer comme 'officiellement celui d'ESPRIT' sans un document de référence — c'est une donnée administrative propre à votre établissement, pas une règle universelle. Vérifiez-le dans le règlement des études d'ESPRIT si possible."

**Current Spec Thresholds**:
| Decision | Mention | Threshold |
|----------|---------|-----------|
| Admis | Très Bien | MG ≥ 16.0 |
| Admis | Bien | 14.0 ≤ MG < 16.0 |
| Admis | Assez Bien | 12.0 ≤ MG < 14.0 |
| Admis | Passable | 10.0 ≤ MG < 12.0 |

**Spec Status**: Requirement 4 marked with note: "**[Subject to institutional validation]**" for each threshold

**ACTION REQUIRED**: Obtain official confirmation from ESPRIT's Règlement des Études:
- Confirm the 16/14/12/10 thresholds are correct ✓
- OR provide corrected thresholds if different
- Document institutional source for the chosen values

---

## 4. Word Document Structure

### ✅ CONFIRMED: Structure Validation

**Your Statement**: "Structure globale cohérente."

**Confirmed Elements**:
- Header with institution info ✅
- Session metadata block ✅
- Jury composition table ✅
- Results table ✅
- Statistics block ✅
- Signature section ✅
- Footer with page numbers ✅

---

### 🔧 RECOMMENDED ENHANCEMENT: ECTS Column in Word

**Your Suggestion**:
> "une colonne ou ligne indiquant le nombre d'ECTS validés/non validés par étudiant directement dans le tableau de résultats, puisque c'est la donnée qui détermine la décision — utile pour que le jury comprenne 'pourquoi' sans recalculer."

**Rationale**: 
- ECTS is a key decision determinant (especially for "Décision Conseil" cases with 15 < ECTS ≤ 22)
- Including ECTS in the official PV document improves transparency and audit trail
- Jury members can understand decision basis without external references

**Spec Status**: Requirement 9, Criterion 7 enhanced with this recommendation

**Current Implementation**: Will add ECTS_Valides column to Word Results table alongside existing columns (N°, Nom et Prénom, Matricule, MG, Decision, Mention)

---

## 5. Observation Field Wording

### 🔧 RECOMMENDED: Wording Adjustment

**Your Suggestion**:
> "Suggestion mineure : remplacez 'Nécessite approbation du Conseil' par 'Décision soumise au Conseil' pour rester neutre (le Conseil peut refuser, pas seulement approuver)."

**Original Wording**: "Nécessite approbation du Conseil"  
**Recommended Wording**: "Décision soumise au Conseil d'École"

**Rationale**: 
- Neutral wording reflects reality: Council makes final decision (can approve or reject)
- Avoids presupposition that Council will approve
- More accurate in French institutional context

**Spec Status**: Requirement 2 (Criterion 2) and related requirements updated to use neutral wording

**Implementation Note**: Configuration file will define observation text templates, allowing easy institutional customization later

---

## 6. Multiple Rescue Conditions - CRITICAL ISSUE

### 🔴 CRITICAL ISSUE IDENTIFIED: Arbitrary Priority Order

**Your Observation & Example**:
> "Votre exemple est excellent et révèle un vrai problème : un étudiant Ancien avec MG=9.8 remplit la règle A (Ancien, MG≥9.7) et potentiellement la règle B (UE-based) selon sa moyenne d'UE."

> "Le souci avec 'prendre la première qui correspond' : cela dépend arbitrairement de l'ordre dans lequel vous codez les conditions, ce qui n'est pas une vraie règle métier — c'est un choix technique arbitraire qui pourrait avantager ou désavantager injustement un étudiant selon l'implémentation."

**Concrete Problem Example**:
```
Étudiant:
  - Statut: Ancien
  - MG: 9.8
  - ECTS: 18
  - Moyenne_UE: 8.0

Rescue Conditions Met:
  ✅ Condition 1 (Ancien MG-based): Ancien AND MG ≥ 9.7 → Decision = "Admis avec modération", Mention = "Rachat validé (Ancien)"
  ✅ Condition 3 (UE-based): MG ≥ 8.0 AND Moyenne_UE ≥ 7.0 → Decision = "Admis avec modération", Mention = "Rachat UE validé"

Current Spec Behavior: "Apply first matching condition"
  Risk: If Condition 1 checked first → Mention = "Rachat validé (Ancien)"
  Risk: If Condition 3 checked first → Mention = "Rachat UE validé"
  Result: Same student, different official decision, depending on code evaluation order!
```

**Why This Is a Problem**:
1. **Not a true business rule**: Order-dependent behavior is implementation-specific, not institutional policy
2. **Unfair outcomes**: Identical students could receive different decisions based on arbitrary code ordering
3. **Audit/compliance risk**: Cannot explain to jury why identical students got different decisions
4. **Difficult to maintain**: Future code reorganization could inadvertently change student outcomes

**Your Recommendation**:
> "Institution must clarify priority logic or combination strategy"

**Possible Solutions**:

**Option A: All applicable rescue conditions listed in Observation**
```
Observation: "Ancien étudiant (MG ≥ 9.7) + Moyenne UE suffisante (≥ 7.0)"
Mention: "Rachat validé (Critères multiples)"
Decision: "Admis avec modération"
```
✅ Pros: Most transparent, jury sees all reasons
❌ Cons: Complex display, may need UI redesign

**Option B: Explicit hierarchical priority defined by institution**
```
Institution specifies: "Always apply MG-based rescue if eligible, ignore UE-based"
Result: Consistent, deterministic, clear documentation
Example: Ancien student gets "Rachat validé (Ancien)" mention regardless of UE score
```
✅ Pros: Simple, clear, deterministic
❌ Cons: Institution must formally define hierarchy; may exclude valid UE-based reasons

**Option C: Separate tracking with distinct decision types**
```
Decision: "Admis (Rachat Ancien)" vs "Admis (Rachat UE)"
Track both rescue types in system, allow jury to see both
```
✅ Pros: No information loss, provides full audit trail
❌ Cons: Complex implementation, UI may need redesign

**Spec Status**: Requirement 3, Criterion 4 marked as ⚠️ PENDING CLARIFICATION with note:
> "Status: Requires explicit institutional decision on priority logic before final deployment. If institution specifies different priority or combination logic, specification will be updated accordingly."

**ACTION REQUIRED**: Institution must decide on rescue condition handling:
1. Choose one of Options A, B, or C above
2. Document the chosen approach and rationale
3. Update Requirement 3 accordingly before Implementation begins

---

## 7. Summary Table: Validation Status

| Item | Status | Action Required | Deadline |
|------|--------|-----------------|----------|
| Case 1c (ECTS > 22, MG ≥ 10) | ⚠️ PENDING | Reconfirm with pedagogical authority | Before Implementation |
| Case 2 (MG < 10 rules) | ✅ CONFIRMED | None | — |
| Case 3 (Rachat rules) | ✅ CONFIRMED | None | — |
| Excel columns (current set) | ✅ CONFIRMED | None | — |
| Session info columns | ⚠️ UNCLEAR | Clarify session management approach | Before Implementation |
| Mention thresholds (16/14/12/10) | ⚠️ PENDING | Verify in ESPRIT Règlement des Études | Before Implementation |
| Multiple rescue conditions priority | 🔴 CRITICAL | Choose Option A, B, or C and document | Before Design Phase |
| ECTS in Word PV | 🔧 ENHANCEMENT | Approved for implementation | — |
| Observation wording | 🔧 ADJUSTMENT | Neutral wording approved | — |

---

## Next Steps

### ✅ COMPLETED (Current Session)

1. ✅ Detailed requirements review completed
2. ✅ All clarifications documented
3. ✅ Spec updated with pending validation notes
4. ✅ Critical issues identified and escalated

### ⏳ PENDING - INSTITUTIONAL DECISION (Before Design Phase)

**MUST COMPLETE BEFORE Design Phase Begins**:

1. **⚠️ Reconfirm Case 1c** (ECTS > 22 when MG ≥ 10):
   - Contact: Pedagogical Authority/Jury President
   - Timeline: ASAP
   - Deliverable: Confirm Decision type or provide alternative

2. **⚠️ Clarify Session Management** (id_session, type_session columns):
   - Contact: Data Manager/IT Administrator
   - Timeline: ASAP
   - Deliverable: Confirm Excel column structure OR update to include session info

3. **⚠️ Validate Mention Thresholds** (16/14/12/10):
   - Contact: Academic Affairs/Registrar
   - Timeline: ASAP
   - Deliverable: Confirm thresholds in official Règlement des Études

4. **🔴 CRITICAL - Rescue Condition Priority** (Multiple rescue conditions):
   - Contact: Institution Leadership/Jury President
   - Timeline: ASAP (blocks Design phase)
   - Deliverable: Decision on Option A, B, or C + documentation + any needed spec updates

### 🚀 AFTER Institutional Decisions Confirmed

1. Update requirements.md with any institutional decisions
2. Proceed to Design Phase
3. Create technical architecture and data models
4. Design Word document template
5. Design decision rules configuration system

---

## Key Dates

| Milestone | Date |
|-----------|------|
| Requirements Review Completed | 27 July 2026 |
| Institutional Decisions Deadline | TBD (Target: ASAP) |
| Design Phase Start | After decisions confirmed |
| Implementation Phase Start | After Design approval |

---

## Questions & Contact

If any clarifications needed on this review document, refer to:
1. Original handwritten requirements sheet (for Case 1-3 validation)
2. ESPRIT Règlement des Études (for mention thresholds)
3. Institutional data structure documentation (for session info)
4. Jury meeting notes (for rescue condition priority discussions)

---

**Document Status**: ✅ Complete - Ready for Institutional Review  
**Last Updated**: 27 July 2026  
**Next Review**: After institutional decisions received
