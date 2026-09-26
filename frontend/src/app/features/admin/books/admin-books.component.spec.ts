import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter, Router } from '@angular/router';
import { BehaviorSubject } from 'rxjs';
import { AdminBooksComponent } from './admin-books.component';

describe('AdminBooksComponent', () => {
  let params: BehaviorSubject<ReturnType<typeof convertToParamMap>>;

  beforeEach(() => {
    params = new BehaviorSubject(convertToParamMap({}));
    TestBed.configureTestingModule({
      imports: [AdminBooksComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: ActivatedRoute, useValue: { queryParamMap: params } },
      ],
    });
  });

  afterEach(() => TestBed.inject(HttpTestingController).verify());

  function createAndLoad() {
    const fixture = TestBed.createComponent(AdminBooksComponent);
    fixture.detectChanges();
    TestBed.inject(HttpTestingController)
      .expectOne((request) => request.url === '/api/v1/books')
      .flush({ items: [], page: 1, pageSize: 10, totalCount: 0, totalPages: 0 });
    fixture.detectChanges();
    return fixture;
  }

  function enterSearch(fixture: ReturnType<typeof createAndLoad>, value: string): HTMLInputElement {
    const input = fixture.nativeElement.querySelector('#inventorySearch') as HTMLInputElement;
    input.value = value;
    input.dispatchEvent(new Event('input', { bubbles: true }));
    return input;
  }

  it('adds the trimmed search value to the URL when Search is clicked', () => {
    const fixture = createAndLoad();
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
    enterSearch(fixture, '  test  ');

    const searchButton = fixture.nativeElement.querySelector(
      'button.btn-outline-primary',
    ) as HTMLButtonElement;
    searchButton.click();

    expect(navigate).toHaveBeenCalledWith(
      [],
      expect.objectContaining({
        queryParams: expect.objectContaining({ search: 'test', page: 1 }),
      }),
    );
  });

  it('runs the same search when Enter is pressed in the input', () => {
    const fixture = createAndLoad();
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
    const input = enterSearch(fixture, 'test');

    input.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter', bubbles: true }));

    expect(navigate).toHaveBeenCalledWith(
      [],
      expect.objectContaining({
        queryParams: expect.objectContaining({ search: 'test', page: 1 }),
      }),
    );
  });
});
