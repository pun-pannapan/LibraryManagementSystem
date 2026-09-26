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
import { BorrowingsApiService } from '../../borrowings/borrowings-api.service';
import {
  AdminBorrowingHistoryRequest,
  BorrowTransactionDto,
  BorrowTransactionStatus,
} from '../../../shared/models/borrowing.models';
import { PagedResult } from '../../../shared/models/api.models';
import { PaginationComponent } from '../../../shared/components/pagination/pagination.component';

interface TransactionFilters {
  search: string;
  status: string;
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
  private readonly destroyRef = inject(DestroyRef);
  private readonly refreshRequests = new Subject<void>();

  protected readonly transactions = signal<BorrowTransactionDto[]>([]);
  protected readonly result = signal<PagedResult<BorrowTransactionDto> | null>(null);
  protected readonly filters = signal<TransactionFilters>(this.emptyFilters());
  protected readonly page = signal(1);
  protected readonly pageSize = signal(20);
  protected readonly loading = signal(true);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly filterError = signal<string | null>(null);
  protected readonly filterForm = this.formBuilder.nonNullable.group({
    search: ['', Validators.maxLength(250)],
    status: [''],
    borrowedFrom: [''],
    borrowedTo: [''],
    returnedFrom: [''],
    returnedTo: [''],
  });
  protected readonly borrowedStatus = BorrowTransactionStatus.Borrowed;
  protected readonly returnedStatus = BorrowTransactionStatus.Returned;

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
    const filters = { ...values, search: values.search.trim() };
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

  protected changePageSize(value: string): void {
    const pageSize = Number(value);
    if ([10, 20, 50].includes(pageSize)) void this.navigate(this.filters(), 1, pageSize);
  }

  private readQuery(params: ParamMap): {
    filters: TransactionFilters;
    page: number;
    pageSize: number;
    request: AdminBorrowingHistoryRequest;
  } {
    const filters: TransactionFilters = {
      search: params.get('search') ?? '',
      status: params.get('status') ?? '',
      borrowedFrom: this.readDate(params.get('borrowedFrom')),
      borrowedTo: this.readDate(params.get('borrowedTo')),
      returnedFrom: this.readDate(params.get('returnedFrom')),
      returnedTo: this.readDate(params.get('returnedTo')),
    };
    const pageValue = Number(params.get('page'));
    const pageSizeValue = Number(params.get('pageSize'));
    const request: AdminBorrowingHistoryRequest = {
      search: filters.search || undefined,
      status:
        filters.status === 'Borrowed' || filters.status === 'Returned' ? filters.status : undefined,
      borrowedFrom: filters.borrowedFrom ? `${filters.borrowedFrom}T00:00:00Z` : undefined,
      borrowedTo: filters.borrowedTo ? `${filters.borrowedTo}T23:59:59.9999999Z` : undefined,
      returnedFrom: filters.returnedFrom ? `${filters.returnedFrom}T00:00:00Z` : undefined,
      returnedTo: filters.returnedTo ? `${filters.returnedTo}T23:59:59.9999999Z` : undefined,
      sort: '-borrowedat',
      page: Number.isInteger(pageValue) && pageValue > 0 ? pageValue : 1,
      pageSize: [10, 20, 50].includes(pageSizeValue) ? pageSizeValue : 20,
    };
    return { filters, page: request.page ?? 1, pageSize: request.pageSize ?? 20, request };
  }

  private navigate(filters: TransactionFilters, page: number, pageSize: number): Promise<boolean> {
    return this.router.navigate([], {
      relativeTo: this.route,
      queryParams: {
        search: filters.search || null,
        status: filters.status || null,
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

  private emptyFilters(): TransactionFilters {
    return {
      search: '',
      status: '',
      borrowedFrom: '',
      borrowedTo: '',
      returnedFrom: '',
      returnedTo: '',
    };
  }
}
