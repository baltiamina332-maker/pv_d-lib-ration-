import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class ApiService {
  private baseUrl = 'http://localhost:8081/api';

  constructor(private http: HttpClient) { }

  // Excel & PV Generator
  uploadExcel(file: File, modeCalcul: string): Observable<any> {
    const formData = new FormData();
    formData.append('file', file);
    formData.append('modeCalcul', modeCalcul);
    return this.http.post(`${this.baseUrl}/excel/upload`, formData);
  }

  getClassesFromDb(): Observable<any[]> {
    return this.http.get<any[]>(`${this.baseUrl}/pv/classes-from-db`);
  }

  generatePv(data: any): Observable<Blob> {
    return this.http.post(`${this.baseUrl}/pv/generate`, data, {
      responseType: 'blob'
    });
  }

  generateBatchPv(data: any): Observable<Blob> {
    return this.http.post(`${this.baseUrl}/pv/generate-batch`, data, {
      responseType: 'blob'
    });
  }

  getHistory(): Observable<any> {
    return this.http.get(`${this.baseUrl}/pv/history`);
  }

  getMyHistory(): Observable<any> {
    return this.http.get(`${this.baseUrl}/pv/history/my`);
  }

  downloadTemplate(): string {
    return `${this.baseUrl}/excel/template`;
  }

  // --- Enseignant Dashboard ---
  getMesClasses(): Observable<any> {
    return this.http.get(`${this.baseUrl}/mes-classes`);
  }

  saveNotes(data: any): Observable<any> {
    return this.http.post(`${this.baseUrl}/saisir-notes`, data);
  }

  getSavedNotes(classeId: number, matiere: string): Observable<any> {
    return this.http.get(`${this.baseUrl}/mes-classes/notes?classeId=${classeId}&matiere=${encodeURIComponent(matiere)}`);
  }

  // --- Admin Management APIs ---
  getClasses(): Observable<any[]> {
    return this.http.get<any[]>(`${this.baseUrl}/admin/classes`);
  }

  createClasse(data: any): Observable<any> {
    return this.http.post(`${this.baseUrl}/admin/classes`, data);
  }

  updateClasse(id: number, data: any): Observable<any> {
    return this.http.put(`${this.baseUrl}/admin/classes/${id}`, data);
  }

  deleteClasse(id: number): Observable<any> {
    return this.http.delete(`${this.baseUrl}/admin/classes/${id}`);
  }

  getAffectations(): Observable<any[]> {
    return this.http.get<any[]>(`${this.baseUrl}/admin/affectations`);
  }

  createAffectation(data: any): Observable<any> {
    return this.http.post(`${this.baseUrl}/admin/affectations`, data);
  }

  updateAffectation(id: number, data: any): Observable<any> {
    return this.http.put(`${this.baseUrl}/admin/affectations/${id}`, data);
  }

  deleteAffectation(id: number): Observable<any> {
    return this.http.delete(`${this.baseUrl}/admin/affectations/${id}`);
  }

  getEtudiants(): Observable<any[]> {
    return this.http.get<any[]>(`${this.baseUrl}/admin/etudiants`);
  }

  createEtudiant(data: any): Observable<any> {
    return this.http.post(`${this.baseUrl}/admin/etudiants`, data);
  }

  updateEtudiant(id: number, data: any): Observable<any> {
    return this.http.put(`${this.baseUrl}/admin/etudiants/${id}`, data);
  }

  deleteEtudiant(id: number): Observable<any> {
    return this.http.delete(`${this.baseUrl}/admin/etudiants/${id}`);
  }

  assignEtudiantClasse(etudiantId: number, classeId: number): Observable<any> {
    return this.http.post(`${this.baseUrl}/admin/etudiants/assign-classe`, { etudiantId, classeId });
  }

  changeUserRole(userId: number, role: string): Observable<any> {
    return this.http.post(`${this.baseUrl}/admin/users/${userId}/role`, { role });
  }

  // --- Machine Learning APIs (Decision Tree, KNN, Random Forest) ---
  predictDecisionTree(features: any): Observable<any> {
    return this.http.post(`${this.baseUrl}/ml/predict/decision-tree`, features);
  }

  predictKnn(features: any, k: number = 3): Observable<any> {
    return this.http.post(`${this.baseUrl}/ml/predict/knn?k=${k}`, features);
  }

  predictRandomForest(features: any): Observable<any> {
    return this.http.post(`${this.baseUrl}/ml/predict/random-forest`, features);
  }

  compareAllMlModels(features: any): Observable<any> {
    return this.http.post(`${this.baseUrl}/ml/compare-all`, features);
  }

  predictClassMl(classeId: number): Observable<any> {
    return this.http.get(`${this.baseUrl}/ml/predict/class/${classeId}`);
  }

  predictBatchMl(classeName: string, students: any[]): Observable<any> {
    return this.http.post(`${this.baseUrl}/ml/predict/batch?classeName=${encodeURIComponent(classeName)}`, students);
  }

  // --- New Enseignant APIs (Stats, Excel Export, Remarques) ---
  getEnseignantStats(classeId: number, matiere?: string): Observable<any> {
    let url = `${this.baseUrl}/enseignant/stats/${classeId}`;
    if (matiere) url += `?matiere=${encodeURIComponent(matiere)}`;
    return this.http.get(url);
  }

  exportEnseignantNotes(classeId: number, matiere?: string): Observable<Blob> {
    let url = `${this.baseUrl}/enseignant/export-notes/${classeId}`;
    if (matiere) url += `?matiere=${encodeURIComponent(matiere)}`;
    return this.http.get(url, { responseType: 'blob' });
  }

  saveRemarque(payload: any): Observable<any> {
    return this.http.post(`${this.baseUrl}/enseignant/remarque`, payload);
  }

  getRemarques(matiere: string): Observable<any[]> {
    return this.http.get<any[]>(`${this.baseUrl}/enseignant/remarques?matiere=${encodeURIComponent(matiere)}`);
  }
}
