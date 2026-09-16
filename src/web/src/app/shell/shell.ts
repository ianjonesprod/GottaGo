import { BreakpointObserver, Breakpoints } from '@angular/cdk/layout';
import { ChangeDetectionStrategy, Component, effect, inject, signal } from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatListModule } from '@angular/material/list';
import { MatMenuModule } from '@angular/material/menu';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatToolbarModule } from '@angular/material/toolbar';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { debounceTime, distinctUntilChanged, map } from 'rxjs';

import { AuthStore } from '../core/auth/auth-store';
import { SearchStore } from '../core/search/search-store';
import { ThemeService } from '../core/theme/theme.service';

interface Destination {
  path: string;
  label: string;
  icon: string;
  hint: string;
}

@Component({
  selector: 'gg-shell',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    RouterOutlet,
    RouterLink,
    RouterLinkActive,
    FormsModule,
    MatToolbarModule,
    MatSidenavModule,
    MatListModule,
    MatIconModule,
    MatButtonModule,
    MatMenuModule,
    MatFormFieldModule,
    MatInputModule,
  ],
  templateUrl: './shell.html',
  styleUrl: './shell.scss',
})
export class Shell {
  private readonly breakpoints = inject(BreakpointObserver);
  private readonly router = inject(Router);
  protected readonly search = inject(SearchStore);
  protected readonly auth = inject(AuthStore);
  protected readonly theme = inject(ThemeService);

  /** Below tablet width the sidenav becomes a slide-over rather than a permanent rail. */
  protected readonly isHandset = toSignal(
    this.breakpoints.observe([Breakpoints.Handset, Breakpoints.TabletPortrait]).pipe(map((r) => r.matches)),
    { initialValue: false },
  );

  protected readonly destinations: Destination[] = [
    { path: '/map', label: 'Map', icon: 'map', hint: 'Find one near you' },
    { path: '/reviews', label: 'Reviews', icon: 'reviews', hint: 'Newest first' },
    { path: '/scores', label: 'High scores', icon: 'trophy', hint: 'Best rated' },
  ];

  protected readonly draftQuery = signal('');

  /**
   * The search box filters as you type.
   *
   * Debounced because every keystroke would otherwise be a request: typing "library" would
   * fire seven searches and the last one to come back would win, which is both wasteful and
   * occasionally wrong. A third of a second is long enough to finish a word and short enough
   * that it still feels immediate.
   */
  private readonly debouncedQuery = toSignal(
    toObservable(this.draftQuery).pipe(
      debounceTime(300),
      map((value) => value.trim()),
      distinctUntilChanged(),
    ),
    { initialValue: '' },
  );

  constructor() {
    effect(() => this.search.setKeyword(this.debouncedQuery()));
  }

  /** Enter still works, and skips the wait rather than making you pause for it. */
  protected submitSearch(event: Event): void {
    event.preventDefault();
    this.search.setKeyword(this.draftQuery().trim());
  }

  protected clearSearch(): void {
    this.draftQuery.set('');
    this.search.setKeyword('');
  }

  protected closeIfHandset(drawer: { close: () => void }): void {
    if (this.isHandset()) {
      drawer.close();
    }
  }

  protected signOut(): void {
    this.auth.signOut().subscribe(() => void this.router.navigate(['/map']));
  }

  protected goHome(): void {
    void this.router.navigate(['/map']);
  }
}
