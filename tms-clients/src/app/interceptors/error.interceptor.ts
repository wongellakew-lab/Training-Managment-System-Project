import { HttpInterceptorFn, HttpRequest, HttpHandlerFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';

export const errorInterceptor: HttpInterceptorFn = (req: HttpRequest<unknown>, next: HttpHandlerFn) => {
  const router = inject(Router);

  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      let errorMessage = 'An unexpected error occurred';

      if (error.error instanceof ErrorEvent) {
        // Client-side or network error
        errorMessage = `Network Error: ${error.error.message}`;
      } else {
        // Backend returned an error response code
        if (error.status === 403) {
          router.navigate(['/unauthorized']);
        }

        if (error.error && typeof error.error === 'object') {
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
      }

      console.error(`[HTTP Error ${error.status}]:`, errorMessage, error);
      return throwError(() => new Error(errorMessage));
    })
  );
};
