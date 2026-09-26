import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { BorrowingsApiService } from './borrowings-api.service';

describe('BorrowingsApiService endpoint mapping', () => {
  beforeEach(() => TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] }));
  afterEach(() => {
    TestBed.inject(HttpTestingController).verify();
    TestBed.resetTestingModule();
  });

  it('maps borrow, personal history, admin history, and return requests', () => {
    const api = TestBed.inject(BorrowingsApiService);
    const http = TestBed.inject(HttpTestingController);
    api.borrowBook(9).subscribe();
    const borrow = http.expectOne('/api/v1/borrowings');
    expect(borrow.request.method).toBe('POST');
    expect(borrow.request.body).toEqual({ bookId: 9 });
    borrow.flush({ id: 1 });

    api.getMyHistory({ status: 'Borrowed', page: 2, pageSize: 10 }).subscribe();
    const mine = http.expectOne((request) => request.url === '/api/v1/borrowings/me');
    expect(mine.request.method).toBe('GET');
    expect(mine.request.params.get('status')).toBe('Borrowed');
    expect(mine.request.params.get('page')).toBe('2');
    mine.flush({ items: [], page: 2, pageSize: 10, totalCount: 0, totalPages: 0 });

    api.getAllHistory({ userId: '00000000-0000-0000-0000-000000000001', bookId: 9, status: 'Returned', borrowedFrom: '2026-01-01', borrowedTo: '2026-01-31', page: 1, pageSize: 20 }).subscribe();
    const all = http.expectOne((request) => request.url === '/api/v1/borrowings');
    expect(all.request.method).toBe('GET');
    expect(all.request.params.get('userId')).toBe('00000000-0000-0000-0000-000000000001');
    expect(all.request.params.get('bookId')).toBe('9');
    expect(all.request.params.get('status')).toBe('Returned');
    expect(all.request.params.get('borrowedFrom')).toBe('2026-01-01');
    expect(all.request.params.get('borrowedTo')).toBe('2026-01-31');
    all.flush({ items: [], page: 1, pageSize: 20, totalCount: 0, totalPages: 0 });

    api.returnBook(3).subscribe();
    const returned = http.expectOne('/api/v1/borrowings/3/return');
    expect(returned.request.method).toBe('POST');
    returned.flush({ id: 3 });
  });
});
