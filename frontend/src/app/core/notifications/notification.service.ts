import { Injectable, signal } from '@angular/core';

export type NotificationType = 'success' | 'warning' | 'error' | 'info';

export interface AppNotification {
  type: NotificationType;
  message: string;
}

@Injectable({ providedIn: 'root' })
export class NotificationService {
  readonly current = signal<AppNotification | null>(null);

  show(type: NotificationType, message: string): void {
    this.current.set({ type, message });
  }

  dismiss(): void {
    this.current.set(null);
  }
}
