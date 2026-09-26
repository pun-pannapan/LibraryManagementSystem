import { authGuard } from './core/auth/auth.guard';
import { roleGuard } from './core/auth/role.guard';
import { routes } from './app.routes';
import { RouteMessageComponent } from './shared/components/route-message/route-message.component';

describe('application route policy', () => {
  it('keeps login public and protects user pages with authentication', () => {
    const login = routes.find((route) => route.path === 'login');
    const books = routes.find((route) => route.path === 'books');
    const detail = routes.find((route) => route.path === 'books/:id');
    const borrowings = routes.find((route) => route.path === 'my-borrowings');

    expect(login?.canActivate).toBeUndefined();
    expect(books?.canActivate).toContain(authGuard);
    expect(detail?.canActivate).toContain(authGuard);
    expect(borrowings?.canActivate).toContain(authGuard);
  });

  it('protects every administrator child route and exposes the recommended paths', () => {
    const admin = routes.find((route) => route.path === 'admin');
    expect(admin?.canActivate).toEqual([authGuard, roleGuard]);
    expect(admin?.canActivateChild).toContain(roleGuard);
    expect(admin?.data?.['roles']).toEqual(['Administrator']);
    expect(admin?.children?.map((route) => route.path)).toEqual(expect.arrayContaining([
      'books', 'books/new', 'books/:id/edit', 'transactions',
    ]));

    const forbidden = routes.find((route) => route.path === 'forbidden');
    const notFound = routes.find((route) => route.path === 'not-found');
    expect(forbidden?.component).toBe(RouteMessageComponent);
    expect(notFound?.component).toBe(RouteMessageComponent);
    expect(routes.at(-1)?.path).toBe('**');
  });
});
