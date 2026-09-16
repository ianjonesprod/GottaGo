import { Routes } from '@angular/router';

import { authGuard, guestGuard } from './core/auth/auth.guard';

/**
 * Route `title` data is read by RouteA11yService, which sets the document title and moves
 * focus to the page heading on every navigation.
 */
export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'map' },
  {
    path: 'map',
    title: 'Map',
    data: { title: 'Map' },
    loadComponent: () => import('./features/map/map-page').then((m) => m.MapPage),
    children: [
      {
        path: ':slug/review',
        data: { title: 'Write a review' },
        canActivate: [authGuard],
        loadComponent: () =>
          import('./features/review-submit/review-submit').then((m) => m.ReviewSubmit),
      },
      {
        // A child route rather than a dialog, so the panel has a shareable URL and the
        // browser back button closes it.
        path: ':slug',
        data: { title: 'Bathroom' },
        loadComponent: () =>
          import('./features/bathroom-detail/bathroom-detail').then((m) => m.BathroomDetail),
      },
    ],
  },
  {
    path: 'reviews',
    title: 'Reviews',
    data: { title: 'Reviews' },
    loadComponent: () => import('./features/reviews-list/reviews-list').then((m) => m.ReviewsList),
  },
  {
    path: 'scores',
    title: 'High scores',
    data: { title: 'High scores' },
    loadComponent: () => import('./features/high-scores/high-scores').then((m) => m.HighScores),
  },
  {
    path: 'sign-in',
    title: 'Sign in',
    data: { title: 'Sign in' },
    canActivate: [guestGuard],
    loadComponent: () => import('./features/auth/sign-in/sign-in').then((m) => m.SignIn),
  },
  {
    path: 'sign-up',
    title: 'Create an account',
    data: { title: 'Create an account' },
    canActivate: [guestGuard],
    loadComponent: () => import('./features/auth/sign-up/sign-up').then((m) => m.SignUp),
  },
  {
    path: 'auth/callback',
    data: { title: 'Signing in' },
    loadComponent: () =>
      import('./features/auth/oauth-callback/oauth-callback').then((m) => m.OauthCallback),
  },
  {
    path: '**',
    data: { title: 'Page not found' },
    loadComponent: () => import('./features/not-found/not-found').then((m) => m.NotFound),
  },
];
