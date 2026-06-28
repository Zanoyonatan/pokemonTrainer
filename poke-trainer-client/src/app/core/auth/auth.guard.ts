import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';

import { TokenStorage } from './token.storage';

export const authGuard: CanActivateFn = () => {
  const tokenStorage = inject(TokenStorage);
  const router = inject(Router);

  return tokenStorage.hasToken()
    ? true
    : router.createUrlTree(['/login']);
};
