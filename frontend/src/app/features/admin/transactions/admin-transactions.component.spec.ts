import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter, Router } from '@angular/router';
import { BehaviorSubject } from 'rxjs';
import { AdminTransactionsComponent } from './admin-transactions.component';

describe('AdminTransactionsComponent', () => {
  let params: BehaviorSubject<ReturnType<typeof convertToParamMap>>;

  beforeEach(() => {
    params = new BehaviorSubject(convertToParamMap({}));
    TestBed.configureTestingModule({
      imports: [AdminTransactionsComponent],
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
    const fixture = TestBed.createComponent(AdminTransactionsComponent);
    fixture.detectChanges();
    const request = TestBed.inject(HttpTestingController).expectOne(
      (candidate) => candidate.url === '/api/v1/borrowings',
    );
    request.flush({ items: [], page: 1, pageSize: 20, totalCount: 0, totalPages: 0 });
    fixture.detectChanges();
    return { fixture, request };
  }

  it('sends inclusive UTC date boundaries for the selected days', () => {
    params.next(convertToParamMap({ borrowedFrom: '2026-09-01', borrowedTo: '2026-09-26' }));
    const { request } = createAndLoad();
    expect(request.request.params.get('borrowedFrom')).toBe('2026-09-01T00:00:00Z');
    expect(request.request.params.get('borrowedTo')).toBe('2026-09-26T23:59:59.9999999Z');
  });

  it('rejects an inverted date range before navigating', () => {
    const { fixture } = createAndLoad();
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
    for (const [field, value] of [
      ['borrowedFrom', '2026-09-26'],
      ['borrowedTo', '2026-09-01'],
    ]) {
      const input = fixture.nativeElement.querySelector('#' + field) as HTMLInputElement;
      input.value = value;
      input.dispatchEvent(new Event('input', { bubbles: true }));
    }
    (fixture.nativeElement.querySelector('form') as HTMLFormElement).dispatchEvent(
      new Event('submit', { bubbles: true, cancelable: true }),
    );
    fixture.detectChanges();
    expect(navigate).not.toHaveBeenCalled();
    expect(fixture.nativeElement.textContent).toContain(
      'The end date must be on or after the start date.',
    );
  });

  it('applies a text search across users and books', () => {
    const { fixture } = createAndLoad();
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
    const input = fixture.nativeElement.querySelector('#search') as HTMLInputElement;
    input.value = 'clean code';
    input.dispatchEvent(new Event('input', { bubbles: true }));
    (fixture.nativeElement.querySelector('form') as HTMLFormElement).dispatchEvent(
      new Event('submit', { bubbles: true, cancelable: true }),
    );
    expect(navigate).toHaveBeenCalledWith(
      [],
      expect.objectContaining({
        queryParams: expect.objectContaining({ search: 'clean code', page: 1 }),
      }),
    );
  });

  it('clears loaded filters in both the form and the URL', () => {
    params.next(convertToParamMap({ status: 'Returned', search: 'clean' }));
    const { fixture } = createAndLoad();
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
    const buttons = fixture.nativeElement.querySelectorAll(
      'button',
    ) as NodeListOf<HTMLButtonElement>;
    const clear = Array.from(buttons).find((button) => button.textContent?.trim() === 'Clear');
    expect(clear).toBeDefined();
    clear?.click();
    fixture.detectChanges();
    expect((fixture.nativeElement.querySelector('#status') as HTMLSelectElement).value).toBe('');
    expect((fixture.nativeElement.querySelector('#search') as HTMLInputElement).value).toBe('');
    expect(navigate).toHaveBeenCalledWith(
      [],
      expect.objectContaining({
        queryParams: expect.objectContaining({ status: null, search: null, page: 1 }),
      }),
    );
  });
});
