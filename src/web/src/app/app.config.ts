import { provideHttpClient, withFetch, withInterceptors } from '@angular/common/http';
import {
  ApplicationConfig,
  inject,
  provideAppInitializer,
  provideBrowserGlobalErrorListeners,
} from '@angular/core';
import { provideRouter, withComponentInputBinding, withInMemoryScrolling } from '@angular/router';

import { RouteA11yService } from './core/a11y/route-a11y.service';
import { authInterceptor } from './core/auth/auth.interceptor';
import { AuthStore } from './core/auth/auth-store';
import { AppConfigService } from './core/config/app-config.service';
import { firstValueFrom } from 'rxjs';

import { routes } from './app.routes';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideHttpClient(withFetch(), withInterceptors([authInterceptor])),
    provideRouter(
      routes,
      withComponentInputBinding(),
      // Restore scroll on back/forward, and start at the top on a new page, the way a
      // browser would if this were not a single-page app.
      withInMemoryScrolling({ scrollPositionRestoration: 'enabled', anchorScrolling: 'enabled' }),
    ),
    provideAppInitializer(() => {
      inject(RouteA11yService).start();

      // Fetch the Maps key and defaults before the first render, so the map can draw
      // immediately rather than flashing a placeholder first.
      const auth = inject(AuthStore);

      // Restore the session before the first render. The access token only lives in memory,
      // so a refresh loses it; the cookie is what survives, and this trades it back in.
      // A 401 here is the normal answer for a visitor who is not signed in.
      return Promise.all([
        inject(AppConfigService).load(),
        firstValueFrom(auth.refresh()).catch(() => null),
      ]);
    }),
  ],
};
