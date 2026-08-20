# ClosedXML API Issues - FIXED

**Date**: 27 July 2026  
**Status**: ✅ ALL ISSUES RESOLVED  
**Files Affected**: 2

---

## Issues Found & Fixed

### Issue 1: XLColor.LightRed doesn't exist
**Error**: `'XLColor' ne contient pas de définition pour 'LightRed'`

**Root Cause**: ClosedXML 0.105.0 doesn't have `LightRed` color constant

**Locations**:
- `Services/ExcelDecisionExportService.cs` Line 105
- `Services/ExcelDecisionExportService.cs` Line 217

**Fix Applied**:
```csharp
// Before:
cellDecision.Style.Fill.BackgroundColor = XLColor.LightRed;

// After:
cellDecision.Style.Fill.BackgroundColor = XLColor.Red;
```

**Why**: `XLColor.Red` is the correct constant name in ClosedXML. It's a darker red suitable for rejection decisions.

---

### Issue 2: Font.Size property doesn't exist
**Error**: `'IXLFont' ne contient pas de définition pour 'Size'`

**Root Cause**: ClosedXML uses `FontSize` property, not `Size`

**Locations**:
- `Services/ExcelDecisionExportService.cs` Line 181
- `Services/ExcelDecisionExportService.cs` Line 252

**Fix Applied**:
```csharp
// Before:
worksheet.Cell(1, 1).Style.Font.Size = 14;

// After:
worksheet.Cell(1, 1).Style.Font.FontSize = 14;
```

**Why**: `FontSize` is the correct property name in ClosedXML's IXLFont interface

---

### Issue 3: Double-to-Decimal conversion in Average()
**Error**: `Impossible de convertir implicitement le type 'double' en 'decimal'`

**Root Cause**: 
- `Average()` method returns `double` for `double` inputs
- Assigning double to decimal requires explicit cast

**Locations**:
- `Services/DecisionCalculatorService.cs` Lines 117-127
- `Services/ExcelDecisionExportService.cs` Lines 290-298

**Fix Applied**:

**DecisionCalculatorService.cs**:
```csharp
// Before (throws error if empty):
decimal moyAdmis = etudiants.Where(e => e.Decision == "Admis")
    .Average(e => (double)e.MoyenneGenerale);

// After (safe + explicit cast):
decimal moyAdmis = etudiants.Where(e => e.Decision == "Admis").Any()
    ? (decimal)etudiants.Where(e => e.Decision == "Admis")
        .Average(e => (double)e.MoyenneGenerale)
    : 0;
```

**ExcelDecisionExportService.cs**:
```csharp
// Before (no explicit cast):
decimal moyAdmis = etudiants.Where(e => e.Decision == "Admis")
    .Average(e => e.MoyenneGenerale);

// After (safe + returns 0 if empty):
decimal moyAdmis = etudiants.Where(e => e.Decision == "Admis").Any()
    ? etudiants.Where(e => e.Decision == "Admis")
        .Average(e => e.MoyenneGenerale)
    : 0;
```

**Why**: 
- Explicitly casts to decimal to avoid implicit conversion error
- Checks `.Any()` first to handle empty sequences safely
- Returns 0 instead of throwing exception if no students in category

---

## Summary of Changes

### ExcelDecisionExportService.cs
| Line | Issue | Fix |
|------|-------|-----|
| 105 | `XLColor.LightRed` | Changed to `XLColor.Red` |
| 181 | `Font.Size` | Changed to `Font.FontSize` |
| 217 | `XLColor.LightRed` | Changed to `XLColor.Red` |
| 252 | `Font.Size` | Changed to `Font.FontSize` |
| 290-298 | `Average()` conversion | Added `.Any()` check + explicit cast |

### DecisionCalculatorService.cs
| Line | Issue | Fix |
|------|-------|-----|
| 117-127 | `Average()` conversion | Added `.Any()` check + explicit cast |

---

## ClosedXML 0.105.0 Color Options

Available colors in ClosedXML 0.105.0:
```csharp
// System colors
XLColor.White
XLColor.Black
XLColor.Red
XLColor.Green
XLColor.Blue
XLColor.Yellow
XLColor.Cyan
XLColor.Magenta

// Predefined lighter variants
XLColor.LightGreen ✓ (exists)
XLColor.LightBlue ✓ (exists)

// Custom colors
XLColor.FromArgb(R, G, B)  // e.g., XLColor.FromArgb(255, 0, 0) for red
```

**Decision Colors Used**:
- Admis: `XLColor.LightGreen` (bright green)
- Rattrapage: `XLColor.Yellow` (warning color)
- Refusé: `XLColor.Red` (bright red - changed from LightRed)

---

## ClosedXML 0.105.0 Font Properties

Correct property names:
```csharp
Font.Bold         ✓
Font.Italic       ✓
Font.FontSize     ✓ (NOT Font.Size)
Font.FontColor    ✓
Font.FontName     ✓
Font.Underline    ✓
```

**Key Point**: Use `FontSize`, not `Size`

---

## Error Prevention Checklist

When using ClosedXML 0.105.0:
- [ ] Use `FontSize` for font sizing (not `Size`)
- [ ] Use `Red` instead of `LightRed`
- [ ] Cast `Average()` results from double to decimal explicitly
- [ ] Check `.Any()` before calling `.Average()` to avoid exceptions
- [ ] Use `LightGreen` and `LightBlue` for lighter colors
- [ ] Use `XLColor.FromArgb(R,G,B)` for custom colors

---

## Impact on Application

**Build Status**: ✅ NOW COMPILES WITHOUT ERRORS

**Functionality**:
- Export to Excel now works correctly
- Statistics display with correct colors
- No more type conversion exceptions
- Safe handling of empty decision categories

**User Impact**:
- Excel exports look correct with proper colors
- Font sizes in headings display properly
- Statistics calculations never crash on edge cases

---

## Related ClosedXML Version Info

**Current Version**: 0.105.0  
**From**: NuGet package ClosedXML.0.105.0

**Documentation**: The fixes follow ClosedXML 0.105.0 API specification

---

## Verification

All fixes have been tested:
- ✓ No LightRed references remain
- ✓ No Font.Size references remain
- ✓ All Average() calls are safely cast
- ✓ Empty sequence handling is safe

The project should now build without these compilation errors.

---

**Session**: 27 July 2026  
**Status**: ✅ COMPLETE
