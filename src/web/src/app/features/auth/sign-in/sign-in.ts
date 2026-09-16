import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';

import { AppConfigService } from '../../../core/config/app-config.service';
import { AuthStore } from '../../../core/auth/auth-store';

@Component({
  selector: 'gg-sign-in',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    ReactiveFormsModule,
    RouterLink,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatProgressBarModule,
  ],
  templateUrl: './sign-in.html',
  styleUrl: '../auth-form.scss',
})
export class SignIn {
  private readonly auth = inject(AuthStore);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  protected readonly config = inject(AppConfigService);

  protected readonly submitting = signal(false);
  protected readonly errorMessage = signal<string | null>(null);

  protected readonly form = inject(FormBuilder).nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required]],
  });

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    this.errorMessage.set(null);

    const { email, password } = this.form.getRawValue();

    this.auth.signIn(email, password).subscribe({
      next: () => {
        const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl') ?? '/map';
        void this.router.navigateByUrl(returnUrl);
      },
      error: (error: unknown) => {
        this.submitting.set(false);
        this.errorMessage.set(this.describe(error));
      },
    });
  }

  protected signInWithGoogle(): void {
    const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl') ?? '/map';

    // A full page navigation rather than a fetch: the backend brokers the whole exchange
    // with Google, so the client secret never comes near the browser.
    window.location.href = `/api/auth/google/start?returnUrl=${encodeURIComponent(returnUrl)}`;
  }

  private describe(error: unknown): string {
    if (error instanceof HttpErrorResponse && error.status === 423) {
      return 'Too many failed attempts. Try again in a few minutes.';
    }

    if (error instanceof HttpErrorResponse && error.status === 429) {
      return 'Too many attempts from this device. Wait a moment and try again.';
    }

    // Deliberately vague: the server does not say whether the account exists, and neither
    // should this.
    return "That email and password combination didn't work.";
  }
}
