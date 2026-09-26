import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { BooksApiService } from './books-api.service';

describe('BooksApiService endpoint mapping', () => {
  beforeEach(() => TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] }));
  afterEach(() => {
    TestBed.inject(HttpTestingController).verify();
    TestBed.resetTestingModule();
  });

  it('maps list/detail/create/update/delete to the books resource', () => {
    const api = TestBed.inject(BooksApiService);
    const http = TestBed.inject(HttpTestingController);
    api.getBooks({ search: 'dune', available: true, page: 2, pageSize: 10 }).subscribe();
    const list = http.expectOne((request) => request.url === '/api/v1/books');
    expect(list.request.method).toBe('GET');
    expect(list.request.params.get('search')).toBe('dune');
    expect(list.request.params.get('available')).toBe('true');
    expect(list.request.params.get('page')).toBe('2');
    list.flush({ items: [], page: 2, pageSize: 10, totalCount: 0, totalPages: 0 });

    api.getBookById(4).subscribe();
    expect(http.expectOne('/api/v1/books/4').request.method).toBe('GET');
    api.createBook({ isbn: '9780000000001', title: 'Dune', author: 'Frank Herbert', publisher: null, publishedYear: 1965, categoryId: 1 }).subscribe();
    expect(http.expectOne('/api/v1/books').request.method).toBe('POST');
    api.updateBook(4, { isbn: '9780000000001', title: 'Dune', author: 'Frank Herbert', publisher: null, publishedYear: 1965, categoryId: 1, rowVersion: 'AQID' }).subscribe();
    expect(http.expectOne('/api/v1/books/4').request.method).toBe('PUT');
    api.deleteBook(4).subscribe();
    const deletion = http.expectOne('/api/v1/books/4');
    expect(deletion.request.method).toBe('DELETE');
    deletion.flush(null);
  });
});
