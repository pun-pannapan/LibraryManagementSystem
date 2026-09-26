import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { apiEndpoints } from '../../core/config/api-endpoints';
import {
  BookDetail,
  BookSearchRequest,
  BookSummary,
  CreateBookRequest,
  UpdateBookRequest,
} from '../../shared/models/book.models';
import { PagedResult } from '../../shared/models/api.models';

@Injectable({ providedIn: 'root' })
export class BooksApiService {
  private readonly http = inject(HttpClient);

  getBooks(query: BookSearchRequest = {}): Observable<PagedResult<BookSummary>> {
    let params = new HttpParams();
    for (const [key, value] of Object.entries(query)) {
      if (value !== undefined && value !== null && value !== '') {
        params = params.set(key, String(value));
      }
    }

    return this.http.get<PagedResult<BookSummary>>(apiEndpoints.books, { params });
  }

  getBookById(id: string): Observable<BookDetail> {
    return this.http.get<BookDetail>(`${apiEndpoints.books}/${id}`);
  }

  createBook(request: CreateBookRequest): Observable<BookDetail> {
    return this.http.post<BookDetail>(apiEndpoints.books, request);
  }

  updateBook(id: string, request: UpdateBookRequest): Observable<BookDetail> {
    return this.http.put<BookDetail>(`${apiEndpoints.books}/${id}`, request);
  }

  deleteBook(id: string): Observable<void> {
    return this.http.delete<void>(`${apiEndpoints.books}/${id}`);
  }
}
