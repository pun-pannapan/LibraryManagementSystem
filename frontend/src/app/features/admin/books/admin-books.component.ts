import { Component, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { ActivatedRoute, ParamMap, Router, RouterLink } from '@angular/router';
import {
  catchError,
  combineLatest,
  finalize,
  map,
  of,
  startWith,
  Subject,
  switchMap,
  tap,
} from 'rxjs';
import { ApiErrorService } from '../../../core/http/api-error.service';
import { NotificationService } from '../../../core/notifications/notification.service';
import { PagedResult } from '../../../shared/models/api.models';
import { BookAvailabilityStatus, BookSummary } from '../../../shared/models/book.models';
import { BooksApiService } from '../../books/books-api.service';
import { DeleteConfirmPanelComponent } from '../../../shared/components/delete-confirm-panel/delete-confirm-panel.component';
import { PaginationComponent } from '../../../shared/components/pagination/pagination.component';

interface AdminBooksQuery {
  search?: string;
  page: number;
  pageSize: number;
}

@Component({
  selector: 'app-admin-books',
  standalone: true,
  imports: [DeleteConfirmPanelComponent, PaginationComponent, ReactiveFormsModule, RouterLink],
  templateUrl: './admin-books.component.html',
})
export class AdminBooksComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly api = inject(BooksApiService);
  private readonly apiErrors = inject(ApiErrorService);
  private readonly notifications = inject(NotificationService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly refreshRequests = new Subject<void>();

  protected readonly searchControl = new FormControl('', { nonNullable: true });
  protected readonly books = signal<BookSummary[]>([]);
  protected readonly result = signal<PagedResult<BookSummary> | null>(null);
  protected readonly query = signal<AdminBooksQuery>({ page: 1, pageSize: 20 });
  protected readonly loading = signal(true);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly availableStatus = BookAvailabilityStatus.Available;
  protected readonly pendingDeleteId = signal<number | null>(null);
  protected readonly isDeleting = signal(false);
  protected readonly deleteError = signal<string | null>(null);

  ngOnInit(): void {
    combineLatest([this.route.queryParamMap, this.refreshRequests.pipe(startWith(undefined))])
      .pipe(
        map(([params]) => this.readQuery(params)),
        tap((query) => {
          this.query.set(query);
          this.searchControl.setValue(query.search ?? '', { emitEvent: false });
          this.errorMessage.set(null);
        }),
        switchMap((query) => {
          this.loading.set(true);
          return this.api
            .getBooks({
              search: query.search,
              page: query.page,
              pageSize: query.pageSize,
              sortBy: 'title',
              sortDirection: 'asc',
            })
            .pipe(
              map((result) => ({ result, error: null })),
              catchError((error: unknown) => of({ result: null, error })),
              finalize(() => this.loading.set(false)),
            );
        }),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe(({ result, error }) => {
        if (result) {
          this.result.set(result);
          this.books.set(result.items);
        } else {
          this.result.set(null);
          this.books.set([]);
          this.errorMessage.set(this.apiErrors.messageFor(error, 'Unable to load the inventory.'));
        }
      });
  }

  protected search(): void {
    void this.navigate({
      search: this.searchControl.value.trim() || undefined,
      page: 1,
      pageSize: this.query().pageSize,
    });
  }

  protected goToPage(page: number): void {
    const totalPages = this.result()?.totalPages ?? 0;
    if (page < 1 || page > totalPages || page === this.query().page) return;
    void this.navigate({ ...this.query(), page });
  }

  protected retry(): void {
    this.refreshRequests.next();
  }

  protected pendingBook(): BookSummary | null {
    const id = this.pendingDeleteId();
    return id === null ? null : (this.books().find((book) => book.id === id) ?? null);
  }

  protected requestDelete(book: BookSummary): void {
    if (this.isDeleting()) return;
    this.pendingDeleteId.set(book.id);
    this.deleteError.set(null);
    this.notifications.dismiss();
  }

  protected cancelDelete(): void {
    if (this.isDeleting()) return;
    this.pendingDeleteId.set(null);
    this.deleteError.set(null);
  }

  protected confirmDelete(): void {
    const book = this.pendingBook();
    if (!book || this.isDeleting()) return;

    this.isDeleting.set(true);
    this.deleteError.set(null);
    this.api
      .deleteBook(book.id)
      .pipe(finalize(() => this.isDeleting.set(false)))
      .subscribe({
        next: () => {
          this.pendingDeleteId.set(null);
          this.notifications.show('success', `“${book.title}” was deleted.`);
          const remaining = this.books().filter((entry) => entry.id !== book.id);
          this.books.set(remaining);
          const nextCount = Math.max(0, (this.result()?.totalCount ?? 1) - 1);
          const pageSize = this.query().pageSize;
          this.result.update((current) =>
            current
              ? {
                  ...current,
                  items: remaining,
                  totalCount: nextCount,
                  totalPages: Math.ceil(nextCount / pageSize),
                }
              : current,
          );

          if (remaining.length === 0 && this.query().page > 1) {
            void this.goToPage(this.query().page - 1);
          } else {
            this.retry();
          }
        },
        error: (error: unknown) => {
          const fallback =
            error instanceof HttpErrorResponse && error.status === 409
              ? 'This book cannot be deleted because it has borrowing history.'
              : 'Unable to delete this book.';
          this.deleteError.set(this.apiErrors.messageFor(error, fallback));
        },
      });
  }

  private readQuery(params: ParamMap): AdminBooksQuery {
    const page = Number(params.get('page'));
    const pageSize = Number(params.get('pageSize'));
    return {
      search: params.get('search') || undefined,
      page: Number.isInteger(page) && page > 0 ? page : 1,
      pageSize: [10, 20, 50].includes(pageSize) ? pageSize : 20,
    };
  }

  private navigate(query: AdminBooksQuery): Promise<boolean> {
    return this.router.navigate([], {
      relativeTo: this.route,
      queryParams: {
        search: query.search ?? null,
        page: query.page,
        pageSize: query.pageSize,
      },
    });
  }
}
