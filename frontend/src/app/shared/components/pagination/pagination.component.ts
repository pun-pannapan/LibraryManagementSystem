import { Component, input, output } from '@angular/core';

let nextPaginationId = 0;

@Component({
  selector: 'app-pagination',
  standalone: true,
  template: `
    @if (totalPages() > 1) {
      <nav
        class="d-flex flex-wrap align-items-center justify-content-between gap-3 mt-3"
        [attr.aria-label]="label()"
      >
        <span class="small text-secondary">{{
          summary() ?? 'Page ' + page() + ' of ' + totalPages()
        }}</span>
        <div class="d-flex align-items-center gap-3">
          <div class="d-flex align-items-center gap-2">
            <label class="small text-secondary mb-0" [for]="pageInputId">Go to page</label>
            <input
              #pageInput
              class="form-control form-control-sm"
              style="width: 5rem"
              type="number"
              min="1"
              [max]="totalPages()"
              [value]="page()"
              [id]="pageInputId"
              [attr.aria-label]="'Go to page in ' + label()"
              (keyup.enter)="goToPage($any($event.target).value)"
            />
            <button
              class="btn btn-sm btn-outline-primary"
              type="button"
              (click)="goToPage(pageInput.value)"
            >
              Go
            </button>
          </div>
          <div class="btn-group" role="group" [attr.aria-label]="label() + ' pagination controls'">
            <button
              class="btn btn-outline-secondary"
              type="button"
              aria-label="Previous page"
              [disabled]="page() <= 1"
              (click)="pageChange.emit(page() - 1)"
            >
              Previous
            </button>
            <button
              class="btn btn-outline-secondary"
              type="button"
              aria-label="Next page"
              [disabled]="page() >= totalPages()"
              (click)="pageChange.emit(page() + 1)"
            >
              Next
            </button>
          </div>
        </div>
      </nav>
    }
  `,
})
export class PaginationComponent {
  protected readonly pageInputId = `pagination-page-${nextPaginationId++}`;
  readonly page = input.required<number>();
  readonly totalPages = input.required<number>();
  readonly label = input.required<string>();
  readonly summary = input<string | null>(null);
  readonly pageChange = output<number>();

  protected goToPage(value: string): void {
    const target = Number(value);
    if (
      Number.isInteger(target) &&
      target >= 1 &&
      target <= this.totalPages() &&
      target !== this.page()
    ) {
      this.pageChange.emit(target);
    }
  }
}
