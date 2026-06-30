# Error Handling UX Guidelines — PokéTrainer AI

## Goal

Make all user-facing errors in the system friendly, clear, and non-technical.  
The application must not crash because of unhandled errors, and technical error details must be written to the developer console on the client side and to application logs on the server side.

Core rule:

> The user sees a simple and helpful message.  
> The developer sees the technical details in the console or logs.

---

## Mandatory Principles

### 1. Do not use `alert`

Do not use:

```ts
alert(...);
```

Use one of the following instead:

- `ErrorState` for a full-page or content-area error.
- Inline error messages inside the relevant screen.
- Toast / Snackbar in the future, if such a component is added.
- `console.error(...)` for technical details.

Bad example:

```ts
alert(error.message);
```

Good example:

```ts
console.error('Failed to load catalog', error);
this.errorMessage.set('We could not load the Pokémon catalog. Please try again.');
```

---

## 2. Do not show technical messages to the user

Do not show messages such as:

```text
Http failure response for https://localhost:7280/api/pokemon: 500 Internal Server Error
Cannot read properties of undefined
SQL timeout
Object reference not set to an instance of an object
Gemini returned invalid JSON
```

Show a friendly message instead:

```text
We could not load this data right now. Please try again.
```

At the same time, log the technical details for developers:

```ts
console.error('Catalog API failed', error);
```

---

## 3. Prevent application crashes

Every API call must handle `error`.

Do not leave `subscribe` calls without an `error` handler.

Bad example:

```ts
this.catalogService.getPokemon(...).subscribe(result => {
  this.pokemons.set(result.items);
});
```

Good example:

```ts
this.catalogService.getPokemon(...).subscribe({
  next: result => {
    this.pokemons.set(result.items);
  },
  error: error => {
    console.error('Failed to load Pokémon catalog', error);
    this.errorMessage.set('We could not load the Pokémon catalog. Please try again.');
  }
});
```

---

# Existing Tools to Use

## Angular Client

### 1. `ErrorState`

The project already has an existing component:

```text
src/app/shared/components/error-state
```

Use it for friendly error display in screens such as:

- Catalog
- Pokémon Details
- Dream Team
- Team Analyzer
- Smart Search
- Auth pages

Example:

```html
@if (errorMessage(); as message) {
  <app-error-state
    title="Catalog unavailable"
    [message]="message"
    (retry)="loadCatalog()"
  />
}
```

### 2. Angular `ErrorHandler`

Angular provides a built-in global error handling mechanism:

```ts
ErrorHandler
```

Recommended: add a `GlobalErrorHandler` that catches unhandled client errors and writes them to the console.

### 3. Angular `HttpInterceptor`

Angular supports HTTP interceptors for centralized HTTP error handling.

Recommended: create an `HttpErrorInterceptor` that:

- Logs every HTTP error with `console.error`.
- Maps common HTTP status codes to friendly user messages.
- Does not show `alert`.
- Does not expose stack traces or technical details to the user.

---

# Error Message Policy by Error Type

## Server unavailable / network issue

Common statuses:

```text
0
503
504
```

User message:

```text
The service is temporarily unavailable. Please try again in a moment.
```

Console:

```ts
console.error('Service unavailable', error);
```

---

## Authorization / session issue

Statuses:

```text
401
403
```

User message:

```text
Your session may have expired. Please sign in again.
```

Recommended action:

- Clear the token if needed.
- Navigate to `/login`.
- Do not show a stack trace.

---

## Validation / business error

Statuses:

```text
400
409
```

If the server returns a friendly business message, it can be displayed.

Good examples:

```text
You can add up to 5 Pokémon to your Dream Team.
This Pokémon is already in your Dream Team.
```

If the message is technical, replace it with a generic message:

```text
Some of the details are invalid. Please check and try again.
```

---

## Internal server error

Status:

```text
500
```

User message:

```text
Something went wrong on our side. Please try again.
```

Console:

```ts
console.error('Internal server error', error);
```

---

## AI / Gemini failure

Do not show messages such as:

```text
Gemini returned invalid JSON
AI parser failed
```

Show this instead:

```text
Smart Search is temporarily unavailable. Please try a simpler search or try again later.
```

Or, if fallback logic was used:

```text
We used a basic search because AI interpretation was unavailable.
```

---

# Recommended Client-Side Error Mapping

Create this file:

```text
src/app/core/errors/user-friendly-error-message.ts
```

```ts
import { HttpErrorResponse } from '@angular/common/http';

export function getUserFriendlyErrorMessage(
  error: unknown,
  fallbackMessage = 'Something went wrong. Please try again.'
): string {
  if (error instanceof HttpErrorResponse) {
    if (error.status === 0) {
      return 'The server is unreachable. Please check your connection or try again later.';
    }

    if (error.status === 401 || error.status === 403) {
      return 'Your session may have expired. Please sign in again.';
    }

    if (error.status === 400 || error.status === 409) {
      return getBusinessErrorMessage(error) ??
        'Some of the details are invalid. Please check and try again.';
    }

    if (error.status === 404) {
      return 'The requested item could not be found.';
    }

    if (error.status >= 500) {
      return 'Something went wrong on our side. Please try again.';
    }
  }

  return fallbackMessage;
}

function getBusinessErrorMessage(error: HttpErrorResponse): string | null {
  const message = error.error?.message ?? error.error?.Message;

  if (typeof message !== 'string' || !message.trim()) {
    return null;
  }

  if (isTechnicalMessage(message)) {
    return null;
  }

  return message;
}

function isTechnicalMessage(message: string): boolean {
  const normalized = message.toLowerCase();

  return normalized.includes('exception') ||
    normalized.includes('stack') ||
    normalized.includes('sql') ||
    normalized.includes('nullreference') ||
    normalized.includes('object reference') ||
    normalized.includes('http failure response');
}
```

