import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
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
  BorrowTransactionStatus,
  BorrowingHistoryRequest,
  BorrowingHistoryItem,
} from '../../../shared/models/borrowing.models';
import { PagedResult } from '../../../shared/models/api.models';
import { ReturnConfirmPanelComponent } from '../../../shared/components/return-confirm-panel/return-confirm-panel.component';
import { BorrowingsApiService } from '../borrowings-api.service';
import { PaginationComponent } from '../../../shared/components/pagination/pagination.component';

type HistoryFilter = 'all' | 'Borrowed' | 'Returned';

@Component({
  selector: 'app-my-borrowings',
  standalone: true,
  imports: [DatePipe, PaginationComponent, ReturnConfirmPanelComponent, RouterLink],
  templateUrl: './my-borrowings.component.html',
  styleUrl: './my-borrowings.component.css',
})
export class MyBorrowingsComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly api = inject(BorrowingsApiService);
  private readonly apiErrors = inject(ApiErrorService);
  private readonly notifications = inject(NotificationService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly refreshRequests = new Subject<void>();

  protected readonly history = signal<BorrowingHistoryItem[]>([]);
  protected readonly result = signal<PagedResult<BorrowingHistoryItem> | null>(null);
  protected readonly filter = signal<HistoryFilter>('all');
  protected readonly page = signal(1);
  protected readonly pageSize = signal(20);
  protected readonly loading = signal(true);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly pendingReturnId = signal<number | null>(null);
  protected readonly isReturning = signal(false);
  protected readonly returnError = signal<string | null>(null);
  protected readonly borrowedStatus = BorrowTransactionStatus.Borrowed;

  ngOnInit(): void {
    combineLatest([this.route.queryParamMap, this.refreshRequests.pipe(startWith(undefined))])
      .pipe(
        map(([params]) => this.readQuery(params)),
        tap((query) => {
          this.filter.set(query.status ?? 'all');
          this.page.set(query.page ?? 1);
          this.pageSize.set(query.pageSize ?? 20);
          this.errorMessage.set(null);
        }),
        switchMap((query) => {
          this.loading.set(true);
          return this.api.getMyHistory(query).pipe(
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
          this.history.set(result.items);
        } else {
          this.result.set(null);
          this.history.set([]);
          this.errorMessage.set(
            this.apiErrors.messageFor(error, 'Unable to load your borrowing history.'),
          );
        }
      });
  }

  protected setFilter(value: string): void {
    const status = value === 'Borrowed' || value === 'Returned' ? value : 'all';
    void this.navigate({ status, page: 1, pageSize: this.pageSize() });
  }

  protected goToPage(page: number): void {
    const totalPages = this.result()?.totalPages ?? 0;
    if (page < 1 || page > totalPages || page === this.page()) return;
    void this.navigate({ status: this.filter(), page, pageSize: this.pageSize() });
  }

  protected retry(): void {
    this.refreshRequests.next();
  }

  protected pendingItem(): BorrowingHistoryItem | null {
    const id = this.pendingReturnId();
    return id === null ? null : (this.history().find((item) => item.id === id) ?? null);
  }

  protected requestReturn(item: BorrowingHistoryItem): void {
    if (item.status !== this.borrowedStatus || this.isReturning()) return;
    this.pendingReturnId.set(item.id);
    this.returnError.set(null);
    this.notifications.dismiss();
  }

  protected cancelReturn(): void {
    if (this.isReturning()) return;
    this.pendingReturnId.set(null);
    this.returnError.set(null);
  }

  protected confirmReturn(): void {
    const item = this.pendingItem();
    if (!item || this.isReturning()) return;

    this.isReturning.set(true);
    this.returnError.set(null);
    this.api
      .returnBook(item.id)
      .pipe(finalize(() => this.isReturning.set(false)))
      .subscribe({
        next: (returned) => {
          this.pendingReturnId.set(null);
          this.notifications.show('success', `“${item.bookTitle}” was returned successfully.`);
          this.history.update((items) =>
            items.map((entry) => (entry.id === returned.id ? returned : entry)),
          );
          this.refreshRequests.next();
        },
        error: (error: unknown) => {
          this.pendingReturnId.set(null);
          this.returnError.set(this.apiErrors.messageFor(error, 'Unable to return this book.'));
          if (error instanceof HttpErrorResponse && error.status === 409)
            this.refreshRequests.next();
        },
      });
  }

  private readQuery(params: ParamMap): BorrowingHistoryRequest {
    const statusValue = params.get('status');
    const pageValue = Number(params.get('page'));
    const pageSizeValue = Number(params.get('pageSize'));
    const status =
      statusValue === 'Borrowed' || statusValue === 'Returned' ? statusValue : undefined;

    return {
      status,
      sort: '-borrowedat',
      page: Number.isInteger(pageValue) && pageValue > 0 ? pageValue : 1,
      pageSize: [10, 20, 50].includes(pageSizeValue) ? pageSizeValue : 20,
    };
  }

  private async navigate(query: {
    status: HistoryFilter;
    page: number;
    pageSize: number;
  }): Promise<void> {
    await this.routeToQuery({
      status: query.status === 'all' ? null : query.status,
      page: query.page,
      pageSize: query.pageSize,
    });
  }

  private routeToQuery(queryParams: Record<string, string | number | null>): Promise<boolean> {
    return this.router.navigate([], { relativeTo: this.route, queryParams });
  }
}
