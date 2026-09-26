import { Component, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { HttpErrorResponse } from '@angular/common/http';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
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
import {
  BookAvailabilityStatus,
  BookSearchRequest,
  BookSummary,
  CategoryDto,
} from '../../../shared/models/book.models';
import { CategoriesApiService } from '../categories-api.service';
import { PagedResult } from '../../../shared/models/api.models';
import { BorrowConfirmPanelComponent } from '../../../shared/components/borrow-confirm-panel/borrow-confirm-panel.component';
import { BorrowingsApiService } from '../../borrowings/borrowings-api.service';
import { BooksApiService } from '../books-api.service';
import { PaginationComponent } from '../../../shared/components/pagination/pagination.component';

@Component({
  selector: 'app-book-list',
  standalone: true,
  imports: [BorrowConfirmPanelComponent, PaginationComponent, ReactiveFormsModule, RouterLink],
  templateUrl: './book-list.component.html',
  styleUrl: './book-list.component.css',
})
export class BookListComponent implements OnInit {
  private readonly booksApi = inject(BooksApiService);
  private readonly categoriesApi = inject(CategoriesApiService);
  private readonly apiErrors = inject(ApiErrorService);
  private readonly notifications = inject(NotificationService);
  private readonly borrowingsApi = inject(BorrowingsApiService);
  private readonly formBuilder = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly books = signal<BookSummary[]>([]);
  protected readonly categories = signal<CategoryDto[]>([]);
  protected readonly categoriesError = signal<string | null>(null);
  protected readonly loading = signal(true);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly pendingBorrowId = signal<number | null>(null);
  protected readonly isBorrowing = signal(false);
  protected readonly borrowError = signal<string | null>(null);
  protected readonly availableStatus = BookAvailabilityStatus.Available;
  protected readonly result = signal<PagedResult<BookSummary> | null>(null);
  protected readonly query = signal<BookSearchRequest>({
    sortBy: 'title',
    sortDirection: 'asc',
    page: 1,
    pageSize: 20,
  });
  protected readonly filterForm = this.formBuilder.nonNullable.group({
    search: [''],
    author: [''],
    isbn: [''],
    categoryId: [''],
    available: [''],
    sortBy: ['title'],
    sortDirection: ['asc'],
    pageSize: ['20'],
  });
  private readonly refreshRequests = new Subject<void>();

  ngOnInit(): void {
    this.loadCategories();
    combineLatest([this.route.queryParamMap, this.refreshRequests.pipe(startWith(undefined))])
      .pipe(
        map(([params]) => this.readQuery(params)),
        tap((query) => {
          this.query.set(query);
          this.syncForm(query);
          this.errorMessage.set(null);
        }),
        switchMap((query) => {
          this.loading.set(true);
          return this.booksApi.getBooks(query).pipe(
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
          this.errorMessage.set(
            this.apiErrors.messageFor(error, 'Unable to load books. Try again.'),
          );
        }
      });
  }

  protected loadCategories(): void {
    this.categoriesError.set(null);
    this.categoriesApi
      .getCategories()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (categories) => this.categories.set(categories),
        error: (error: unknown) =>
          this.categoriesError.set(this.apiErrors.messageFor(error, 'Unable to load categories.')),
      });
  }

  protected applyFilters(): void {
    const values = this.filterForm.getRawValue();
    const categoryId = Number(values.categoryId);
    const available = values.available === '' ? undefined : values.available === 'true';
    const query: BookSearchRequest = {
      search: values.search.trim() || undefined,
      author: values.author.trim() || undefined,
      isbn: values.isbn.trim() || undefined,
      categoryId: Number.isInteger(categoryId) && categoryId > 0 ? categoryId : undefined,
      available,
      sortBy: values.sortBy as BookSearchRequest['sortBy'],
      sortDirection: values.sortDirection as BookSearchRequest['sortDirection'],
      page: 1,
      pageSize: Number(values.pageSize),
    };

    void this.updateUrl(query);
  }

  protected resetFilters(): void {
    this.filterForm.reset({
      search: '',
      author: '',
      isbn: '',
      categoryId: '',
      available: '',
      sortBy: 'title',
      sortDirection: 'asc',
      pageSize: '20',
    });
    this.applyFilters();
  }

  protected goToPage(page: number): void {
    const totalPages = this.result()?.totalPages ?? 0;
    if (page < 1 || page > totalPages || page === this.query().page) return;
    void this.updateUrl({ ...this.query(), page });
  }

  protected reload(): void {
    this.refreshRequests.next();
  }

  protected requestBorrow(book: BookSummary): void {
    if (book.availabilityStatus !== this.availableStatus || this.isBorrowing()) return;
    this.pendingBorrowId.set(book.id);
    this.borrowError.set(null);
    this.notifications.dismiss();
  }

  protected pendingBook(): BookSummary | null {
    const id = this.pendingBorrowId();
    return id === null ? null : (this.books().find((book) => book.id === id) ?? null);
  }

  protected cancelBorrow(): void {
    if (this.isBorrowing()) return;
    this.pendingBorrowId.set(null);
    this.borrowError.set(null);
  }

  protected confirmBorrow(): void {
    const book = this.pendingBook();
    if (!book || this.isBorrowing()) return;

    this.isBorrowing.set(true);
    this.borrowError.set(null);
    this.borrowingsApi
      .borrowBook(book.id)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.isBorrowing.set(false)),
      )
      .subscribe({
        next: () => {
          this.pendingBorrowId.set(null);
          this.notifications.show('success', `“${book.title}” was borrowed successfully.`);
          this.reload();
        },
        error: (error: unknown) => {
          this.pendingBorrowId.set(null);
          this.borrowError.set(
            this.apiErrors.messageFor(error, 'This book is no longer available.'),
          );
          if (error instanceof HttpErrorResponse && error.status === 409) this.reload();
        },
      });
  }

  private async updateUrl(query: BookSearchRequest, replaceUrl = false): Promise<void> {
    await this.router.navigate([], {
      relativeTo: this.route,
      queryParams: {
        search: query.search ?? null,
        author: query.author ?? null,
        isbn: query.isbn ?? null,
        categoryId: query.categoryId ?? null,
        available: query.available ?? null,
        sortBy: query.sortBy ?? 'title',
        sortDirection: query.sortDirection ?? 'asc',
        page: query.page ?? 1,
        pageSize: query.pageSize ?? 20,
      },
      replaceUrl,
    });
  }

  private readQuery(params: ParamMap): BookSearchRequest {
    const categoryValue = Number(params.get('categoryId'));
    const pageValue = Number(params.get('page'));
    const pageSizeValue = Number(params.get('pageSize'));
    const sortBy = params.get('sortBy');
    const sortDirection = params.get('sortDirection');
    const availableValue = params.get('available');

    return {
      search: params.get('search') || undefined,
      author: params.get('author') || undefined,
      isbn: params.get('isbn') || undefined,
      categoryId: Number.isInteger(categoryValue) && categoryValue > 0 ? categoryValue : undefined,
      available: availableValue === 'true' ? true : availableValue === 'false' ? false : undefined,
      sortBy: sortBy === 'author' || sortBy === 'publishedYear' ? sortBy : 'title',
      sortDirection: sortDirection === 'desc' ? 'desc' : 'asc',
      page: Number.isInteger(pageValue) && pageValue > 0 ? pageValue : 1,
      pageSize: [10, 20, 50].includes(pageSizeValue) ? pageSizeValue : 20,
    };
  }

  private syncForm(query: BookSearchRequest): void {
    this.filterForm.patchValue(
      {
        search: query.search ?? '',
        author: query.author ?? '',
        isbn: query.isbn ?? '',
        categoryId: query.categoryId?.toString() ?? '',
        available: query.available === undefined ? '' : String(query.available),
        sortBy: query.sortBy ?? 'title',
        sortDirection: query.sortDirection ?? 'asc',
        pageSize: String(query.pageSize ?? 20),
      },
      { emitEvent: false },
    );
  }
}