---

# Recommended HTTP Interceptor

Create this file:

```text
src/app/core/interceptors/http-error.interceptor.ts
```

```ts
import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { catchError, throwError } from 'rxjs';

export const httpErrorInterceptor: HttpInterceptorFn = (request, next) => {
  return next(request).pipe(
    catchError((error: HttpErrorResponse) => {
      console.error('HTTP request failed', {
        url: request.url,
        method: request.method,
        status: error.status,
        error
      });

      return throwError(() => error);
    })
  );
};
```

Important: the interceptor should not display messages to the user by itself.  
The relevant screen should decide which friendly message to show.

---

# Recommended Global Error Handler

Create this file:

```text
src/app/core/errors/global-error-handler.ts
```

```ts
import { ErrorHandler, Injectable } from '@angular/core';

@Injectable()
export class GlobalErrorHandler implements ErrorHandler {
  handleError(error: unknown): void {
    console.error('Unhandled client error', error);
  }
}
```

Register it in the providers:

```ts
{
  provide: ErrorHandler,
  useClass: GlobalErrorHandler
}
```

Purpose: make sure unhandled client errors are printed to the console and do not remain silent.

---

# Examples by Screen

## Catalog

Error while loading the catalog:

```ts
error: error => {
  console.error('Failed to load Pokémon catalog', error);
  this.errorMessage.set(
    getUserFriendlyErrorMessage(
      error,
      'We could not load the Pokémon catalog. Please try again.'
    )
  );
}
```

User message:

```text
We could not load the Pokémon catalog. Please try again.
```

---

## Pokémon Details

Error while loading details:

```text
We could not load this Pokémon. Please try again.
```

If 404:

```text
This Pokémon could not be found.
```

---

## Dream Team

Failed to add Pokémon:

```text
We could not add this Pokémon to your Dream Team.
```

If the team is full:

```text
Your Dream Team is full. Remove a Pokémon before adding another one.
```

If the Pokémon is already in the team:

```text
This Pokémon is already in your Dream Team.
```

---

## Team Analyzer

Failed to analyze:

```text
We could not analyze your Dream Team right now. Please try again.
```

If the team is empty:

```text
Add Pokémon to your Dream Team before running the analysis.
```

---

## Smart Search

Smart Search failure:

```text
Smart Search is temporarily unavailable. Try a simpler search or try again later.
```

No results:

```text
No Pokémon matched your smart search. Try a different type, stat, or description.
```

---

## Auth

Register failed:

```text
We could not create your account. Please check your details and try again.
```

Login failed:

```text
Email or password is incorrect.
```

Session expired:

```text
Your session expired. Please sign in again.
```

---

# Server-Side Policy

In ASP.NET Core, do not return raw exceptions to the client.

Bad example:

```csharp
return BadRequest(ex.Message);
```

Good example:

```csharp
_logger.LogError(ex, "Failed to analyze Dream Team.");

return StatusCode(StatusCodes.Status500InternalServerError, new
{
    Message = "We could not analyze your Dream Team right now. Please try again."
});
```

Controllers should return clean business messages only.

Good example:

```csharp
return BadRequest(new
{
    ErrorCode = "TEAM_FULL",
    Message = "Your Dream Team is full. Remove a Pokémon before adding another one."
});
```

---

# Using `ServiceResult`

The project already uses this pattern:

```text
ServiceResult<T>
```

Use it for business errors:

```csharp
return ServiceResult<T>.Fail(
    "TEAM_FULL",
    "Your Dream Team is full. Remove a Pokémon before adding another one.");
```

Then map the result in the controller:

```csharp
return result.ErrorCode switch
{
    "POKEMON_NOT_FOUND" => NotFound(error),
    "TEAM_FULL" => BadRequest(error),
    "DUPLICATE_POKEMON" => Conflict(error),
    _ => BadRequest(error)
};
```

---

# Full-System Checklist

## Client

- [ ] Search for `alert(` and replace it with friendly UI error handling.
- [ ] Search for `subscribe(` calls without an `error` handler.
- [ ] Search for direct usage of `error.message` in user-facing UI.
- [ ] Ensure every screen uses `errorMessage` or an equivalent UI state.
- [ ] Ensure every technical error is logged with `console.error`.
- [ ] Add `HttpErrorInterceptor`.
- [ ] Add `GlobalErrorHandler`.
- [ ] Ensure the app does not crash when the API returns `null`, empty arrays, or `500`.
- [ ] Ensure empty arrays have an Empty State.
- [ ] Ensure Retry exists where relevant.

## Server

- [ ] Do not return raw exceptions to the client.
- [ ] Use `ILogger`.
- [ ] Use `ServiceResult` for business errors.
- [ ] Map error codes to proper HTTP status codes.
- [ ] Do not expose SQL, Gemini, or stack trace details.
- [ ] Provide clear business messages such as `TEAM_FULL`, `POKEMON_NOT_FOUND`.

---

# Final Rule

For every error, ask:

```text
Can the user understand what happened and what to do next?
Can the developer see the technical details in the console or logs?
Does the application continue working without crashing?
```

If the answer to any of these is no, the error handling should be improved.
