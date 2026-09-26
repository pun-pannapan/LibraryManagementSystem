import { HttpErrorResponse } from '@angular/common/http';
import { Injectable } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class ApiErrorService {
  messageFor(error: unknown, fallback = 'The request could not be completed. Please try again.'): string {
    if (!(error instanceof HttpErrorResponse)) return fallback;

    switch (error.status) {
      case 0:
        return 'Unable to connect to the library service. Check your connection and try again.';
      case 401:
        return 'Your session has expired. Please sign in again.';
      case 403:
        return 'You do not have permission to perform this action.';
      case 404:
        return 'The requested item could not be found.';
      case 409:
        return this.safeApiMessage(error.error) ?? 'This request conflicts with the current data.';
      case 500:
      default:
        return error.status >= 500
          ? 'An unexpected error occurred. Please try again.'
          : this.validationMessage(error.error) ?? fallback;
    }
  }

  private safeApiMessage(body: unknown): string | null {
    if (typeof body !== 'object' || body === null) return null;

    const candidate = body as { message?: unknown; detail?: unknown };
    if (typeof candidate.message === 'string' && candidate.message.trim()) return candidate.message;
    if (typeof candidate.detail === 'string' && candidate.detail.trim()) return candidate.detail;
    return null;
  }

  private validationMessage(body: unknown): string | null {
    if (typeof body !== 'object' || body === null) return null;

    const errors = (body as { errors?: unknown }).errors;
    if (typeof errors !== 'object' || errors === null) return null;

    const firstError = Object.values(errors).flat().find((value): value is string => typeof value === 'string');
    return firstError ?? null;
  }
}
