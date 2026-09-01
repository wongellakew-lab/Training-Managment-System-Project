import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';
import { UserRole } from '../models/user.model';

export const roleGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (!authService.isAuthenticated()) {
    return router.createUrlTree(['/login'], {
      queryParams: { returnUrl: state.url }
    });
  }

  const expectedRoles = route.data?.['roles'] as UserRole[] | undefined;
  const currentRole = authService.userRole();

  if (!expectedRoles || (currentRole && expectedRoles.includes(currentRole))) {
    return true;
  }

  return router.createUrlTree(['/unauthorized']);
};
