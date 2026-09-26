import { ActivatedRouteSnapshot, Router, RouterStateSnapshot, UrlTree, provideRouter } from '@angular/router';
import { TestBed } from '@angular/core/testing';
import { AuthService } from './auth.service';
import { authGuard } from './auth.guard';
import { roleGuard } from './role.guard';

describe('route guards', () => {
  let auth: { hasValidSession: ReturnType<typeof vi.fn>; hasRole: ReturnType<typeof vi.fn> };

  beforeEach(() => {
    auth = { hasValidSession: vi.fn(), hasRole: vi.fn() };
    TestBed.configureTestingModule({
      providers: [provideRouter([]), { provide: AuthService, useValue: auth }],
    });
  });

  afterEach(() => TestBed.resetTestingModule());

  it('redirects unauthenticated users to login with their return URL', () => {
    auth.hasValidSession.mockReturnValue(false);
    const guard = TestBed.runInInjectionContext(() => authGuard({} as ActivatedRouteSnapshot, { url: '/my-borrowings' } as RouterStateSnapshot));
    const tree = guard as UrlTree;

    expect(TestBed.inject(Router).serializeUrl(tree)).toContain('/login?returnUrl=');
    expect(tree.queryParams['returnUrl']).toBe('/my-borrowings');
  });

  it('denies a signed-in non-admin and allows an administrator', () => {
    auth.hasValidSession.mockReturnValue(true);
    const route = { data: { roles: ['Administrator'] } } as unknown as ActivatedRouteSnapshot;
    auth.hasRole.mockReturnValue(false);
    const denied = TestBed.runInInjectionContext(() => roleGuard(route, { url: '/admin/books' } as RouterStateSnapshot));
    expect(TestBed.inject(Router).serializeUrl(denied as UrlTree)).toBe('/forbidden');

    auth.hasRole.mockReturnValue(true);
    const allowed = TestBed.runInInjectionContext(() => roleGuard(route, { url: '/admin/books' } as RouterStateSnapshot));
    expect(allowed).toBe(true);
  });
});
