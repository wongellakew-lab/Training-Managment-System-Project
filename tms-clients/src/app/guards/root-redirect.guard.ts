import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

/**
 * Root route guard — redirects to the authenticated user's role-based home.
 * Unauthenticated users are sent to /login.
 */
export const rootRedirectGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (!authService.isAuthenticated()) {
    return router.createUrlTree(['/login']);
  }

  const role = authService.userRole();
  if (role === 'Admin') {
    return router.createUrlTree(['/admin/dashboard']);
  } else if (role === 'Instructor') {
    return router.createUrlTree(['/instructor/dashboard']);
  }
  return router.createUrlTree(['/student/dashboard']);
};
