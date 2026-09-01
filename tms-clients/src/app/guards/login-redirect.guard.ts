import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

export const loginRedirectGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (!authService.isAuthenticated()) {
    return true;
  }

  // Already authenticated → send to role-based home
  const role = authService.userRole();
  if (role === 'Admin') {
    return router.createUrlTree(['/admin/dashboard']);
  } else if (role === 'Instructor') {
    return router.createUrlTree(['/instructor/dashboard']);
  }
  return router.createUrlTree(['/student/dashboard']);
};
