import { Component, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, ParamMap, RouterLink } from '@angular/router';
import { catchError, combineLatest, finalize, map, of, startWith, Subject, switchMap } from 'rxjs';
import { ApiErrorService } from '../../../core/http/api-error.service';
import { NotificationService } from '../../../core/notifications/notification.service';
import { BookAvailabilityStatus, BookDetail } from '../../../shared/models/book.models';
import { BorrowConfirmPanelComponent } from '../../../shared/components/borrow-confirm-panel/borrow-confirm-panel.component';
import { BorrowingsApiService } from '../../borrowings/borrowings-api.service';
import { BooksApiService } from '../books-api.service';

@Component({
  selector: 'app-book-detail',
  standalone: true,
  imports: [BorrowConfirmPanelComponent, RouterLink],
  templateUrl: './book-detail.component.html',
  styleUrl: './book-detail.component.css',
})
export class BookDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly booksApi = inject(BooksApiService);
  private readonly apiErrors = inject(ApiErrorService);
  private readonly notifications = inject(NotificationService);
  private readonly borrowingsApi = inject(BorrowingsApiService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly refreshRequests = new Subject<void>();

  protected readonly book = signal<BookDetail | null>(null);
  protected readonly loading = signal(true);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly notFound = signal(false);
  protected readonly availableStatus = BookAvailabilityStatus.Available;
  protected readonly confirmingBorrow = signal(false);
  protected readonly isBorrowing = signal(false);
  protected readonly borrowError = signal<string | null>(null);

  ngOnInit(): void {
    combineLatest([this.route.paramMap, this.refreshRequests.pipe(startWith(undefined))])
      .pipe(
        map(([params]) => this.readId(params)),
        switchMap((id) => {
          this.loading.set(true);
          this.errorMessage.set(null);
          this.notFound.set(false);
          this.confirmingBorrow.set(false);
          if (id === null) {
            this.book.set(null);
            this.loading.set(false);
            this.notFound.set(true);
            return of({ book: null, error: new HttpErrorResponse({ status: 404 }) });
          }

          return this.booksApi.getBookById(id).pipe(
            map((book) => ({ book, error: null })),
            catchError((error: unknown) => of({ book: null, error })),
            finalize(() => this.loading.set(false)),
          );
        }),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe(({ book, error }) => {
        this.book.set(book);
        if (error) {
          this.notFound.set(error instanceof HttpErrorResponse && error.status === 404);
          this.errorMessage.set(
            this.notFound()
              ? 'This book could not be found.'
              : this.apiErrors.messageFor(error, 'Unable to load this book.'),
          );
        }
      });
  }

  protected reload(): void {
    this.refreshRequests.next();
  }

  protected requestBorrow(): void {
    const item = this.book();
    if (!item || item.availabilityStatus !== this.availableStatus || this.isBorrowing()) return;
    this.confirmingBorrow.set(true);
    this.borrowError.set(null);
    this.notifications.dismiss();
  }

  protected cancelBorrow(): void {
    if (this.isBorrowing()) return;
    this.confirmingBorrow.set(false);
    this.borrowError.set(null);
  }

  protected confirmBorrow(): void {
    const item = this.book();
    if (!item || !this.confirmingBorrow() || this.isBorrowing()) return;

    this.isBorrowing.set(true);
    this.borrowError.set(null);
    this.borrowingsApi
      .borrowBook(item.id)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.isBorrowing.set(false)),
      )
      .subscribe({
        next: () => {
          this.confirmingBorrow.set(false);
          this.notifications.show('success', `“${item.title}” was borrowed successfully.`);
          this.reload();
        },
        error: (error: unknown) => {
          this.confirmingBorrow.set(false);
          this.borrowError.set(
            this.apiErrors.messageFor(error, 'This book is no longer available.'),
          );
          if (error instanceof HttpErrorResponse && error.status === 409) this.reload();
        },
      });
  }

  private readId(params: ParamMap): number | null {
    const id = Number(params.get('id'));
    return Number.isInteger(id) && id > 0 ? id : null;
  }
}
