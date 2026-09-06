import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../services/api.service';
import { AuthService } from '../services/auth.service';
import { Router, RouterModule, ActivatedRoute } from '@angular/router';

@Component({
  selector: 'app-wizard',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './wizard.component.html',
  styleUrls: ['./wizard.component.css']
})
export class WizardComponent implements OnInit {
  // Theme
  theme = 'light';

  // Navigation views: 'mes-classes', 'dashboard', 'wizard', 'rules', 'history', 'ml-models'
  currentView = 'mes-classes';
  
  // Enseignant Automatic Display state
  mesClassesData: any = null;
  isLoadingMesClasses = false;
  selectedClasseIndex = 0;

  // Dashboard stats
  statTotalPv = 0;
  statAvgSuccess = 0;
  statTotalClasses = 0;
  recentHistory: any[] = [];
  allHistory: any[] = [];

  // Wizard state
  currentStep = 1;
  modeCalcul = 'AUTO'; // AUTO or PRECALC
  
  // File upload state
  selectedFile: File | null = null;
  isUploading = false;
  uploadErrors: string[] = [];
  uploadWarnings: string[] = [];

  // Preview state
  parsedClasses: any[] = [];
  selectedClassIndex = 0;

  // Session & Jury state
  sessionInfo = {
    etablissement: "REPUBLIQUE TUNISIENNE - MINISTERE DE L'ENSEIGNEMENT SUPERIEUR",
    departement: "Département des Sciences & Technologies",
    anneeUniversitaire: "2025-2026",
    dateDeliberation: "Juillet 2026",
    typeSession: "Principale",
    lieu: "Salle des Conseils",
    presidentJury: "Prof. Président du Jury",
    membresJury: [
      { nom: "", fonction: "" }
    ]
  };

  // Export state
  downloadClassIndex = 0;

  // Machine Learning View State
  mlMode: 'CLASS' | 'SIMULATION' = 'CLASS';
  mlClasses: any[] = [];
  selectedMlClasseId: any = null;
  mlClassSummary: any = null;
  isLoadingClassMl: boolean = false;
  mlStudentFilter: string = 'ALL'; // 'ALL', 'ADMIS', 'RACHAT', 'RISK'

  selectedMlModel = 'COMPARE'; // 'DT', 'KNN', 'RF', 'COMPARE'
  knnK = 3;
  mlFeatures = {
    moyenneGenerale: 11.5,
    ectsNonValides: 6,
    statutEtudiant: 'nouveau',
    moyenneUe: 10.0,
    noteCc: 12.0,
    noteTp: 13.5,
    noteExam: 11.0
  };
  mlPredictionResult: any = null;
  mlComparisonResult: any = null;
  isLoadingMl = false;

  constructor(
    private apiService: ApiService, 
    public authService: AuthService,
    private router: Router,
    private route: ActivatedRoute
  ) {}

  ngOnInit() {
    if (this.authService.isAdmin()) {
      this.currentView = 'wizard';
    }

    this.route.queryParams.subscribe(params => {
      if (params['view']) {
        this.switchView(params['view']);
      }
    });

    this.loadMesClasses();
    this.loadHistory();
    this.loadMlClasses();
    this.runMlPrediction();
  }

  logout() {
    this.authService.logout();
    this.router.navigate(['/login']);
  }

  goToAdmin() {
    this.router.navigate(['/admin']);
  }

  toggleTheme() {
    this.theme = this.theme === 'light' ? 'dark' : 'light';
  }

  switchView(view: string) {
    this.currentView = view;
    if (view === 'mes-classes') {
      this.loadMesClasses();
    } else if (view === 'history' || view === 'dashboard') {
      this.loadHistory();
    } else if (view === 'ml-models') {
      this.loadMlClasses();
      this.runMlPrediction();
    }
  }

  loadMlClasses() {
    this.apiService.getClasses().subscribe({
      next: (classes) => {
        this.mlClasses = classes || [];
        if (this.mlClasses.length > 0 && !this.selectedMlClasseId) {
          this.selectedMlClasseId = this.mlClasses[0].id;
          this.runClassMlPrediction();
        }
      },
      error: (err) => console.error('Erreur chargement classes ML:', err)
    });
  }

