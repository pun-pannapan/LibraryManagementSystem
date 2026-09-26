import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { BookDetailComponent } from './book-detail.component';

describe('BookDetailComponent', () => {
  it('treats a malformed book ID as not found without requesting the API', () => {
    TestBed.configureTestingModule({
      imports: [BookDetailComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        {
          provide: ActivatedRoute,
          useValue: { paramMap: of(convertToParamMap({ id: 'invalid' })) },
        },
      ],
    });
    const fixture = TestBed.createComponent(BookDetailComponent);
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('This book could not be found.');
    expect(fixture.nativeElement.textContent).not.toContain('Try again');
    TestBed.inject(HttpTestingController).verify();
  });

  it('accepts SQL Server GUIDs whose version bits are not RFC 4122 values', () => {
    const bookId = '501f75e6-17ae-4c7b-3f10-08df1bc823fa';
    TestBed.configureTestingModule({
      imports: [BookDetailComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        {
          provide: ActivatedRoute,
          useValue: { paramMap: of(convertToParamMap({ id: bookId })) },
        },
      ],
    });
    const fixture = TestBed.createComponent(BookDetailComponent);
    fixture.detectChanges();
    const request = TestBed.inject(HttpTestingController).expectOne(`/api/v1/books/${bookId}`);
    request.flush({
      id: bookId,
      isbn: '9780000000001',
      title: 'Test book',
      author: 'Test author',
      publisher: null,
      publishedYear: 2026,
      categoryId: '10000000-0000-0000-0000-000000000001',
      categoryName: 'Technology',
      availabilityStatus: 1,
      rowVersion: 'AQID',
    });
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Test book');
    TestBed.inject(HttpTestingController).verify();
  });
});
