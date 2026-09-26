import { computed, DestroyRef, inject, Injectable, signal } from '@angular/core';
import { Observable, tap } from 'rxjs';
import { AuthResponse, LoginRequest, UserDto } from '../../shared/models/auth.models';
import { AuthApiService } from './auth-api.service';

const AUTH_STORAGE_KEY = 'library-management.auth';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly api = inject(AuthApiService);
  private readonly sessionState = signal<AuthResponse | null>(this.restoreSession());
  private expiryTimer: ReturnType<typeof setTimeout> | undefined;

  readonly currentUser = computed<UserDto | null>(() => this.sessionState()?.user ?? null);
  readonly currentRoles = computed<string[]>(() => this.sessionState()?.user.roles ?? []);
  readonly isAuthenticated = computed(() => this.sessionState() !== null);

  constructor() {
    inject(DestroyRef).onDestroy(() => {
      if (this.expiryTimer) clearTimeout(this.expiryTimer);
    });
    this.scheduleExpiry(this.sessionState());
  }

  login(request: LoginRequest): Observable<AuthResponse> {
    return this.api.login(request).pipe(tap((session) => this.saveSession(session)));
  }

  logout(): void {
    this.clearSession();
  }

  getToken(): string | null {
    const session = this.getValidSession();
    return session?.token ?? null;
  }

  hasValidSession(): boolean {
    return this.getValidSession() !== null;
  }

  hasRole(role: string): boolean {
    return this.getValidSession()?.user.roles.includes(role) ?? false;
  }

  private saveSession(session: AuthResponse): void {
    if (!this.isAuthResponse(session) || !this.isFutureDate(session.expiresAtUtc)) {
      this.clearSession();
      throw new Error('The server returned an invalid or expired authentication session.');
    }

    this.sessionState.set(session);
    this.scheduleExpiry(session);

    try {
      sessionStorage.setItem(AUTH_STORAGE_KEY, JSON.stringify(session));
    } catch {
      // Keep the session in memory if browser storage is unavailable.
    }
  }

  private restoreSession(): AuthResponse | null {
    try {
      const stored = sessionStorage.getItem(AUTH_STORAGE_KEY);
      if (!stored) return null;

      const session: unknown = JSON.parse(stored);
      if (this.isAuthResponse(session) && this.isFutureDate(session.expiresAtUtc)) {
        return session;
      }
    } catch {
      // Invalid JSON or unavailable storage is treated as a signed-out session.
    }

    this.removeStoredSession();
    return null;
  }

  private getValidSession(): AuthResponse | null {
    const session = this.sessionState();
    if (session && !this.isFutureDate(session.expiresAtUtc)) {
      this.clearSession();
      return null;
    }

    return session;
  }

  private clearSession(): void {
    if (this.expiryTimer) clearTimeout(this.expiryTimer);
    this.expiryTimer = undefined;
    this.sessionState.set(null);
    this.removeStoredSession();
  }

  private removeStoredSession(): void {
    try {
      sessionStorage.removeItem(AUTH_STORAGE_KEY);
    } catch {
      // Storage may be disabled by the browser.
    }
  }

  private scheduleExpiry(session: AuthResponse | null): void {
    if (this.expiryTimer) clearTimeout(this.expiryTimer);
    this.expiryTimer = undefined;

    if (!session) return;

    const delay = Date.parse(session.expiresAtUtc) - Date.now();
    if (delay <= 0) {
      this.clearSession();
      return;
    }

    this.expiryTimer = setTimeout(() => this.clearSession(), delay);
  }

  private isFutureDate(value: string): boolean {
    const timestamp = Date.parse(value);
    return Number.isFinite(timestamp) && timestamp > Date.now();
  }

  private isAuthResponse(value: unknown): value is AuthResponse {
    if (typeof value !== 'object' || value === null) return false;

    const candidate = value as Partial<AuthResponse>;
    return (
      typeof candidate.token === 'string' &&
      candidate.token.trim().length > 0 &&
      typeof candidate.expiresAtUtc === 'string' &&
      typeof candidate.user === 'object' &&
      candidate.user !== null &&
      typeof candidate.user.id === 'string' &&
      typeof candidate.user.email === 'string' &&
      (candidate.user.firstName === null || typeof candidate.user.firstName === 'string') &&
      (candidate.user.lastName === null || typeof candidate.user.lastName === 'string') &&
      Array.isArray(candidate.user.roles) &&
      candidate.user.roles.every((role) => typeof role === 'string')
    );
  }
}
