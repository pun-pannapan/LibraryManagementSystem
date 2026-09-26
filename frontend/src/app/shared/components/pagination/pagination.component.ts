import { Component, input, output } from '@angular/core';

@Component({
  selector: 'app-pagination',
  standalone: true,
  template: `
    @if (totalPages() > 1) {
      <nav class="d-flex flex-wrap align-items-center justify-content-between gap-3 mt-3" [attr.aria-label]="label()">
        <span class="small text-secondary">{{ summary() ?? ('Page ' + page() + ' of ' + totalPages()) }}</span>
        <div class="btn-group" role="group" [attr.aria-label]="label() + ' pagination controls'">
          <button class="btn btn-outline-secondary" type="button" aria-label="Previous page" [disabled]="page() <= 1" (click)="pageChange.emit(page() - 1)">Previous</button>
          <button class="btn btn-outline-secondary" type="button" aria-label="Next page" [disabled]="page() >= totalPages()" (click)="pageChange.emit(page() + 1)">Next</button>
        </div>
      </nav>
    }
  `,
})
export class PaginationComponent {
  readonly page = input.required<number>();
  readonly totalPages = input.required<number>();
  readonly label = input.required<string>();
  readonly summary = input<string | null>(null);
  readonly pageChange = output<number>();
}
