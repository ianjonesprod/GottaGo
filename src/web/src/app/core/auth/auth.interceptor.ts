import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, Observable, shareReplay, switchMap, throwError } from 'rxjs';

import { AuthStore } from './auth-store';

/** Endpoints that must not be retried or given a bearer token, or refresh would recurse. */
const SKIPPED = ['/api/auth/refresh', '/api/auth/login', '/api/auth/register', '/api/client-config'];

/**
 * Shared across requests so ten simultaneous 401s cause one refresh rather than ten.
 *
 * Without this, a page that fires several requests at once when the token has just expired
 * would rotate the refresh token several times concurrently - and because reuse of a rotated
 * token is treated as theft, that would revoke the whole chain and sign the user out.
 */
let inFlightRefresh: Observable<unknown> | null = null;

export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const auth = inject(AuthStore);
  const router = inject(Router);

  const isSkipped = SKIPPED.some((path) => request.url.includes(path));
  const token = auth.token;

  const authorised =
    token && !isSkipped
      ? request.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
      : request;

  return next(authorised).pipe(
    catchError((error: unknown) => {
      const isUnauthorised = error instanceof HttpErrorResponse && error.status === 401;

      if (!isUnauthorised || isSkipped) {
        return throwError(() => error);
      }

      inFlightRefresh ??= auth.refresh().pipe(shareReplay(1));

      return inFlightRefresh.pipe(
        switchMap(() => {
          inFlightRefresh = null;

          const refreshed = auth.token;

          if (!refreshed) {
            void router.navigate(['/sign-in'], {
              queryParams: { returnUrl: router.url },
            });

            return throwError(() => error);
          }

          // Retried exactly once. A second 401 after a fresh token is a real failure, not a
          // timing problem, and retrying again would loop.
          return next(request.clone({ setHeaders: { Authorization: `Bearer ${refreshed}` } }));
        }),
        catchError((refreshError: unknown) => {
          inFlightRefresh = null;

          return throwError(() => refreshError);
        }),
      );
    }),
  );
};
