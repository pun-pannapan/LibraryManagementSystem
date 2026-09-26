import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth.service';

export const roleGuard: CanActivateFn = (route, state) => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (!auth.hasValidSession()) {
    return router.createUrlTree(['/login'], {
      queryParams: { returnUrl: state.url },
    });
  }

  const allowedRoles = route.data['roles'] as string[] | undefined;
  if (!allowedRoles?.length || allowedRoles.some((role) => auth.hasRole(role))) {
    return true;
  }

  return router.createUrlTree(['/forbidden']);
};