  setMlMode(mode: 'CLASS' | 'SIMULATION') {
    this.mlMode = mode;
    if (mode === 'CLASS' && this.mlClasses.length === 0) {
      this.loadMlClasses();
    } else if (mode === 'CLASS' && this.selectedMlClasseId && !this.mlClassSummary) {
      this.runClassMlPrediction();
    } else if (mode === 'SIMULATION') {
      this.runMlPrediction();
    }
  }

  onSelectMlClass(classeId: any) {
    this.selectedMlClasseId = Number(classeId);
    this.runClassMlPrediction();
  }

  runClassMlPrediction() {
    if (!this.selectedMlClasseId) return;
    this.isLoadingClassMl = true;
    this.apiService.predictClassMl(this.selectedMlClasseId).subscribe({
      next: (summary) => {
        this.mlClassSummary = summary;
        this.isLoadingClassMl = false;
      },
      error: (err) => {
        console.error('Erreur prédiction classe ML:', err);
        this.isLoadingClassMl = false;
      }
    });
  }

  setMlStudentFilter(filter: string) {
    this.mlStudentFilter = filter;
  }

  getFilteredMlStudents(): any[] {
    if (!this.mlClassSummary || !this.mlClassSummary.studentPredictions) return [];
    if (this.mlStudentFilter === 'ALL') return this.mlClassSummary.studentPredictions;
    if (this.mlStudentFilter === 'ADMIS') {
      return this.mlClassSummary.studentPredictions.filter((s: any) => s.riskLevel === 'FAIBLE');
    }
    if (this.mlStudentFilter === 'RACHAT') {
      return this.mlClassSummary.studentPredictions.filter((s: any) => s.riskLevel === 'MOYEN');
    }
    if (this.mlStudentFilter === 'RISK') {
      return this.mlClassSummary.studentPredictions.filter((s: any) => s.riskLevel === 'ELEVE' || s.riskLevel === 'CRITIQUE');
    }
    return this.mlClassSummary.studentPredictions;
  }

  printClassMlReport() {
    window.print();
  }

  runMlPrediction() {
    this.isLoadingMl = true;
    if (this.selectedMlModel === 'DT') {
      this.apiService.predictDecisionTree(this.mlFeatures).subscribe({
        next: (res) => { this.mlPredictionResult = res; this.isLoadingMl = false; },
        error: () => this.isLoadingMl = false
      });
    } else if (this.selectedMlModel === 'KNN') {
      this.apiService.predictKnn(this.mlFeatures, this.knnK).subscribe({
        next: (res) => { this.mlPredictionResult = res; this.isLoadingMl = false; },
        error: () => this.isLoadingMl = false
      });
    } else if (this.selectedMlModel === 'RF') {
      this.apiService.predictRandomForest(this.mlFeatures).subscribe({
        next: (res) => { this.mlPredictionResult = res; this.isLoadingMl = false; },
        error: () => this.isLoadingMl = false
      });
    } else {
      this.apiService.compareAllMlModels(this.mlFeatures).subscribe({
        next: (res) => { this.mlComparisonResult = res; this.isLoadingMl = false; },
        error: () => this.isLoadingMl = false
      });
    }
  }

  loadMlPreset(type: string) {
    if (type === 'EXCELLENT') {
      this.mlFeatures = { moyenneGenerale: 16.5, ectsNonValides: 0, statutEtudiant: 'nouveau', moyenneUe: 16.0, noteCc: 17, noteTp: 16, noteExam: 16.5 };
    } else if (type === 'RACHAT_MG') {
      this.mlFeatures = { moyenneGenerale: 9.6, ectsNonValides: 8, statutEtudiant: 'nouveau', moyenneUe: 9.0, noteCc: 10, noteTp: 11, noteExam: 9.0 };
    } else if (type === 'RACHAT_UE') {
      this.mlFeatures = { moyenneGenerale: 9.2, ectsNonValides: 14, statutEtudiant: 'nouveau', moyenneUe: 8.5, noteCc: 10, noteTp: 10, noteExam: 8.5 };
    } else if (type === 'CONSEIL') {
      this.mlFeatures = { moyenneGenerale: 8.4, ectsNonValides: 18, statutEtudiant: 'nouveau', moyenneUe: 7.0, noteCc: 9, noteTp: 8.5, noteExam: 8.0 };
    } else if (type === 'AJOURNE') {
      this.mlFeatures = { moyenneGenerale: 5.2, ectsNonValides: 32, statutEtudiant: 'nouveau', moyenneUe: 4.5, noteCc: 6, noteTp: 5, noteExam: 5.0 };
    }
    this.runMlPrediction();
  }

