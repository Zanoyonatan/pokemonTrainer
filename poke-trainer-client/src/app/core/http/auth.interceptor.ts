import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';

import { TokenStorage } from '../auth/token.storage';

export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const tokenStorage = inject(TokenStorage);
  const token = tokenStorage.getToken();

  if (!token) {
    return next(request);
  }

  return next(
    request.clone({
      setHeaders: {
        Authorization: `Bearer ${token}`
      }
    })
  );
};
