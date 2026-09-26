import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { AuthApiService } from './auth-api.service';

describe('AuthApiService', () => {
  beforeEach(() =>
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    }),
  );
  afterEach(() => {
    TestBed.inject(HttpTestingController).verify();
    TestBed.resetTestingModule();
  });

  it('maps login and registration to their public authentication endpoints', () => {
    const api = TestBed.inject(AuthApiService);
    api.login({ email: 'reader@example.test', password: 'secret' }).subscribe();
    const login = TestBed.inject(HttpTestingController).expectOne('/api/v1/auth/login');
    expect(login.request.method).toBe('POST');
    login.flush({
      token: 'token',
      expiresAtUtc: new Date(Date.now() + 60_000).toISOString(),
      user: {
        id: 'u1',
        email: 'reader@example.test',
        firstName: null,
        lastName: null,
        roles: ['User'],
      },
    });

    api
      .register({
        email: 'new@example.test',
        password: 'secret',
        firstName: 'New',
        lastName: 'Reader',
      })
      .subscribe();
    const register = TestBed.inject(HttpTestingController).expectOne('/api/v1/auth/register');
    expect(register.request.method).toBe('POST');
    expect(register.request.body.email).toBe('new@example.test');
    register.flush({
      token: 'token',
      expiresAtUtc: new Date(Date.now() + 60_000).toISOString(),
      user: {
        id: 'u2',
        email: 'new@example.test',
        firstName: 'New',
        lastName: 'Reader',
        roles: ['User'],
      },
    });
  });

  it('maps the current-user lookup to the protected me endpoint', () => {
    TestBed.inject(AuthApiService).getCurrentUser().subscribe();
    const request = TestBed.inject(HttpTestingController).expectOne('/api/v1/auth/me');
    expect(request.request.method).toBe('GET');
    request.flush({
      id: 'u1',
      email: 'reader@example.test',
      firstName: null,
      lastName: null,
      roles: ['User'],
    });
  });
});
