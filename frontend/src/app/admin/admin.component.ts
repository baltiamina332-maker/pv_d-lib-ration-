import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule, Router, ActivatedRoute } from '@angular/router';
import { AuthService } from '../services/auth.service';
import { ApiService } from '../services/api.service';

@Component({
  selector: 'app-admin',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './admin.component.html',
  styleUrls: ['./admin.component.css']
})
export class AdminComponent implements OnInit {
  activeTab: string = 'users'; // 'users', 'affectations', 'classes', 'pv-generator'

  // Data lists
  users: any[] = [];
  classes: any[] = [];
  affectations: any[] = [];
  etudiants: any[] = [];

  // Form inputs
  newClasse = {
    nomClasse: '',
    niveau: '4ème Année',
    anneeUniversitaire: '2025-2026'
  };

  newAffectation = {
    enseignantId: null,
    classeId: null,
    matiere: '',
    anneeUniversitaire: '2025-2026'
  };

  assignEtudiantForm = {
    etudiantId: null,
    classeId: null
  };

  newEtudiantForm = {
    idEtudiant: null,
    nom: '',
    prenom: '',
    classeId: null
  };

  // Status messages
  successMessage: string = '';
  errorMessage: string = '';

  constructor(
    public authService: AuthService,
    private apiService: ApiService,
    private router: Router,
    private route: ActivatedRoute
  ) {}

  ngOnInit(): void {
    this.route.queryParams.subscribe(params => {
      if (params['tab']) {
        this.activeTab = params['tab'];
      }
    });
    this.loadAllData();
  }

  setTab(tab: string) {
    this.activeTab = tab;
    this.clearMessages();
  }

  getTabTitle() {
    switch (this.activeTab) {
      case 'users': return 'Gestion des Utilisateurs & Accès';
      case 'affectations': return 'Affectations Enseignants (Matières & Classes)';
      case 'classes': return 'Gestion des Classes & Étudiants';
      case 'pv-generator': return 'Générateur de Procès-Verbaux (PV)';
      default: return 'Panneau d\'Administration';
    }
  }

  getTabSubtitle() {
    switch (this.activeTab) {
      case 'users': return 'Approbation des comptes enseignants et gestion des rôles d\'accès';
      case 'affectations': return 'Définissez les relations Enseignant ➔ Matière ➔ Classe avant le début de l\'année';
      case 'classes': return 'Création des classes universitaires et association des étudiants';
      case 'pv-generator': return 'Accès aux outils d\'importation Excel et de délibération LMD';
      default: return 'Gestion globale de l\'établissement';
    }
  }

  clearMessages() {
    this.successMessage = '';
    this.errorMessage = '';
  }

  loadAllData() {
    this.loadUsers();
    this.loadClasses();
    this.loadAffectations();
    this.loadEtudiants();
  }

  loadUsers() {
    this.authService.getUsers().subscribe({
      next: (data) => this.users = data,
      error: (err) => console.error('Erreur chargement utilisateurs', err)
    });
  }

  loadClasses() {
    this.apiService.getClasses().subscribe({
      next: (data) => this.classes = data,
      error: (err) => console.error('Erreur chargement classes', err)
    });
  }

  loadAffectations() {
    this.apiService.getAffectations().subscribe({
      next: (data) => this.affectations = data,
      error: (err) => console.error('Erreur chargement affectations', err)
    });
  }

  loadEtudiants() {
    this.apiService.getEtudiants().subscribe({
      next: (data) => this.etudiants = data,
      error: (err) => console.error('Erreur chargement étudiants', err)
    });
  }

  // --- Actions Utilisateurs ---
  approve(id: number) {
    this.authService.approveUser(id).subscribe({
      next: () => {
        this.successMessage = 'Utilisateur approuvé avec succès.';
        this.loadUsers();
      },
      error: (err) => this.errorMessage = err.error?.message || 'Erreur lors de l\'approbation.'
    });
  }

  reject(id: number) {
    this.authService.rejectUser(id).subscribe({
      next: () => {
        this.successMessage = 'Accès utilisateur révoqué.';
        this.loadUsers();
      },
      error: (err) => this.errorMessage = err.error?.message || 'Erreur lors de la révocation.'
    });
  }

  changeRole(userId: number, role: string) {
    this.apiService.changeUserRole(userId, role).subscribe({
      next: () => {
        this.successMessage = 'Rôle mis à jour avec succès.';
        this.loadUsers();
      },
      error: (err) => this.errorMessage = err.error?.message || 'Erreur lors du changement de rôle.'
    });
  }

  editingClasseId: number | null = null;

  // --- Actions Classes ---
  onCreateClasse() {
    if (!this.newClasse.nomClasse) {
      this.errorMessage = 'Le nom de la classe est obligatoire.';
      return;
    }

    this.apiService.createClasse(this.newClasse).subscribe({
      next: () => {
        this.successMessage = `Classe "${this.newClasse.nomClasse}" créée avec succès.`;
        this.newClasse.nomClasse = '';
        this.loadClasses();
      },
      error: (err) => this.errorMessage = err.error?.message || 'Erreur lors de la création de la classe.'
    });
  }

