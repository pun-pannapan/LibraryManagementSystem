import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { AuthService } from '../auth/auth.service';
import { authInterceptor } from './auth.interceptor';

describe('authInterceptor', () => {
  let token: string | null;

  beforeEach(() => {
    token = 'unit-test-token';
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
        { provide: AuthService, useValue: { getToken: () => token } },
      ],
    });
  });

  afterEach(() => {
    TestBed.inject(HttpTestingController).verify();
    TestBed.resetTestingModule();
  });

  it('attaches a bearer token only to API requests', () => {
    const http = TestBed.inject(HttpTestingController);
    const client = TestBed.inject(HttpClient);
    client.get('/api/v1/books').subscribe();
    const apiRequest = http.expectOne('/api/v1/books');
    expect(apiRequest.request.headers.get('Authorization')).toBe('Bearer unit-test-token');
    apiRequest.flush({});

    client.get('/assets/example.json').subscribe();
    const assetRequest = http.expectOne('/assets/example.json');
    expect(assetRequest.request.headers.has('Authorization')).toBe(false);
    assetRequest.flush({});

    token = null;
    client.get('/api/v1/health').subscribe();
    const anonymousApiRequest = http.expectOne('/api/v1/health');
    expect(anonymousApiRequest.request.headers.has('Authorization')).toBe(false);
    anonymousApiRequest.flush({});
  });
});
