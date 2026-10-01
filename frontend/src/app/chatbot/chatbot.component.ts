import { Component, ElementRef, OnInit, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../services/api.service';
import { AuthService } from '../services/auth.service';

interface ChatMessage {
  role: 'user' | 'assistant';
  content: string;
  html: string;
  mode?: string;
}

@Component({
  selector: 'app-chatbot',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './chatbot.component.html',
  styleUrls: ['./chatbot.component.css']
})
export class ChatbotComponent implements OnInit {
  @ViewChild('scrollArea') scrollArea?: ElementRef<HTMLDivElement>;

  isOpen = false;
  isSending = false;
  input = '';
  mode: 'claude' | 'local' | '' = '';
  messages: ChatMessage[] = [];

  suggestionsAdmin = [
    'Résumé des résultats par classe',
    'Quels étudiants sont à risque en 4 SAE ?',
    'Comment envoyer un PV par e-mail ?',
    'Explique les règles de rachat'
  ];
  suggestionsEnseignant = [
    'Combien d\'étudiants dans mes classes ?',
    'Comment saisir les notes ?',
    'Explique les règles de rachat'
  ];

  constructor(private api: ApiService, public auth: AuthService) {}

  ngOnInit(): void {
    this.reset();
  }

  get suggestions(): string[] {
    return this.auth.isAdmin() ? this.suggestionsAdmin : this.suggestionsEnseignant;
  }

  toggle(): void {
    this.isOpen = !this.isOpen;
    if (this.isOpen && !this.mode) {
      this.api.getChatbotStatus().subscribe({
        next: (res) => this.mode = res.mode,
        error: () => this.mode = ''
      });
    }
    this.scrollToBottom();
  }

  reset(): void {
    const name = this.auth.getCurrentUser()?.username || '';
    const welcome = `Bonjour ${name} ! Je suis l'assistant PV-Delib. Posez-moi une question sur vos classes, les résultats, `
      + `les règles LMD ou l'utilisation de la plateforme.`;
    this.messages = [{ role: 'assistant', content: welcome, html: this.format(welcome) }];
  }

  send(text?: string): void {
    const message = (text ?? this.input).trim();
    if (!message || this.isSending) return;

    // Historique envoyé au serveur : la conversation sans le message d'accueil
    const history = this.messages.slice(1).map(m => ({ role: m.role, content: m.content }));
    this.messages.push({ role: 'user', content: message, html: this.format(message) });
    this.input = '';
    this.isSending = true;
    this.scrollToBottom();

    this.api.sendChatMessage(message, history).subscribe({
      next: (res) => {
        this.mode = res.mode;
        this.messages.push({ role: 'assistant', content: res.reply, html: this.format(res.reply), mode: res.mode });
        this.isSending = false;
        this.scrollToBottom();
      },
      error: (err) => {
        const msg = err.status === 401 || err.status === 403
          ? 'Votre session a expiré, veuillez vous reconnecter.'
          : (err.error?.message || 'Le serveur ne répond pas. Vérifiez que le backend est démarré.');
        this.messages.push({ role: 'assistant', content: msg, html: this.format(msg) });
        this.isSending = false;
        this.scrollToBottom();
      }
    });
  }

  onKeydown(event: KeyboardEvent): void {
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault();
      this.send();
    }
  }

  /** Texte -> HTML sûr : échappement, **gras** et retours à la ligne. */
  private format(text: string): string {
    const escaped = text
      .replace(/&/g, '&amp;')
      .replace(/</g, '&lt;')
      .replace(/>/g, '&gt;')
      .replace(/"/g, '&quot;');
    return escaped
      .replace(/\*\*(.+?)\*\*/g, '<strong>$1</strong>')
      .replace(/\n/g, '<br>');
  }

  private scrollToBottom(): void {
    setTimeout(() => {
      const el = this.scrollArea?.nativeElement;
      if (el) el.scrollTop = el.scrollHeight;
    });
  }
}
