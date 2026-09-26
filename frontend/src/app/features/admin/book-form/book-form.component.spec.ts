import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, provideRouter } from '@angular/router';
import { BookFormComponent } from './book-form.component';

describe('BookFormComponent', () => {
  beforeEach(() =>
    TestBed.configureTestingModule({
      imports: [BookFormComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: { data: { mode: 'create' }, paramMap: { get: () => null }, queryParams: {} },
          },
        },
      ],
    }),
  );

  afterEach(() => {
    TestBed.inject(HttpTestingController).verify();
    TestBed.resetTestingModule();
  });

  it('rejects required fields before making an API request', () => {
    const fixture = TestBed.createComponent(BookFormComponent);
    fixture.detectChanges();
    TestBed.inject(HttpTestingController)
      .expectOne('/api/v1/categories')
      .flush([{ id: 1, name: 'Fiction', description: null }]);
    fixture.detectChanges();
    (fixture.nativeElement.querySelector('button[type="submit"]') as HTMLButtonElement).click();
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelectorAll('.invalid-feedback').length).toBeGreaterThan(0);
    expect(fixture.nativeElement.textContent).toContain('This field is required.');
    TestBed.inject(HttpTestingController).expectNone('/api/v1/books');
  });

  it('rejects whitespace-only text before submitting a book', () => {
    const fixture = TestBed.createComponent(BookFormComponent);
    fixture.detectChanges();
    const http = TestBed.inject(HttpTestingController);
    http.expectOne('/api/v1/categories').flush([{ id: 1, name: 'Fiction', description: null }]);
    fixture.detectChanges();

    for (const field of ['isbn', 'title', 'author']) {
      const input = fixture.nativeElement.querySelector('#' + field) as HTMLInputElement;
      input.value = '   ';
      input.dispatchEvent(new Event('input', { bubbles: true }));
    }
    const category = fixture.nativeElement.querySelector('#categoryId') as HTMLSelectElement;
    category.value = '1';
    category.dispatchEvent(new Event('change', { bubbles: true }));
    (fixture.nativeElement.querySelector('button[type="submit"]') as HTMLButtonElement).click();
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelectorAll('.invalid-feedback').length).toBe(3);
    http.expectNone('/api/v1/books');
  });
});
