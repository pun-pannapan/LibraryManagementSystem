import { DatePipe } from '@angular/common';
import { Component, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
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
import { BorrowingsApiService } from '../../borrowings/borrowings-api.service';
import {
  AdminBorrowingHistoryRequest,
  BORROWING_HISTORY_SORTS,
  BORROWING_STATUS_FILTERS,
  BorrowTransactionDto,
  BorrowTransactionStatus,
  BorrowingHistorySort,
  BorrowingStatusFilter,
} from '../../../shared/models/borrowing.models';
import { PagedResult } from '../../../shared/models/api.models';
import { PaginationComponent } from '../../../shared/components/pagination/pagination.component';

interface TransactionFilters {
  search: string;
  status: string;
  sort: BorrowingHistorySort;
  borrowedFrom: string;
  borrowedTo: string;
  returnedFrom: string;
  returnedTo: string;
}

@Component({
  selector: 'app-admin-transactions',
  standalone: true,
  imports: [DatePipe, PaginationComponent, ReactiveFormsModule, RouterLink],
  templateUrl: './admin-transactions.component.html',
  styleUrl: './admin-transactions.component.css',
})
export class AdminTransactionsComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly api = inject(BorrowingsApiService);
  private readonly formBuilder = inject(FormBuilder);
  private readonly apiErrors = inject(ApiErrorService);
  private readonly notifications = inject(NotificationService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly refreshRequests = new Subject<void>();

  protected readonly transactions = signal<BorrowTransactionDto[]>([]);
  protected readonly result = signal<PagedResult<BorrowTransactionDto> | null>(null);
  protected readonly filters = signal<TransactionFilters>(this.emptyFilters());
  protected readonly page = signal(1);
  protected readonly pageSize = signal(10);
  protected readonly loading = signal(true);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly filterError = signal<string | null>(null);
  protected readonly filterForm = this.formBuilder.nonNullable.group({
    search: ['', Validators.maxLength(250)],
    status: [''],
    sort: ['-requestedat'],
    borrowedFrom: [''],
    borrowedTo: [''],
    returnedFrom: [''],
    returnedTo: [''],
  });
  protected readonly borrowedStatus = BorrowTransactionStatus.Borrowed;
  protected readonly returnedStatus = BorrowTransactionStatus.Returned;
  protected readonly requestedStatus = BorrowTransactionStatus.Requested;
  protected readonly returnRequestedStatus = BorrowTransactionStatus.ReturnRequested;
  protected readonly processingId = signal<string | null>(null);

  ngOnInit(): void {
    combineLatest([this.route.queryParamMap, this.refreshRequests.pipe(startWith(undefined))])
      .pipe(
        map(([params]) => this.readQuery(params)),
        tap(({ filters, page, pageSize }) => {
          this.filters.set(filters);
          this.filterForm.patchValue(filters, { emitEvent: false });
          this.filterError.set(null);
          this.page.set(page);
          this.pageSize.set(pageSize);
          this.errorMessage.set(null);
        }),
        switchMap((query) => {
          this.loading.set(true);
          return this.api.getAllHistory(query.request).pipe(
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
          this.transactions.set(result.items);
        } else {
          this.result.set(null);
          this.transactions.set([]);
          this.errorMessage.set(
            this.apiErrors.messageFor(error, 'Unable to load transaction history.'),
          );
        }
      });
  }

  protected applyFilters(): void {
    const values = this.filterForm.getRawValue();
    const filters = {
      ...values,
      search: values.search.trim(),
      sort: values.sort as BorrowingHistorySort,
    };
    this.filterForm.patchValue(filters);
    this.filterError.set(null);

    if (this.filterForm.invalid) {
      this.filterForm.markAllAsTouched();
      this.filterError.set('Search text must be 250 characters or fewer.');
      return;
    }

    if (
      (filters.borrowedFrom && filters.borrowedTo && filters.borrowedFrom > filters.borrowedTo) ||
      (filters.returnedFrom && filters.returnedTo && filters.returnedFrom > filters.returnedTo)
    ) {
      this.filterError.set('The end date must be on or after the start date.');
      return;
    }

    void this.navigate(filters, 1, this.pageSize());
  }

  protected clearFilters(): void {
    const filters = this.emptyFilters();
    this.filterForm.reset(filters);
    this.filterError.set(null);
    void this.navigate(filters, 1, this.pageSize());
  }

  protected goToPage(page: number): void {
    const totalPages = this.result()?.totalPages ?? 0;
    if (page < 1 || page > totalPages || page === this.page()) return;
    void this.navigate(this.filters(), page, this.pageSize());
  }

  protected retry(): void {
    this.refreshRequests.next();
  }

  protected processAction(id: string, action: 'assign' | 'reject' | 'accept-return'): void {
    if (this.processingId()) return;

    const request = this.transactionActionRequest(id, action);
    this.processingId.set(id);
    request.pipe(finalize(() => this.processingId.set(null))).subscribe({
      next: () => {
        this.notifications.show('success', this.transactionActionMessage(action));
        this.refreshRequests.next();
      },
      error: (error: unknown) => {
        this.errorMessage.set(
          this.apiErrors.messageFor(error, 'The transaction could not be processed.'),
        );
        this.refreshRequests.next();
      },
    });
  }

  protected changePageSize(value: string): void {
    const pageSize = Number(value);
    if ([10, 20, 50].includes(pageSize)) void this.navigate(this.filters(), 1, pageSize);
  }

  protected sortByColumn(column: 'borrowedat' | 'dueat' | 'returnedat' | 'status'): void {
    const current = this.filters().sort;
    const ascending = current !== column;
    const sort = (ascending ? column : `-${column}`) as BorrowingHistorySort;
    const filters = { ...this.filters(), sort };
    void this.navigate(filters, 1, this.pageSize());
  }

  protected sortIndicator(column: 'borrowedat' | 'dueat' | 'returnedat' | 'status'): string {
    const sort = this.filters().sort;
    if (sort !== column && sort !== `-${column}`) return '';
    return sort === column ? '↑' : '↓';
  }

  private readQuery(params: ParamMap): {
    filters: TransactionFilters;
    page: number;
    pageSize: number;
    request: AdminBorrowingHistoryRequest;
  } {
    const sortValue = params.get('sort') ?? '-requestedat';
    const sort = BORROWING_HISTORY_SORTS.includes(sortValue as BorrowingHistorySort)
      ? (sortValue as BorrowingHistorySort)
      : '-requestedat';
    const filters: TransactionFilters = {
      search: params.get('search') ?? '',
      status: params.get('status') ?? '',
      sort,
      borrowedFrom: this.readDate(params.get('borrowedFrom')),
      borrowedTo: this.readDate(params.get('borrowedTo')),
      returnedFrom: this.readDate(params.get('returnedFrom')),
      returnedTo: this.readDate(params.get('returnedTo')),
    };
    const pageValue = Number(params.get('page'));
    const pageSizeValue = Number(params.get('pageSize'));
    const request: AdminBorrowingHistoryRequest = {
      search: filters.search || undefined,
      status: BORROWING_STATUS_FILTERS.includes(filters.status as BorrowingStatusFilter)
        ? (filters.status as BorrowingStatusFilter)
        : undefined,
      borrowedFrom: filters.borrowedFrom ? `${filters.borrowedFrom}T00:00:00Z` : undefined,
      borrowedTo: filters.borrowedTo ? `${filters.borrowedTo}T23:59:59.9999999Z` : undefined,
      returnedFrom: filters.returnedFrom ? `${filters.returnedFrom}T00:00:00Z` : undefined,
      returnedTo: filters.returnedTo ? `${filters.returnedTo}T23:59:59.9999999Z` : undefined,
      sort: BORROWING_HISTORY_SORTS.includes(filters.sort) ? filters.sort : '-requestedat',
      page: Number.isInteger(pageValue) && pageValue > 0 ? pageValue : 1,
      pageSize: [10, 20, 50].includes(pageSizeValue) ? pageSizeValue : 10,
    };
    return { filters, page: request.page ?? 1, pageSize: request.pageSize ?? 10, request };
  }

  private navigate(filters: TransactionFilters, page: number, pageSize: number): Promise<boolean> {
    return this.router.navigate([], {
      relativeTo: this.route,
      queryParams: {
        search: filters.search || null,
        status: filters.status || null,
        sort: filters.sort || '-requestedat',
        borrowedFrom: filters.borrowedFrom || null,
        borrowedTo: filters.borrowedTo || null,
        returnedFrom: filters.returnedFrom || null,
        returnedTo: filters.returnedTo || null,
        page,
        pageSize,
      },
    });
  }

  private readDate(value: string | null): string {
    if (!value || !/^\d{4}-\d{2}-\d{2}$/.test(value)) return '';
    const date = new Date(`${value}T00:00:00Z`);
    return Number.isFinite(date.getTime()) && date.toISOString().slice(0, 10) === value
      ? value
      : '';
  }

  private transactionActionRequest(id: string, action: 'assign' | 'reject' | 'accept-return') {
    switch (action) {
      case 'assign':
        return this.api.assignBorrowing(id);
      case 'reject':
        return this.api.rejectBorrowing(id);
      case 'accept-return':
        return this.api.acceptReturn(id);
    }
  }

  private transactionActionMessage(action: 'assign' | 'reject' | 'accept-return'): string {
    switch (action) {
      case 'assign':
        return 'Borrow request assigned.';
      case 'reject':
        return 'Borrow request rejected.';
      case 'accept-return':
        return 'Return accepted.';
    }
  }

  private emptyFilters(): TransactionFilters {
    return {
      search: '',
      status: '',
      sort: '-requestedat',
      borrowedFrom: '',
      borrowedTo: '',
      returnedFrom: '',
      returnedTo: '',
    };
  }
}
