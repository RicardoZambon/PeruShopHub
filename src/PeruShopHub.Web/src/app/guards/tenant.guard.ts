import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

export const tenantGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (auth.hasTenant()) return true;
  if (auth.isSuperAdmin()) return true;

  router.navigate(['/login']);
  return false;
};