  // Notes Entry State
  studentGradeRows: any[] = [];
  gradeSaveSuccessMessage = '';
  gradeSaveErrorMessage = '';
  isSavingNotes = false;

  // New Enseignant Features State
  enseignantStats: any = null;
  isLoadingStats = false;
  isExportingExcel = false;

  // --- Automatic Display for Enseignant (/api/mes-classes) ---
  loadMesClasses() {
    this.isLoadingMesClasses = true;
    this.apiService.getMesClasses().subscribe({
      next: (data: any) => {
        this.isLoadingMesClasses = false;
        this.mesClassesData = data;
        this.prepareGradeRows();
      },
      error: (err: any) => {
        this.isLoadingMesClasses = false;
        console.error('Erreur chargement mes classes', err);
      }
    });
  }

  onClasseChange(index: number) {
    this.selectedClasseIndex = index;
    this.prepareGradeRows();
  }

  prepareGradeRows() {
    const c = this.currentSelectedClasse;
    if (!c || !c.etudiants) {
      this.studentGradeRows = [];
      this.enseignantStats = null;
      return;
    }

    this.studentGradeRows = c.etudiants.map((e: any) => ({
      idEtudiant: e.idEtudiant,
      nom: e.nom,
      prenom: e.prenom,
      noteCc: null,
      noteTp: null,
      noteExam: null,
      remarque: ''
    }));

    // Load Class Stats
    this.loadEnseignantStats(c.classeId, c.matiere);

    // Fetch existing saved notes from DB
    this.apiService.getSavedNotes(c.classeId, c.matiere).subscribe({
      next: (savedNotes: any[]) => {
        if (savedNotes && savedNotes.length > 0) {
          const map = new Map<number, any>();
          savedNotes.forEach(n => map.set(n.etudiantId, n));

          this.studentGradeRows.forEach(row => {
            const saved = map.get(row.idEtudiant);
            if (saved) {
              if (saved.noteCc !== null) row.noteCc = saved.noteCc;
              if (saved.noteTp !== null) row.noteTp = saved.noteTp;
              if (saved.noteExam !== null) row.noteExam = saved.noteExam;
            }
          });
        }
      },
      error: (err) => console.error('Erreur récupération des notes enregistrées', err)
    });

    // Fetch student remarks
    if (c.matiere) {
      this.apiService.getRemarques(c.matiere).subscribe({
        next: (remarques: any[]) => {
          if (remarques && remarques.length > 0) {
            const map = new Map<number, string>();
            remarques.forEach(r => map.set(r.etudiantId, r.remarque));
            this.studentGradeRows.forEach(row => {
              if (map.has(row.idEtudiant)) {
                row.remarque = map.get(row.idEtudiant) || '';
              }
            });
          }
        },
        error: (err) => console.error('Erreur chargement remarques', err)
      });
    }
  }

  loadEnseignantStats(classeId: number, matiere: string) {
    this.isLoadingStats = true;
    this.apiService.getEnseignantStats(classeId, matiere).subscribe({
      next: (res) => {
        this.enseignantStats = res;
        this.isLoadingStats = false;
      },
      error: (err) => {
        console.error('Erreur chargement stats enseignant:', err);
        this.isLoadingStats = false;
      }
    });
  }

  onSaveGrades() {
    const c = this.currentSelectedClasse;
    if (!c) return;

    this.isSavingNotes = true;
    this.gradeSaveSuccessMessage = '';
    this.gradeSaveErrorMessage = '';

    const payload = {
      affectationId: c.affectationId,
      classeId: c.classeId,
      matiere: c.matiere,
      notes: this.studentGradeRows.map(row => ({
        etudiantId: row.idEtudiant,
        noteCc: row.noteCc !== null && row.noteCc !== '' ? parseFloat(row.noteCc) : null,
        noteTp: row.noteTp !== null && row.noteTp !== '' ? parseFloat(row.noteTp) : null,
        noteExam: row.noteExam !== null && row.noteExam !== '' ? parseFloat(row.noteExam) : null
      }))
    };

    this.apiService.saveNotes(payload).subscribe({
      next: (res: any) => {
        this.isSavingNotes = false;
        this.gradeSaveSuccessMessage = res.message || 'Notes enregistrées avec succès dans la base de données MySQL !';
        this.loadEnseignantStats(c.classeId, c.matiere);
      },
      error: (err: any) => {
        this.isSavingNotes = false;
        this.gradeSaveErrorMessage = err.error?.message || 'Erreur lors de l\'enregistrement des notes.';
      }
    });
  }

