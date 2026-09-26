import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { apiEndpoints } from '../../core/config/api-endpoints';
import {
  AdminBorrowingHistoryRequest,
  BorrowBookRequest,
  BorrowTransactionDto,
  BorrowingHistoryRequest,
} from '../../shared/models/borrowing.models';
import { PagedResult } from '../../shared/models/api.models';

@Injectable({ providedIn: 'root' })
export class BorrowingsApiService {
  private readonly http = inject(HttpClient);

  borrowBook(bookId: string): Observable<BorrowTransactionDto> {
    const request: BorrowBookRequest = { bookId };
    return this.http.post<BorrowTransactionDto>(apiEndpoints.borrowings, request);
  }

  getMyHistory(query: BorrowingHistoryRequest = {}): Observable<PagedResult<BorrowTransactionDto>> {
    let params = new HttpParams();
    for (const [key, value] of Object.entries(query)) {
      if (value !== undefined && value !== null && value !== '') {
        params = params.set(key, String(value));
      }
    }

    return this.http.get<PagedResult<BorrowTransactionDto>>(`${apiEndpoints.borrowings}/me`, {
      params,
    });
  }

  getAllHistory(
    query: AdminBorrowingHistoryRequest = {},
  ): Observable<PagedResult<BorrowTransactionDto>> {
    let params = new HttpParams();
    for (const [key, value] of Object.entries(query)) {
      if (value !== undefined && value !== null && value !== '') {
        params = params.set(key, String(value));
      }
    }

    return this.http.get<PagedResult<BorrowTransactionDto>>(apiEndpoints.borrowings, { params });
  }

  returnBook(borrowingId: string): Observable<BorrowTransactionDto> {
    return this.http.post<BorrowTransactionDto>(
      `${apiEndpoints.borrowings}/${borrowingId}/return`,
      {},
    );
  }

  cancelBorrowRequest(borrowingId: string): Observable<BorrowTransactionDto> {
    return this.http.post<BorrowTransactionDto>(
      `${apiEndpoints.borrowings}/${borrowingId}/cancel`,
      {},
    );
  }

  assignBorrowing(borrowingId: string): Observable<BorrowTransactionDto> {
    return this.http.post<BorrowTransactionDto>(
      `${apiEndpoints.borrowings}/${borrowingId}/assign`,
      {},
    );
  }

  rejectBorrowing(borrowingId: string): Observable<BorrowTransactionDto> {
    return this.http.post<BorrowTransactionDto>(
      `${apiEndpoints.borrowings}/${borrowingId}/reject`,
      {},
    );
  }

  acceptReturn(borrowingId: string): Observable<BorrowTransactionDto> {
    return this.http.post<BorrowTransactionDto>(
      `${apiEndpoints.borrowings}/${borrowingId}/accept-return`,
      {},
    );
  }
}
