import { HttpErrorResponse } from '@angular/common/http';

import { ApiError } from '../../shared/models/api-error.model';

export function getUserFriendlyErrorMessage(
  error: unknown,
  fallbackMessage = 'Something went wrong. Please try again.'
): string {
  console.error('Application error', error);

  const apiError = extractApiError(error);

  if (apiError?.status === 0) {
    return 'The service is temporarily unavailable. Please try again in a moment.';
  }

  if (apiError?.status === 401 || apiError?.status === 403) {
    return 'Your session may have expired. Please sign in again.';
  }

  if (apiError?.status === 400 || apiError?.status === 409) {
    const businessMessage = getBusinessErrorMessage(error, apiError);

    return businessMessage ?? 'Some of the details are invalid. Please check and try again.';
  }

  if (apiError?.status === 404) {
    return 'The requested item could not be found.';
  }

  if (apiError?.status !== undefined && apiError.status >= 500) {
    return 'Something went wrong on our side. Please try again.';
  }

  if (typeof apiError?.message === 'string' && apiError.message.trim()) {
    return sanitizeBusinessMessage(apiError.message) ?? fallbackMessage;
  }

  if (error instanceof HttpErrorResponse) {
    const message = getHttpErrorMessage(error);

    if (message) {
      return message;
    }
  }

  return fallbackMessage;
}

function extractApiError(error: unknown): Partial<ApiError> | null {
  if (!error || typeof error !== 'object') {
    return null;
  }

  const candidate = error as Partial<ApiError> & Record<string, unknown>;
  const status = typeof candidate['status'] === 'number' ? candidate['status'] : undefined;
  const message = typeof candidate['message'] === 'string' ? candidate['message'] : undefined;
  const code = typeof candidate['code'] === 'string' ? candidate['code'] : undefined;

  if (status === undefined && message === undefined && code === undefined) {
    const nestedError = candidate['error'];

    if (nestedError && typeof nestedError === 'object') {
      const nestedCandidate = nestedError as Partial<ApiError> & Record<string, unknown>;
      const nestedStatus = typeof nestedCandidate['status'] === 'number' ? nestedCandidate['status'] : undefined;
      const nestedMessage = typeof nestedCandidate['message'] === 'string' ? nestedCandidate['message'] : undefined;
      const nestedCode = typeof nestedCandidate['code'] === 'string' ? nestedCandidate['code'] : undefined;

      if (nestedStatus !== undefined || nestedMessage !== undefined || nestedCode !== undefined) {
        return {
          status: nestedStatus,
          message: nestedMessage,
          code: nestedCode
        };
      }
    }

    return null;
  }

  return {
    status,
    message,
    code
  };
}

function getBusinessErrorMessage(error: unknown, apiError: Partial<ApiError>): string | null {
  const message = getMessageFromError(error, apiError);

  if (!message) {
    return null;
  }

  return sanitizeBusinessMessage(message);
}

function getMessageFromError(error: unknown, apiError: Partial<ApiError>): string | null {
  if (typeof apiError.message === 'string' && apiError.message.trim()) {
    return apiError.message.trim();
  }

  if (error instanceof HttpErrorResponse) {
    return getHttpErrorMessage(error);
  }

  if (error && typeof error === 'object') {
    const candidate = error as Record<string, unknown>;

    if (typeof candidate['message'] === 'string' && candidate['message'].trim()) {
      return candidate['message'].trim();
    }

    if (candidate['error'] && typeof candidate['error'] === 'object') {
      const nested = candidate['error'] as Record<string, unknown>;

      if (typeof nested['message'] === 'string' && nested['message'].trim()) {
        return nested['message'].trim();
      }

      if (typeof nested['title'] === 'string' && nested['title'].trim()) {
        return nested['title'].trim();
      }
    }
  }

  return null;
}

function getHttpErrorMessage(error: HttpErrorResponse): string | null {
  const backendError = error.error;

  if (typeof backendError === 'string' && backendError.trim()) {
    return sanitizeBusinessMessage(backendError);
  }

  if (backendError && typeof backendError === 'object') {
    const candidate = backendError as Record<string, unknown>;

    if (typeof candidate['message'] === 'string' && candidate['message'].trim()) {
      return sanitizeBusinessMessage(candidate['message']);
    }

    if (typeof candidate['title'] === 'string' && candidate['title'].trim()) {
      return sanitizeBusinessMessage(candidate['title']);
    }
  }

  return null;
}

function sanitizeBusinessMessage(message: string): string | null {
  const normalized = message.trim();

  if (!normalized) {
    return null;
  }

  if (isTechnicalMessage(normalized)) {
    return null;
  }

  return normalized;
}

function isTechnicalMessage(message: string): boolean {
  const normalized = message.toLowerCase();

  return normalized.includes('exception') ||
    normalized.includes('stack') ||
    normalized.includes('sql') ||
    normalized.includes('nullreference') ||
    normalized.includes('object reference') ||
    normalized.includes('http failure response') ||
    normalized.includes('cannot read properties of') ||
    normalized.includes('gemini returned invalid json');
}
