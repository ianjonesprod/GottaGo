import { BreakpointObserver, Breakpoints } from '@angular/cdk/layout';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
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
import { map } from 'rxjs';

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
  protected readonly theme = inject(ThemeService);

  /** Below tablet width the sidenav becomes a slide-over rather than a permanent rail. */
  protected readonly isHandset = toSignal(
    this.breakpoints.observe([Breakpoints.Handset, Breakpoints.TabletPortrait]).pipe(map((r) => r.matches)),
    { initialValue: false },
  );

  protected readonly destinations: Destination[] = [
    { path: '/map', label: 'Map', icon: 'map', hint: 'Bathrooms near you on a map' },
    { path: '/reviews', label: 'Reviews', icon: 'reviews', hint: 'Every review, newest first' },
    { path: '/scores', label: 'High scores', icon: 'trophy', hint: 'The best rated bathrooms' },
  ];

  protected readonly draftQuery = signal('');

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

  protected goHome(): void {
    void this.router.navigate(['/map']);
  }
}
