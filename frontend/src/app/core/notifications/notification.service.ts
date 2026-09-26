import { Injectable, signal } from '@angular/core';

export type NotificationType = 'success' | 'warning' | 'error' | 'info';

export interface AppNotification {
  type: NotificationType;
  message: string;
  fading: boolean;
}

@Injectable({ providedIn: 'root' })
export class NotificationService {
  readonly current = signal<AppNotification | null>(null);
  private fadeTimer: ReturnType<typeof setTimeout> | null = null;
  private dismissTimer: ReturnType<typeof setTimeout> | null = null;

  show(type: NotificationType, message: string): void {
    this.clearTimers();
    this.current.set({ type, message, fading: false });
    this.fadeTimer = setTimeout(() => {
      this.current.update((notification) =>
        notification ? { ...notification, fading: true } : null,
      );
      this.fadeTimer = null;
    }, 2700);
    this.dismissTimer = setTimeout(() => {
      this.current.set(null);
      this.dismissTimer = null;
    }, 3000);
  }

  dismiss(): void {
    this.clearTimers();
    this.current.set(null);
  }

  private clearTimers(): void {
    if (this.fadeTimer !== null) {
      clearTimeout(this.fadeTimer);
      this.fadeTimer = null;
    }
    if (this.dismissTimer !== null) {
      clearTimeout(this.dismissTimer);
      this.dismissTimer = null;
    }
  }
}
