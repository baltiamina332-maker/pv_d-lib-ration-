import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { AuthService } from '../services/auth.service';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './register.component.html',
  styleUrls: ['./register.component.css']
})
export class RegisterComponent {
  form: any = {
    username: '',
    email: '',
    password: ''
  };
  isSuccessful = false;
  errorMessage = '';

  constructor(private authService: AuthService) { }

  onSubmit(): void {
    this.authService.register(this.form).subscribe({
      next: data => {
        this.isSuccessful = true;
      },
      error: err => {
        this.errorMessage = err.error.message || 'Échec de l\'inscription.';
      }
    });
  }
}
