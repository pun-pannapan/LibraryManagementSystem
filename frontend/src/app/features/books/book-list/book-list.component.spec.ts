import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter, Router } from '@angular/router';
import { BehaviorSubject, of } from 'rxjs';
import { BookListComponent } from './book-list.component';

describe('BookListComponent', () => {
  let http: HttpTestingController;
  let router: { navigate: ReturnType<typeof vi.fn> };
  const route = { queryParamMap: of(convertToParamMap({})) };

  beforeEach(() => {
    router = { navigate: vi.fn().mockResolvedValue(true) };
    TestBed.configureTestingModule({
      imports: [BookListComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: ActivatedRoute, useValue: route },
        { provide: Router, useValue: router },
      ],
    });
  });

  afterEach(() => {
    http.verify();
    TestBed.resetTestingModule();
  });

  function createAndLoad(): ComponentFixture<BookListComponent> {
    http = TestBed.inject(HttpTestingController);
    const fixture = TestBed.createComponent(BookListComponent);
    fixture.detectChanges();
    http
      .expectOne('/api/v1/categories')
      .flush([{ id: 1, name: 'Science Fiction', description: null }]);
    http
      .expectOne((request) => request.url === '/api/v1/books')
      .flush({
        items: [
          {
            id: 7,
            isbn: '9780000000001',
            title: 'Dune',
            author: 'Frank Herbert',
            publisher: null,
            publishedYear: 1965,
            categoryId: 1,
            categoryName: 'Science Fiction',
            availabilityStatus: 1,
            rowVersion: 'AQID',
          },
        ],
        page: 1,
        pageSize: 20,
        totalCount: 1,
        totalPages: 1,
      });
    fixture.detectChanges();
    return fixture;
  }

  it('renders books returned by the API', () => {
    const fixture = createAndLoad();
    expect(fixture.nativeElement.textContent).toContain('Dune');
    expect(fixture.nativeElement.textContent).toContain('Available');
  });

  it('builds the search query from the filter form', () => {
    const fixture = createAndLoad();
    const search = fixture.nativeElement.querySelector('#search') as HTMLInputElement;
    search.value = '  dune  ';
    search.dispatchEvent(new Event('input', { bubbles: true }));
    const author = fixture.nativeElement.querySelector('#author') as HTMLInputElement;
    author.value = 'Herbert';
    author.dispatchEvent(new Event('input', { bubbles: true }));
    const available = fixture.nativeElement.querySelector('#available') as HTMLSelectElement;
    available.value = 'true';
    available.dispatchEvent(new Event('change', { bubbles: true }));
    const category = fixture.nativeElement.querySelector('#categoryId') as HTMLSelectElement;
    category.value = '1';
    category.dispatchEvent(new Event('change', { bubbles: true }));
    fixture.detectChanges();
    (fixture.nativeElement.querySelector('form') as HTMLFormElement).dispatchEvent(
      new Event('submit', { bubbles: true, cancelable: true }),
    );

    expect(router.navigate).toHaveBeenCalledWith(
      [],
      expect.objectContaining({
        queryParams: expect.objectContaining({
          search: 'dune',
          author: 'Herbert',
          categoryId: 1,
          available: true,
          page: 1,
        }),
      }),
    );
  });

  it('cancels an old search and keeps loading until the new request finishes', () => {
    const params = new BehaviorSubject(convertToParamMap({}));
    TestBed.overrideProvider(ActivatedRoute, { useValue: { queryParamMap: params } });
    http = TestBed.inject(HttpTestingController);
    const fixture = TestBed.createComponent(BookListComponent);
    fixture.detectChanges();
    http.expectOne('/api/v1/categories').flush([]);
    const firstRequest = http.expectOne((request) => request.url === '/api/v1/books');

    params.next(convertToParamMap({ search: 'new query' }));
    fixture.detectChanges();
    expect(firstRequest.cancelled).toBe(true);
    expect(fixture.nativeElement.textContent).toContain('Loading books');
    const currentRequest = http.expectOne((request) => request.url === '/api/v1/books');
    expect(currentRequest.request.params.get('search')).toBe('new query');
    currentRequest.flush({ items: [], page: 1, pageSize: 20, totalCount: 0, totalPages: 0 });
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('No books found');
  });

  it('shows a safe conflict message when the book became unavailable', () => {
    const fixture = createAndLoad();
    (
      fixture.nativeElement.querySelector('button[aria-label="Borrow Dune"]') as HTMLButtonElement
    ).click();
    fixture.detectChanges();
    const buttons = fixture.nativeElement.querySelectorAll(
      'button',
    ) as NodeListOf<HTMLButtonElement>;
    const confirmButton = Array.from(buttons).find((button: HTMLButtonElement) =>
      button.textContent?.includes('Confirm borrow'),
    );
    expect(confirmButton).toBeDefined();
    confirmButton?.click();
    fixture.detectChanges();

    const request = http.expectOne('/api/v1/borrowings');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ bookId: 7 });
    request.flush(
      { message: 'Book is no longer available.' },
      { status: 409, statusText: 'Conflict' },
    );
    http
      .expectOne((candidate) => candidate.url === '/api/v1/books')
      .flush({
        items: [],
        page: 1,
        pageSize: 20,
        totalCount: 0,
        totalPages: 0,
      });
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Book is no longer available.');
  });
});
