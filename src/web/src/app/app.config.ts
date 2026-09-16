import { provideHttpClient, withFetch } from '@angular/common/http';
import {
  ApplicationConfig,
  inject,
  provideAppInitializer,
  provideBrowserGlobalErrorListeners,
} from '@angular/core';
import { provideRouter, withComponentInputBinding, withInMemoryScrolling } from '@angular/router';

import { RouteA11yService } from './core/a11y/route-a11y.service';
import { AppConfigService } from './core/config/app-config.service';
import { routes } from './app.routes';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideHttpClient(withFetch()),
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
      return inject(AppConfigService).load();
    }),
  ],
};
