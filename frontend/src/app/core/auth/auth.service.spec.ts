import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { AuthService } from './auth.service';

describe('AuthService', () => {
  const storageKey = 'library-management.auth';

  beforeEach(() => {
    sessionStorage.removeItem(storageKey);
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
  });

  afterEach(() => {
    TestBed.inject(HttpTestingController).verify();
    TestBed.resetTestingModule();
    sessionStorage.removeItem(storageKey);
  });

  it('stores the session after login and exposes the user role', () => {
    const auth = TestBed.inject(AuthService);
    let completed = false;
    auth.login({ email: 'admin@example.test', password: 'secret' }).subscribe(() => {
      completed = true;
    });

    const request = TestBed.inject(HttpTestingController).expectOne('/api/v1/auth/login');
    expect(request.request.method).toBe('POST');
    request.flush({
      token: 'test-token',
      expiresAtUtc: new Date(Date.now() + 60_000).toISOString(),
      user: {
        id: 'user-1',
        email: 'admin@example.test',
        firstName: 'Library',
        lastName: 'Admin',
        roles: ['Administrator'],
      },
    });

    expect(completed).toBe(true);
    expect(auth.isAuthenticated()).toBe(true);
    expect(auth.getToken()).toBe('test-token');
    expect(auth.currentUser()?.email).toBe('admin@example.test');
    expect(auth.hasRole('Administrator')).toBe(true);
    expect(JSON.parse(sessionStorage.getItem(storageKey) ?? 'null').token).toBe('test-token');
  });

  it('clears authentication state and persisted session on logout', () => {
    sessionStorage.setItem(
      storageKey,
      JSON.stringify({
        token: 'token',
        expiresAtUtc: new Date(Date.now() + 60_000).toISOString(),
        user: {
          id: 'user-1',
          email: 'user@example.test',
          firstName: null,
          lastName: null,
          roles: [],
        },
      }),
    );
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });

    const auth = TestBed.inject(AuthService);
    expect(auth.isAuthenticated()).toBe(true);
    auth.logout();

    expect(auth.isAuthenticated()).toBe(false);
    expect(auth.getToken()).toBeNull();
    expect(sessionStorage.getItem(storageKey)).toBeNull();
  });

  it('rejects an expired restored session and removes it from storage', () => {
    sessionStorage.setItem(
      storageKey,
      JSON.stringify({
        token: 'expired-token',
        expiresAtUtc: new Date(Date.now() - 60_000).toISOString(),
        user: {
          id: 'user-1',
          email: 'user@example.test',
          firstName: null,
          lastName: null,
          roles: [],
        },
      }),
    );

    const auth = TestBed.inject(AuthService);
    expect(auth.isAuthenticated()).toBe(false);
    expect(auth.hasValidSession()).toBe(false);
    expect(auth.getToken()).toBeNull();
    expect(sessionStorage.getItem(storageKey)).toBeNull();
  });

  it.each([
    { token: '', roles: ['User'] },
    { token: 'token', roles: [1] },
  ])('rejects a malformed stored session: %j', ({ token, roles }) => {
    sessionStorage.setItem(
      storageKey,
      JSON.stringify({
        token,
        expiresAtUtc: new Date(Date.now() + 60_000).toISOString(),
        user: {
          id: 'user-1',
          email: 'reader@example.test',
          firstName: null,
          lastName: null,
          roles,
        },
      }),
    );

    const auth = TestBed.inject(AuthService);
    expect(auth.isAuthenticated()).toBe(false);
    expect(auth.getToken()).toBeNull();
    expect(sessionStorage.getItem(storageKey)).toBeNull();
  });
});
