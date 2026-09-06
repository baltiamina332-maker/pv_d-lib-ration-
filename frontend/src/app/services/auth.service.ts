import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, tap } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private baseUrl = 'http://localhost:8081/api/auth';
  private adminUrl = 'http://localhost:8081/api/admin';

  private currentUserSubject = new BehaviorSubject<any>(null);
  public currentUser$ = this.currentUserSubject.asObservable();

  constructor(private http: HttpClient) {
    const user = localStorage.getItem('user');
    if (user) {
      this.currentUserSubject.next(JSON.parse(user));
    }
  }

  login(credentials: any): Observable<any> {
    return this.http.post(`${this.baseUrl}/login`, credentials).pipe(
      tap((res: any) => {
        if (res && res.token) {
          localStorage.setItem('user', JSON.stringify(res));
          this.currentUserSubject.next(res);
        }
      })
    );
  }

  register(user: any): Observable<any> {
    return this.http.post(`${this.baseUrl}/register`, user);
  }

  logout() {
    localStorage.removeItem('user');
    this.currentUserSubject.next(null);
  }

  getToken(): string | null {
    const user = this.currentUserSubject.value;
    return user ? user.token : null;
  }

  getCurrentUser(): any {
    return this.currentUserSubject.value;
  }

  isLoggedIn(): boolean {
    return !!this.getToken();
  }

  isAdmin(): boolean {
    const user = this.currentUserSubject.value;
    return user && user.role === 'ROLE_ADMIN';
  }

  // Admin APIs
  getUsers(): Observable<any[]> {
    return this.http.get<any[]>(`${this.adminUrl}/users`);
  }

  approveUser(id: number): Observable<any> {
    return this.http.post(`${this.adminUrl}/users/${id}/approve`, {});
  }
  
  rejectUser(id: number): Observable<any> {
    return this.http.post(`${this.adminUrl}/users/${id}/reject`, {});
  }

  changePassword(data: any): Observable<any> {
    return this.http.post(`${this.baseUrl}/change-password`, data);
  }
}
