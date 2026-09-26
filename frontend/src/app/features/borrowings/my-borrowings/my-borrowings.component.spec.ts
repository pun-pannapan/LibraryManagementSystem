import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { MyBorrowingsComponent } from './my-borrowings.component';

describe('MyBorrowingsComponent', () => {
  const route = { queryParamMap: of(convertToParamMap({})) };

  beforeEach(() =>
    TestBed.configureTestingModule({
      imports: [MyBorrowingsComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: ActivatedRoute, useValue: route },
      ],
    }),
  );

  afterEach(() => {
    TestBed.inject(HttpTestingController).verify();
    TestBed.resetTestingModule();
  });

  it('shows the backend conflict when a borrowing was already returned', () => {
    const fixture = TestBed.createComponent(MyBorrowingsComponent);
    const http = TestBed.inject(HttpTestingController);
    fixture.detectChanges();
    http
      .expectOne((request) => request.url === '/api/v1/borrowings/me')
      .flush({
        items: [
          {
            id: '30000000-0000-0000-0000-000000000001',
            bookId: '20000000-0000-0000-0000-000000000001',
            bookTitle: 'Dune',
            userId: 'user-1',
            userEmail: 'reader@example.test',
            borrowedAtUtc: '2026-01-01T00:00:00Z',
            dueAtUtc: '2026-01-15T00:00:00Z',
            returnedAtUtc: null,
            status: 1,
          },
        ],
        page: 1,
        pageSize: 20,
        totalCount: 1,
        totalPages: 1,
      });
    fixture.detectChanges();

    (
      fixture.nativeElement.querySelector('[aria-label="Return Dune"]') as HTMLButtonElement
    ).click();
    fixture.detectChanges();
    const buttons = fixture.nativeElement.querySelectorAll(
      'button',
    ) as NodeListOf<HTMLButtonElement>;
    const confirm = Array.from(buttons).find((button) =>
      button.textContent?.includes('Confirm return'),
    );
    expect(confirm).toBeDefined();
    confirm?.click();

    const request = http.expectOne(
      '/api/v1/borrowings/30000000-0000-0000-0000-000000000001/return',
    );
    expect(request.request.method).toBe('POST');
    request.flush(
      { message: 'This borrowing has already been returned.' },
      { status: 409, statusText: 'Conflict' },
    );
    http
      .expectOne((candidate) => candidate.url === '/api/v1/borrowings/me')
      .flush({
        items: [
          {
            id: '30000000-0000-0000-0000-000000000001',
            bookId: '20000000-0000-0000-0000-000000000001',
            bookTitle: 'Dune',
            userId: 'user-1',
            userEmail: 'reader@example.test',
            borrowedAtUtc: '2026-01-01T00:00:00Z',
            dueAtUtc: '2026-01-15T00:00:00Z',
            returnedAtUtc: '2026-01-10T00:00:00Z',
            status: 2,
          },
        ],
        page: 1,
        pageSize: 20,
        totalCount: 1,
        totalPages: 1,
      });
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain(
      'This borrowing has already been returned.',
    );
    expect(fixture.nativeElement.textContent).toContain('Returned');
  });
});
