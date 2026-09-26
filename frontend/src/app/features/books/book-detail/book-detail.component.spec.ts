import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { BookDetailComponent } from './book-detail.component';

describe('BookDetailComponent', () => {
  it('treats a non-numeric book ID as not found without requesting the API', () => {
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
});
