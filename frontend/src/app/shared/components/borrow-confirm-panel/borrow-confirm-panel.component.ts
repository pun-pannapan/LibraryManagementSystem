import { Component, input, output } from '@angular/core';

@Component({
  selector: 'app-borrow-confirm-panel',
  standalone: true,
  template: `
    @if (bookTitle(); as title) {
      <section class="border rounded-3 bg-light p-3 mb-3" role="group" aria-labelledby="borrow-confirm-title">
        <h2 id="borrow-confirm-title" class="h6">Borrow “{{ title }}”?</h2>
        <p class="small text-secondary">The library will record this borrowing under your account.</p>
        @if (errorMessage(); as message) {
          <div class="alert alert-danger py-2" role="alert">{{ message }}</div>
        }
        <div class="d-flex gap-2">
          <button class="btn btn-primary btn-sm" type="button" [disabled]="busy()" (click)="confirm.emit()">
            @if (busy()) {
              <span class="spinner-border spinner-border-sm me-1" aria-hidden="true"></span>
              <span>Borrowing…</span>
            } @else {
              <span>Confirm borrow</span>
            }
          </button>
          <button class="btn btn-outline-secondary btn-sm" type="button" [disabled]="busy()" (click)="cancel.emit()">Cancel</button>
        </div>
      </section>
    }
  `,
})
export class BorrowConfirmPanelComponent {
  readonly bookTitle = input<string | null>(null);
  readonly busy = input(false);
  readonly errorMessage = input<string | null>(null);
  readonly confirm = output<void>();
  readonly cancel = output<void>();
}
