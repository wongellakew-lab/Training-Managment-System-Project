import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';
import { map, catchError, of } from 'rxjs';

export const authGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (authService.isAuthenticated()) {
    return true;
  }

  // If token is expired but a refresh token exists, attempt proactive refresh
  if (authService.getRefreshToken() && authService.currentUser()) {
    return authService.refreshToken().pipe(
      map(() => true),
      catchError(() => {
        authService.logout(true);
        return of(
          router.createUrlTree(['/login'], {
            queryParams: { returnUrl: state.url, sessionExpired: 'true' }
          })
        );
      })
    );
  }

  return router.createUrlTree(['/login'], {
    queryParams: { returnUrl: state.url }
  });
};
