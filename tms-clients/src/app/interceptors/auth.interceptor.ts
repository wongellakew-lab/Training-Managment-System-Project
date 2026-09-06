import { HttpInterceptorFn, HttpRequest, HttpHandlerFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { AuthService } from '../services/auth.service';
import { catchError, switchMap, throwError } from 'rxjs';

export const authInterceptor: HttpInterceptorFn = (req: HttpRequest<unknown>, next: HttpHandlerFn) => {
  const authService = inject(AuthService);
  const token = authService.getAccessToken();

  let authReq = req;
  const isAuthEndpoint = req.url.includes('/auth/login') || req.url.includes('/auth/register');

  if (token && !isAuthEndpoint) {
    authReq = req.clone({
      setHeaders: {
        Authorization: `Bearer ${token}`
      }
    });
  }

  return next(authReq).pipe(
    catchError((error: HttpErrorResponse) => {
      // Allow credentials/registration errors to pass directly to login/register forms
      if (isAuthEndpoint) {
        return throwError(() => error);
      }

      // If refresh token request itself fails with 401/400, session cannot be recovered
      if (req.url.includes('/auth/refresh')) {
        authService.logout(true);
        return throwError(() => new Error('Session expired. Please sign in again.'));
      }

      // Handle 401 Unauthorized for authenticated resources
      if (error.status === 401) {
        if (!authService.getRefreshToken()) {
          authService.logout(true);
          return throwError(() => new Error('Session expired. Please sign in again.'));
        }

        // Delegate to mutexed refreshToken() in AuthService
        return authService.refreshToken().pipe(
          switchMap((newAuth) => {
            const newToken = newAuth.token || newAuth.accessToken || '';
            const retryReq = req.clone({
              setHeaders: {
                Authorization: `Bearer ${newToken}`
              }
            });
            return next(retryReq);
          }),
          catchError(() => {
            // AuthService.refreshToken already calls logout(true) on failure
            return throwError(() => new Error('Session expired. Please sign in again.'));
          })
        );
      }

      return throwError(() => error);
    })
  );
};
