import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterOutlet } from '@angular/router';
import { ChatbotComponent } from './chatbot/chatbot.component';
import { AuthService } from './services/auth.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, RouterOutlet, ChatbotComponent],
  template: `
    <router-outlet></router-outlet>
    <app-chatbot *ngIf="auth.isLoggedIn()"></app-chatbot>
  `
})
export class AppComponent {
  constructor(public auth: AuthService) {}
}
