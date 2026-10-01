import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../services/auth.service';
import { Router, RouterModule } from '@angular/router';
import { ApiService } from '../services/api.service';

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './profile.component.html',
  styleUrls: ['./profile.component.css']
})
export class ProfileComponent implements OnInit {
  user: any;
  myHistory: any[] = [];
  pwdForm: any = {
    oldPassword: '',
    newPassword: '',
    confirmPassword: ''
  };
  successMessage: string = '';
  errorMessage: string = '';
  email: string = '';
  savedEmail: string = '';
  emailMessage: string = '';
  emailError = false;

  constructor(
    public authService: AuthService,
    private apiService: ApiService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.user = this.authService.getCurrentUser();
    if (!this.user) {
      this.logout();
      return;
    }
    
    this.apiService.getMyHistory().subscribe({
      next: (res) => this.myHistory = res,
      error: (err) => console.error("Erreur de chargement de l'historique", err)
    });

    this.apiService.getMe().subscribe({
      next: (me) => this.email = this.savedEmail = me.email || '',
      error: (err) => console.error('Erreur de chargement du profil', err)
    });
  }

  saveEmail(): void {
    const email = this.email.trim();
    this.apiService.updateMyEmail(email).subscribe({
      next: (res) => {
        this.email = this.savedEmail = email;
        this.emailError = false;
        this.emailMessage = res.message;
      },
      error: (err) => {
        this.emailError = true;
        this.emailMessage = err.error?.message || "Erreur lors de l'enregistrement de l'e-mail.";
      }
    });
  }

  changePassword(): void {
    this.successMessage = '';
    this.errorMessage = '';

    if (this.pwdForm.newPassword !== this.pwdForm.confirmPassword) {
      this.errorMessage = "Les mots de passe ne correspondent pas.";
      return;
    }

    if (this.pwdForm.newPassword.length < 5) {
      this.errorMessage = "Le nouveau mot de passe est trop court.";
      return;
    }

    this.authService.changePassword({
      oldPassword: this.pwdForm.oldPassword,
      newPassword: this.pwdForm.newPassword
    }).subscribe({
      next: (res) => {
        this.successMessage = res.message;
        this.pwdForm = { oldPassword: '', newPassword: '', confirmPassword: '' };
      },
      error: (err) => {
        this.errorMessage = err.error?.message || "Erreur lors du changement de mot de passe.";
      }
    });
  }

  logout(): void {
    this.authService.logout();
    this.router.navigate(['/login']);
  }
}
