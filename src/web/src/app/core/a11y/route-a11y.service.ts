import { DOCUMENT, inject, Injectable } from '@angular/core';
import { Title } from '@angular/platform-browser';
import { NavigationEnd, Router } from '@angular/router';
import { filter } from 'rxjs';

/**
 * Makes route changes perceivable to people who are not watching the screen.
 *
 * A single-page app swaps content without a page load, so a screen reader is given no signal
 * that anything happened and keyboard focus is left wherever the old page put it. On every
 * navigation this sets the document title and moves focus to the new page heading, which is
 * what a real page load would have done.
 */
@Injectable({ providedIn: 'root' })
export class RouteA11yService {
  private readonly router = inject(Router);
  private readonly title = inject(Title);
  private readonly document = inject(DOCUMENT);

  start(): void {
    this.router.events
      .pipe(filter((event): event is NavigationEnd => event instanceof NavigationEnd))
      .subscribe(() => {
        const routeTitle = this.deepestTitle();
        this.title.setTitle(routeTitle ? `${routeTitle} — GottaGo` : 'GottaGo');

        // Let the new view render before hunting for its heading.
        queueMicrotask(() => this.focusMainHeading());
      });
  }

  private deepestTitle(): string | undefined {
    let route = this.router.routerState.snapshot.root;

    while (route.firstChild) {
      route = route.firstChild;
    }

    return route.data['title'] as string | undefined;
  }

  private focusMainHeading(): void {
    const heading = this.document.querySelector<HTMLElement>('main h1, main [data-page-heading]');

    if (!heading) {
      return;
    }

    // tabindex="-1" makes a heading programmatically focusable without adding it to the tab
    // order, so focus lands on the page title rather than being left behind on the old view.
    if (!heading.hasAttribute('tabindex')) {
      heading.setAttribute('tabindex', '-1');
    }

    heading.focus({ preventScroll: false });
  }
}