  onSaveStudentRemarque(row: any) {
    const c = this.currentSelectedClasse;
    if (!c || !row.idEtudiant) return;

    const payload = {
      etudiantId: row.idEtudiant,
      matiere: c.matiere,
      remarque: row.remarque
    };

    this.apiService.saveRemarque(payload).subscribe({
      next: () => {
        console.log('Remarque enregistrée pour étudiant #' + row.idEtudiant);
      },
      error: (err) => console.error('Erreur sauvegarde remarque', err)
    });
  }

  exportNotesExcel() {
    const c = this.currentSelectedClasse;
    if (!c) return;

    this.isExportingExcel = true;
    this.apiService.exportEnseignantNotes(c.classeId, c.matiere).subscribe({
      next: (blob: Blob) => {
        this.isExportingExcel = false;
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `Releve_Notes_${c.nomClasse}_${c.matiere}.xlsx`;
        a.click();
        window.URL.revokeObjectURL(url);
      },
      error: (err) => {
        this.isExportingExcel = false;
        console.error('Erreur export notes Excel:', err);
      }
    });
  }

  get currentSelectedClasse() {
    if (this.mesClassesData && this.mesClassesData.classesAffectees && this.mesClassesData.classesAffectees.length > 0) {
      return this.mesClassesData.classesAffectees[this.selectedClasseIndex] || this.mesClassesData.classesAffectees[0];
    }
    return null;
  }

  getPageTitle() {
    switch(this.currentView) {
      case 'mes-classes': return 'Mes Classes & Affectations';
      case 'dashboard': return 'Tableau de Bord';
      case 'wizard': return 'Assistant de Génération PV';
      case 'ml-models': return 'Intelligence Artificielle & Machine Learning (Arbre de Décision, KNN, Random Forest)';
      case 'rules': return 'Règles LMD';
      case 'history': return 'Historique des Délibérations';
      default: return 'Espace Enseignant';
    }
  }

  getPageSubtitle() {
    switch(this.currentView) {
      case 'mes-classes': return 'Affichage automatique de vos matières, classes et liste des étudiants rattachés';
      case 'dashboard': return 'Aperçu général et statistiques de délibération';
      case 'wizard': return 'Importez vos données et générez les PVs';
      case 'ml-models': return 'Simulation prédictive de délibération avec Arbre de Décision, K-Plus Proches Voisins (KNN) et Forêt Aléatoire (Random Forest)';
      case 'rules': return 'Paramètres du moteur de décision';
      case 'history': return 'Registre complet des générations passées';
      default: return '';
    }
  }

  loadHistory() {
    this.apiService.getHistory().subscribe({
      next: (res: any) => {
        this.allHistory = res;
        this.recentHistory = res.slice(0, 5);
        this.updateDashboardStats(res);
      },
      error: (err: any) => console.error('Erreur chargement historique', err)
    });
  }

  updateDashboardStats(history: any[]) {
    this.statTotalPv = history.length;
    const uniqueClasses = new Set(history.map(h => h.nomClasse));
    this.statTotalClasses = uniqueClasses.size;

    if (history.length > 0) {
      const sumSuccess = history.reduce((acc, h) => acc + h.tauxReussite, 0);
      this.statAvgSuccess = sumSuccess / history.length;
    } else {
      this.statAvgSuccess = 0;
    }
  }

  downloadTemplate() {
    window.location.href = this.apiService.downloadTemplate();
  }

  // --- Wizard Step 1: Upload ---
  onFileSelected(event: any) {
    const file = event.target.files[0];
    if (file) {
      this.selectedFile = file;
      this.uploadErrors = [];
      this.uploadWarnings = [];
    }
  }

  removeFile() {
    this.selectedFile = null;
    this.uploadErrors = [];
    this.uploadWarnings = [];
  }

