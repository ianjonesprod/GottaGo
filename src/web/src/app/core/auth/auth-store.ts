import { HttpClient } from '@angular/common/http';
import { computed, inject, Injectable, signal } from '@angular/core';
import { catchError, map, Observable, of, tap } from 'rxjs';

export interface CurrentUser {
  id: string;
  email: string;
  displayName: string;
}

interface AuthResponseDto {
  accessToken: string;
  expiresAt: string;
  user: CurrentUser;
}

/**
 * Who is signed in, and the token proving it.
 *
 * The access token is held in memory only - never in localStorage or sessionStorage. Anything
 * stored there is readable by any script that manages to run on the page, so a token kept
 * there survives to be stolen. In memory it dies with the tab.
 *
 * The cost is that a page refresh loses it, which is why the app calls refresh once at
 * startup. The long-lived credential for that lives in a cookie JavaScript cannot read.
 */
@Injectable({ providedIn: 'root' })
export class AuthStore {
  private readonly http = inject(HttpClient);

  private readonly accessToken = signal<string | null>(null);
  private readonly currentUser = signal<CurrentUser | null>(null);

  readonly user = this.currentUser.asReadonly();
  readonly isSignedIn = computed(() => this.currentUser() !== null);

  get token(): string | null {
    return this.accessToken();
  }

  register(email: string, password: string, displayName: string): Observable<CurrentUser> {
    return this.http
      .post<AuthResponseDto>('/api/auth/register', { email, password, displayName })
      .pipe(tap((response) => this.accept(response)), map((response) => response.user));
  }

  signIn(email: string, password: string): Observable<CurrentUser> {
    return this.http
      .post<AuthResponseDto>('/api/auth/login', { email, password })
      .pipe(tap((response) => this.accept(response)), map((response) => response.user));
  }

  /**
   * Swaps the refresh cookie for a new access token.
   *
   * A 401 here is the normal answer for a visitor who is not signed in, so it resolves to
   * null rather than surfacing as an error.
   */
  refresh(): Observable<CurrentUser | null> {
    return this.http.post<AuthResponseDto>('/api/auth/refresh', {}).pipe(
      tap((response) => this.accept(response)),
      map((response) => response.user),
      catchError(() => {
        this.clear();
        return of(null);
      }),
    );
  }

  signOut(): Observable<void> {
    return this.http.post<void>('/api/auth/logout', {}).pipe(
      tap(() => this.clear()),
      catchError(() => {
        // The server may already consider us signed out. Either way, forget the token here.
        this.clear();
        return of(void 0);
      }),
    );
  }

  private accept(response: AuthResponseDto): void {
    this.accessToken.set(response.accessToken);
    this.currentUser.set(response.user);
  }

  private clear(): void {
    this.accessToken.set(null);
    this.currentUser.set(null);
  }
}
