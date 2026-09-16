import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { Router, RouterLink } from '@angular/router';

import { AuthStore } from '../../../core/auth/auth-store';

/** Matches the server's rule. Length beats character-class requirements and is easier to meet. */
export const MINIMUM_PASSWORD_LENGTH = 10;

@Component({
  selector: 'gg-sign-up',
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
  templateUrl: './sign-up.html',
  styleUrl: '../auth-form.scss',
})
export class SignUp {
  private readonly auth = inject(AuthStore);
  private readonly router = inject(Router);

  protected readonly minimumPasswordLength = MINIMUM_PASSWORD_LENGTH;
  protected readonly submitting = signal(false);
  protected readonly errorMessage = signal<string | null>(null);

  protected readonly form = inject(FormBuilder).nonNullable.group({
    displayName: ['', [Validators.required, Validators.maxLength(100)]],
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(MINIMUM_PASSWORD_LENGTH)]],
  });

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    this.errorMessage.set(null);

    const { email, password, displayName } = this.form.getRawValue();

    this.auth.register(email, password, displayName).subscribe({
      next: () => void this.router.navigateByUrl('/map'),
      error: (error: unknown) => {
        this.submitting.set(false);

        if (error instanceof HttpErrorResponse && error.status === 400) {
          // The server distinguishes "already registered" from "too short", and the message
          // is safe to show because registration necessarily reveals whether an email is
          // taken - there is no way to offer sign-up without that.
          this.errorMessage.set(
            (error.error as { detail?: string } | null)?.detail ?? 'That did not work. Check the form and try again.',
          );
          return;
        }

        this.errorMessage.set('Something went wrong creating your account. Try again.');
      },
    });
  }
}
