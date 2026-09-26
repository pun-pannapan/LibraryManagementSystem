import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { apiEndpoints } from '../config/api-endpoints';
import { AuthService } from '../auth/auth.service';
import { environment } from '../../../environments/environment';

export const errorInterceptor: HttpInterceptorFn = (request, next) => {
  const auth = inject(AuthService);
  const router = inject(Router);
  const apiBaseUrl = environment.apiBaseUrl;
  const isApiRequest = request.url === apiBaseUrl || request.url.startsWith(`${apiBaseUrl}/`);
  const isPublicAuthRequest =
    request.url === `${apiEndpoints.auth}/login` || request.url === `${apiEndpoints.auth}/register`;

  return next(request).pipe(
    catchError((error: unknown) => {
      if (
        isApiRequest &&
        error instanceof HttpErrorResponse &&
        error.status === 401 &&
        !isPublicAuthRequest
      ) {
        auth.logout();
        void router.navigate(['/login'], {
          queryParams: { returnUrl: router.url },
        });
      } else if (isApiRequest && error instanceof HttpErrorResponse && error.status === 403) {
        void router.navigate(['/forbidden']);
      }

      // Keep feature errors (especially 400/404/409) available to their callers.
      return throwError(() => error);
    }),
  );
};
