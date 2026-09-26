import { Component, input } from '@angular/core';

@Component({
  selector: 'app-validation-message',
  standalone: true,
  template: `@if (message(); as text) {
    <div [id]="id()" class="invalid-feedback d-block">{{ text }}</div>
  }`,
})
export class ValidationMessageComponent {
  readonly id = input.required<string>();
  readonly message = input<string | null>(null);
}