  uploadAndAnalyze() {
    if (!this.selectedFile) return;

    this.isUploading = true;
    this.uploadErrors = [];
    this.uploadWarnings = [];

    this.apiService.uploadExcel(this.selectedFile, this.modeCalcul).subscribe({
      next: (res: any) => {
        this.isUploading = false;
        
        if (res.valid) {
          this.parsedClasses = res.classes;
          this.selectedClassIndex = 0;
          this.currentStep = 2;
          
          if (res.warnings && res.warnings.length > 0) {
            this.uploadWarnings = res.warnings;
          }
        } else {
          this.uploadErrors = res.errors && res.errors.length > 0 ? res.errors : ['Erreur lors de l\'analyse du fichier.'];
        }
      },
      error: (err: any) => {
        this.isUploading = false;
        console.error('Erreur upload Excel:', err);
        if (err.error && err.error.errors && err.error.errors.length > 0) {
          this.uploadErrors = err.error.errors;
        } else if (err.error && typeof err.error === 'string') {
          this.uploadErrors = [err.error];
        } else if (err.error && err.error.message) {
          this.uploadErrors = [err.error.message];
        } else {
          this.uploadErrors = ["Erreur lors du traitement du fichier Excel. Vérifiez le format des colonnes."];
        }
      }
    });
  }

  loadFromDatabase() {
    this.isUploading = true;
    this.uploadErrors = [];
    this.uploadWarnings = [];

    this.apiService.getClassesFromDb().subscribe({
      next: (classes: any[]) => {
        this.isUploading = false;
        if (classes && classes.length > 0) {
          this.parsedClasses = classes;
          this.selectedClassIndex = 0;
          this.downloadClassIndex = 0;
          this.currentStep = 2;
        } else {
          this.uploadErrors = ["Aucune classe avec des étudiants n'a été trouvée dans la base de données."];
        }
      },
      error: (err: any) => {
        this.isUploading = false;
        console.error(err);
        this.uploadErrors = [err.error?.message || "Erreur de chargement depuis la base de données."];
      }
    });
  }

  // --- Wizard Step 2: Preview ---
  get currentPreviewClass() {
    return this.parsedClasses[this.selectedClassIndex] || null;
  }

  updatePreviewStats() {
    // Helper method for template change listener
  }

  getDecisionBadgeHtml(decision: string): string {
    if (!decision) return '-';
    let cssClass = 'badge-blue';
    const lower = decision.toLowerCase();
    if (lower.includes('admis')) {
      cssClass = lower.includes('rachat') || lower.includes('ects') ? 'badge-cyan' : 'badge-green';
    } else if (lower.includes('conseil')) {
      cssClass = 'badge-amber';
    } else if (lower.includes('redouble') || lower.includes('exclu')) {
      cssClass = 'badge-red';
    }
    return `<span class="badge ${cssClass}">${decision}</span>`;
  }

  // --- Wizard Step 3: Jury ---
  addJuryMember() {
    this.sessionInfo.membresJury.push({ nom: '', fonction: '' });
  }

  removeJuryMember(index: number) {
    this.sessionInfo.membresJury.splice(index, 1);
  }

  // --- Wizard Step 4: Generate ---
  generateSingle() {
    const cls = this.parsedClasses[this.downloadClassIndex];
    if (!cls) return;

    const requestData = {
      classesData: [cls],
      sessionInfo: this.sessionInfo
    };

    this.apiService.generatePv(requestData).subscribe({
      next: (blob: any) => {
        if (blob.size === 0) {
          console.error('Le fichier généré est vide.');
          return;
        }
        this.triggerDownload(blob, `PV_Deliberation_${cls.nomClasse}.docx`);
      },
      error: (err: any) => console.error('Erreur génération', err)
    });
  }

  generateBatch() {
    if (!this.parsedClasses || this.parsedClasses.length === 0) return;

    const requestData = {
      classesData: this.parsedClasses,
      sessionInfo: this.sessionInfo
    };

    this.apiService.generateBatchPv(requestData).subscribe({
      next: (blob: any) => this.triggerDownload(blob, `PVs_Deliberation_Batch.zip`),
      error: (err: any) => console.error('Erreur génération batch', err)
    });
  }

  private triggerDownload(blob: Blob, filename: string) {
    const url = window.URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = filename;
    document.body.appendChild(a);
    a.click();
    window.URL.revokeObjectURL(url);
    document.body.removeChild(a);
  }
}
