import { HttpClient } from '@angular/common/http';
import { Component, computed, inject, signal } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { apiEndpoints } from './core/config/api-endpoints';
import { AuthService } from './core/auth/auth.service';
import { NotificationService } from './core/notifications/notification.service';

@Component({
  selector: 'app-root',
  imports: [RouterLink, RouterLinkActive, RouterOutlet],
  templateUrl: './app.component.html',
  styleUrl: './app.component.css',
})
export class App {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  protected readonly auth = inject(AuthService);
  protected readonly notifications = inject(NotificationService);
  protected readonly apiStatus = signal<'checking' | 'online' | 'offline'>('checking');
  protected readonly menuOpen = signal(false);
  protected readonly isAdministrator = computed(() =>
    this.auth.currentRoles().includes('Administrator'),
  );
  protected readonly userDisplayName = computed(() => {
    const user = this.auth.currentUser();
    const name = [user?.firstName, user?.lastName].filter(Boolean).join(' ');
    return name || user?.email || '';
  });

  constructor() {
    this.http.get<{ status: string }>(apiEndpoints.health).subscribe({
      next: () => this.apiStatus.set('online'),
      error: () => this.apiStatus.set('offline'),
    });
  }

  protected toggleMenu(): void {
    this.menuOpen.update((open) => !open);
  }

  protected closeMenu(): void {
    this.menuOpen.set(false);
  }

  protected logout(): void {
    this.auth.logout();
    this.closeMenu();
    void this.router.navigateByUrl('/login');
  }
}
