import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { TokenStorage } from '../auth/token.storage';
import { ApiError } from '../../shared/models/api-error.model';
export const errorInterceptor: HttpInterceptorFn = (request, next) => {
  const router = inject(Router);
  const tokenStorage = inject(TokenStorage);
  return next(request).pipe(catchError((error: HttpErrorResponse) => {
    const apiError = mapHttpError(error);
    if (apiError.status === 401) { tokenStorage.clearToken(); router.navigate(['/login']); }
    return throwError(() => apiError);
  }));
};
function mapHttpError(error: HttpErrorResponse): ApiError {
  if (error.status === 0) return { status: 0, code: 'NETWORK_ERROR', message: 'Cannot reach the server right now. Please check your connection or try again later.' };
  const backendError = error.error;
  return { status: error.status, code: backendError?.code ?? defaultCode(error.status), message: backendError?.message ?? defaultMessage(error.status), details: backendError?.details };
}
function defaultCode(status: number): string { return ({401:'UNAUTHORIZED',403:'FORBIDDEN',404:'NOT_FOUND',409:'CONFLICT',503:'SERVICE_UNAVAILABLE'} as Record<number,string>)[status] ?? 'API_ERROR'; }
function defaultMessage(status: number): string { return ({401:'Session expired. Please login again.',403:'You do not have permission to perform this action.',404:'The requested resource was not found.',409:'This action conflicts with the current state.',503:'The service is temporarily unavailable. Please try again soon.'} as Record<number,string>)[status] ?? 'Something went wrong. Please try again.'; }