  startEditClasse(c: any) {
    this.editingClasseId = c.id;
    this.newClasse = {
      nomClasse: c.nomClasse || '',
      niveau: c.niveau || '',
      anneeUniversitaire: c.anneeUniversitaire || '2025-2026'
    };
    this.clearMessages();
  }

  onUpdateClasse() {
    if (!this.editingClasseId || !this.newClasse.nomClasse) {
      this.errorMessage = 'Le nom de la classe est obligatoire.';
      return;
    }

    this.apiService.updateClasse(this.editingClasseId, this.newClasse).subscribe({
      next: () => {
        this.successMessage = 'Classe mise à jour avec succès !';
        this.cancelEditClasse();
        this.loadClasses();
      },
      error: (err) => this.errorMessage = err.error?.message || 'Erreur de modification.'
    });
  }

  cancelEditClasse() {
    this.editingClasseId = null;
    this.newClasse = { nomClasse: '', niveau: '4ème Année', anneeUniversitaire: '2025-2026' };
  }

  onDeleteClasse(id: number) {
    if (!confirm('Voulez-vous vraiment supprimer cette classe ?')) return;
    this.apiService.deleteClasse(id).subscribe({
      next: () => {
        this.successMessage = 'Classe supprimée.';
        this.loadClasses();
      },
      error: (err) => this.errorMessage = err.error?.message || 'Erreur de suppression.'
    });
  }

  editingAffectationId: number | null = null;

  // --- Actions Affectations (Enseignant -> Matière -> Classe) ---
  onCreateAffectation() {
    if (!this.newAffectation.enseignantId || !this.newAffectation.classeId || !this.newAffectation.matiere) {
      this.errorMessage = 'Veuillez remplir tous les champs de l\'affectation (Enseignant, Classe et Matière).';
      return;
    }

    this.apiService.createAffectation(this.newAffectation).subscribe({
      next: () => {
        this.successMessage = 'Affectation créée avec succès ! L\'enseignant verra cette classe sur son tableau de bord.';
        this.newAffectation.matiere = '';
        this.loadAffectations();
      },
      error: (err) => this.errorMessage = err.error?.message || 'Erreur lors de l\'affectation.'
    });
  }

  startEditAffectation(aff: any) {
    this.editingAffectationId = aff.id;
    this.newAffectation = {
      enseignantId: aff.enseignant ? aff.enseignant.id : null,
      classeId: aff.classe ? aff.classe.id : null,
      matiere: aff.matiere || '',
      anneeUniversitaire: aff.anneeUniversitaire || '2025-2026'
    };
    this.clearMessages();
  }

  onUpdateAffectation() {
    if (!this.editingAffectationId || !this.newAffectation.enseignantId || !this.newAffectation.classeId || !this.newAffectation.matiere) {
      this.errorMessage = 'Veuillez remplir tous les champs obligatoires pour la modification.';
      return;
    }

    this.apiService.updateAffectation(this.editingAffectationId, this.newAffectation).subscribe({
      next: () => {
        this.successMessage = 'Affectation mise à jour avec succès !';
        this.cancelEditAffectation();
        this.loadAffectations();
      },
      error: (err) => this.errorMessage = err.error?.message || 'Erreur lors de la modification.'
    });
  }

  cancelEditAffectation() {
    this.editingAffectationId = null;
    this.newAffectation = {
      enseignantId: null,
      classeId: null,
      matiere: '',
      anneeUniversitaire: '2025-2026'
    };
  }

  // --- Sorting & Filtering for Registre des Affectations ---
  affectationSortColumn: string = 'matiere'; // 'enseignant', 'matiere', 'classe', 'annee'
  affectationSortDirection: 'asc' | 'desc' = 'asc';
  affectationFilterClasse: string = 'ALL';
  affectationSearchMatiere: string = '';

  toggleAffectationSort(column: string) {
    if (this.affectationSortColumn === column) {
      this.affectationSortDirection = this.affectationSortDirection === 'asc' ? 'desc' : 'asc';
    } else {
      this.affectationSortColumn = column;
      this.affectationSortDirection = 'asc';
    }
  }

  get filteredAndSortedAffectations(): any[] {
    let list = [...this.affectations];

    // Filter by Classe
    if (this.affectationFilterClasse !== 'ALL') {
      list = list.filter(a => a.classe && String(a.classe.id) === String(this.affectationFilterClasse));
    }

    // Filter by Matiere search
    if (this.affectationSearchMatiere && this.affectationSearchMatiere.trim() !== '') {
      const q = this.affectationSearchMatiere.trim().toLowerCase();
      list = list.filter(a => a.matiere && a.matiere.toLowerCase().includes(q));
    }

    // Sort
    list.sort((a, b) => {
      let valA = '';
      let valB = '';

      switch (this.affectationSortColumn) {
        case 'enseignant':
          valA = a.enseignant?.username || '';
          valB = b.enseignant?.username || '';
          break;
        case 'matiere':
          valA = a.matiere || '';
          valB = b.matiere || '';
          break;
        case 'classe':
          valA = a.classe?.nomClasse || '';
          valB = b.classe?.nomClasse || '';
          break;
        case 'annee':
          valA = a.anneeUniversitaire || '';
          valB = b.anneeUniversitaire || '';
          break;
        default:
          valA = a.matiere || '';
          valB = b.matiere || '';
      }

      const res = valA.localeCompare(valB, undefined, { sensitivity: 'base', numeric: true });
      return this.affectationSortDirection === 'asc' ? res : -res;
    });

    return list;
  }

