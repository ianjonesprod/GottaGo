import { ChangeDetectionStrategy, Component, inject, OnInit } from '@angular/core';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { ActivatedRoute, Router } from '@angular/router';

import { AuthStore } from '../../../core/auth/auth-store';

/**
 * Where Google sends people back to.
 *
 * The backend has already set the refresh cookie by this point. No token is passed in the
 * URL on purpose - that would put a credential in browser history and in the logs of
 * anything in between - so this simply exchanges the cookie for an access token and moves on.
 */
@Component({
  selector: 'gg-oauth-callback',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatProgressBarModule],
  template: `
    <div class="callback">
      <h1 tabindex="-1">Signing you in…</h1>
      <mat-progress-bar mode="indeterminate" aria-label="Signing you in" />
      <p role="status">Just a moment.</p>
    </div>
  `,
  styles: `
    .callback {
      display: grid;
      gap: 1rem;
      padding: 4rem 1rem;
      max-width: 24rem;
      margin: 0 auto;
      text-align: center;
    }
  `,
})
export class OauthCallback implements OnInit {
  private readonly auth = inject(AuthStore);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  ngOnInit(): void {
    const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl') ?? '/map';

    this.auth.refresh().subscribe({
      next: (user) => void this.router.navigateByUrl(user ? returnUrl : '/sign-in?error=google'),
      error: () => void this.router.navigateByUrl('/sign-in?error=google'),
    });
  }
}
