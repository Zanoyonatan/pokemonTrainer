import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';

import { TokenStorage } from '../auth/token.storage';
import { ApiError } from '../../shared/models/api-error.model';

export const errorInterceptor: HttpInterceptorFn = (request, next) => {
  const router = inject(Router);
  const tokenStorage = inject(TokenStorage);

  return next(request).pipe(
    catchError((error: HttpErrorResponse) => {
      console.error('HTTP request failed', {
        url: request.url,
        method: request.method,
        status: error.status,
        error
      });

      const apiError = mapHttpError(error);

      if (apiError.status === 401) {
        tokenStorage.clearToken();
        router.navigate(['/login']);
      }

      return throwError(() => apiError);
    })
  );
};

function mapHttpError(error: HttpErrorResponse): ApiError {
  if (error.status === 0) {
    return {
      status: 0,
      code: 'NETWORK_ERROR',
      message: 'Cannot reach the server right now. Please check your connection or try again later.'
    };
  }

  const backendError = error.error;
  const validationMessage = extractValidationMessage(backendError);

  return {
    status: error.status,
    code: backendError?.errorCode ?? backendError?.code ?? getDefaultCode(error.status),
    message:
      validationMessage ??
      backendError?.message ??
      backendError?.title ??
      getDefaultMessage(error.status),
    details: backendError?.details,
    traceId: backendError?.traceId
  };
}

function extractValidationMessage(errorBody: unknown): string | null {
  if (!isObject(errorBody) || !isObject(errorBody['errors'])) {
    return null;
  }

  const errors = errorBody['errors'];
  const messages: string[] = [];

  for (const fieldName of Object.keys(errors)) {
    const fieldErrors = errors[fieldName];

    if (Array.isArray(fieldErrors)) {
      for (const fieldError of fieldErrors) {
        messages.push(`${fieldName}: ${fieldError}`);
      }
    }
  }

  return messages.length > 0
    ? messages.join(' ')
    : null;
}

function isObject(value: unknown): value is Record<string, any> {
  return typeof value === 'object' && value !== null;
}

function getDefaultCode(status: number): string {
  switch (status) {
    case 400:
      return 'BAD_REQUEST';
    case 401:
      return 'UNAUTHORIZED';
    case 403:
      return 'FORBIDDEN';
    case 404:
      return 'NOT_FOUND';
    case 409:
      return 'CONFLICT';
    case 503:
      return 'SERVICE_UNAVAILABLE';
    default:
      return 'API_ERROR';
  }
}

function getDefaultMessage(status: number): string {
  switch (status) {
    case 400:
      return 'The request is invalid. Please check the form fields.';
    case 401:
      return 'Session expired. Please login again.';
    case 403:
      return 'You do not have permission to perform this action.';
    case 404:
      return 'The requested resource was not found.';
    case 409:
      return 'This action conflicts with the current state.';
    case 503:
      return 'The service is temporarily unavailable. Please try again soon.';
    default:
      return 'Something went wrong. Please try again.';
  }
}