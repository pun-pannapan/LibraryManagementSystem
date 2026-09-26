import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { App } from './app.component';

describe('API status', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [App],
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });
  });

  afterEach(() => TestBed.inject(HttpTestingController).verify());

  it('shows checking followed by online when the health request succeeds', () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('.status-row').textContent).toContain('checking');

    const request = TestBed.inject(HttpTestingController).expectOne('/api/v1/health');
    expect(request.request.method).toBe('GET');
    request.flush({ status: 'Healthy' });
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('.status-row.online').textContent).toContain('online');
  });

  it('shows offline when the database health check fails', () => {
    const fixture = TestBed.createComponent(App);
    TestBed.inject(HttpTestingController).expectOne('/api/v1/health')
      .flush({ status: 'Unhealthy' }, { status: 503, statusText: 'Service Unavailable' });
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('.status-row.offline').textContent).toContain('offline');
  });

  it('shows offline when the API cannot be reached', () => {
    const fixture = TestBed.createComponent(App);
    TestBed.inject(HttpTestingController).expectOne('/api/v1/health')
      .error(new ProgressEvent('error'));
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('.status-row.offline').textContent).toContain('offline');
  });
});
