import { HttpInterceptorFn, HttpRequest, HttpHandlerFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';

export const errorInterceptor: HttpInterceptorFn = (req: HttpRequest<unknown>, next: HttpHandlerFn) => {
  const router = inject(Router);

  return next(req).pipe(
    catchError((error: unknown) => {
      if (error instanceof HttpErrorResponse) {
        // Suppress generic alerts for 401 since auth flow handles session expiration
        if (error.status === 401) {
          const sessionMsg = error.error?.message || 'Session expired. Please sign in again.';
          return throwError(() => new Error(sessionMsg));
        }

        if (error.status === 403) {
          router.navigate(['/unauthorized']);
        }

        let errorMessage = 'An unexpected error occurred';

        if (error.error instanceof ErrorEvent) {
          errorMessage = `Network Error: ${error.error.message}`;
        } else if (error.error && typeof error.error === 'object') {
          if (error.error.detail) {
            errorMessage = error.error.detail;
          } else if (error.error.title) {
            errorMessage = error.error.title;
          } else if (error.error.message) {
            errorMessage = error.error.message;
          }
        } else if (typeof error.error === 'string') {
          errorMessage = error.error;
        }

        console.error(`[HTTP Error ${error.status}]:`, errorMessage, error);
        return throwError(() => new Error(errorMessage));
      }

      return throwError(() => error);
    })
  );
};
