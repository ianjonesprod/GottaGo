import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';

import { AuthStore } from './auth-store';

/**
 * Keeps signed-out visitors away from pages that need an account, remembering where they
 * were headed so they land there after signing in rather than being dumped on the home page.
 */
export const authGuard: CanActivateFn = (_route, state) => {
  const auth = inject(AuthStore);
  const router = inject(Router);

  if (auth.isSignedIn()) {
    return true;
  }

  return router.createUrlTree(['/sign-in'], { queryParams: { returnUrl: state.url } });
};

/** Sends already-signed-in people away from the sign-in and sign-up pages. */
export const guestGuard: CanActivateFn = () => {
  const auth = inject(AuthStore);
  const router = inject(Router);

  return auth.isSignedIn() ? router.createUrlTree(['/map']) : true;
};