  onDeleteAffectation(id: number) {
    if (!confirm('Voulez-vous supprimer cette affectation ?')) return;
    this.apiService.deleteAffectation(id).subscribe({
      next: () => {
        this.successMessage = 'Affectation supprimée.';
        this.loadAffectations();
      },
      error: (err) => this.errorMessage = err.error?.message || 'Erreur de suppression.'
    });
  }

  editingEtudiantId: number | null = null;

  // --- Actions Étudiants ---
  onCreateEtudiant() {
    if (!this.newEtudiantForm.nom || !this.newEtudiantForm.prenom) {
      this.errorMessage = 'Le Nom et le Prénom sont obligatoires pour créer un étudiant.';
      return;
    }

    this.apiService.createEtudiant(this.newEtudiantForm).subscribe({
      next: () => {
        this.successMessage = `Étudiant "${this.newEtudiantForm.nom} ${this.newEtudiantForm.prenom}" créé et inscrit avec succès !`;
        this.newEtudiantForm = { idEtudiant: null, nom: '', prenom: '', classeId: null };
        this.loadEtudiants();
      },
      error: (err) => this.errorMessage = err.error?.message || 'Erreur lors de la création de l\'étudiant.'
    });
  }

  startEditEtudiant(etu: any) {
    this.editingEtudiantId = etu.idEtudiant;
    this.newEtudiantForm = {
      idEtudiant: etu.idEtudiant,
      nom: etu.nom || '',
      prenom: etu.prenom || '',
      classeId: etu.classe ? etu.classe.id : null
    };
    this.clearMessages();
  }

  onUpdateEtudiant() {
    if (!this.editingEtudiantId || !this.newEtudiantForm.nom || !this.newEtudiantForm.prenom) {
      this.errorMessage = 'Le nom et le prénom de l\'étudiant sont obligatoires.';
      return;
    }

    this.apiService.updateEtudiant(this.editingEtudiantId, this.newEtudiantForm).subscribe({
      next: () => {
        this.successMessage = 'Étudiant mis à jour avec succès !';
        this.cancelEditEtudiant();
        this.loadEtudiants();
      },
      error: (err) => this.errorMessage = err.error?.message || 'Erreur lors de la mise à jour de l\'étudiant.'
    });
  }

  cancelEditEtudiant() {
    this.editingEtudiantId = null;
    this.newEtudiantForm = { idEtudiant: null, nom: '', prenom: '', classeId: null };
  }

  onDeleteEtudiant(id: number) {
    if (!confirm('Voulez-vous supprimer cet étudiant ?')) return;
    this.apiService.deleteEtudiant(id).subscribe({
      next: () => {
        this.successMessage = 'Étudiant supprimé.';
        this.loadEtudiants();
      },
      error: (err) => this.errorMessage = err.error?.message || 'Erreur de suppression.'
    });
  }

  onAssignEtudiant() {
    if (!this.assignEtudiantForm.etudiantId || !this.assignEtudiantForm.classeId) {
      this.errorMessage = 'Veuillez sélectionner un étudiant et une classe.';
      return;
    }

    this.apiService.assignEtudiantClasse(this.assignEtudiantForm.etudiantId, this.assignEtudiantForm.classeId).subscribe({
      next: () => {
        this.successMessage = 'Étudiant affecté à la classe avec succès.';
        this.loadEtudiants();
      },
      error: (err) => this.errorMessage = err.error?.message || 'Erreur lors de l\'affectation de l\'étudiant.'
    });
  }

  // --- Per-Class Grouping & Filtering ---
  selectedClassFilter: any = 'ALL';

  setSelectedClassFilter(filter: any) {
    this.selectedClassFilter = filter;
  }

  getStudentsForClass(classeId: number): any[] {
    return this.etudiants.filter(e => e.classe && e.classe.id === classeId);
  }

  getUnassignedStudents(): any[] {
    return this.etudiants.filter(e => !e.classe || !e.classe.id);
  }

  get displayedClasses(): any[] {
    if (this.selectedClassFilter === 'ALL') {
      return this.classes;
    } else if (this.selectedClassFilter === 'UNASSIGNED') {
      return [];
    } else {
      return this.classes.filter(c => c.id === this.selectedClassFilter);
    }
  }

  logout() {
    this.authService.logout();
    this.router.navigate(['/login']);
  }
}
