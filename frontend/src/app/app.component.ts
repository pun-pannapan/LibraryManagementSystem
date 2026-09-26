import { HttpClient } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrl: './app.component.css'
})
export class App {
  private readonly http = inject(HttpClient);
  protected readonly apiStatus = signal<'checking' | 'online' | 'offline'>('checking');

  constructor() {
    this.http.get<{ status: string }>('/api/v1/health').subscribe({
      next: () => this.apiStatus.set('online'),
      error: () => this.apiStatus.set('offline')
    });
  }
}
