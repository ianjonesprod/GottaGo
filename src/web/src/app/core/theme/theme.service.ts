import { DOCUMENT, inject, Injectable, signal } from '@angular/core';

const STORAGE_KEY = 'gottago.theme';

/**
 * Light or dark, remembered per browser.
 *
 * Defaults to whatever the operating system asks for. Storage is wrapped in try/catch
 * because private browsing and blocked site data make it throw, and a theme preference is
 * never worth breaking the app over.
 */
@Injectable({ providedIn: 'root' })
export class ThemeService {
  private readonly document = inject(DOCUMENT);
  private readonly dark = signal(this.initialPreference());

  readonly isDark = this.dark.asReadonly();

  constructor() {
    this.apply(this.dark());
  }

  toggle(): void {
    const next = !this.dark();

    this.dark.set(next);
    this.apply(next);

    try {
      localStorage.setItem(STORAGE_KEY, next ? 'dark' : 'light');
    } catch {
      // Preference simply will not persist. Not worth surfacing.
    }
  }

  private apply(dark: boolean): void {
    this.document.documentElement.style.colorScheme = dark ? 'dark' : 'light';
  }

  private initialPreference(): boolean {
    try {
      const stored = localStorage.getItem(STORAGE_KEY);

      if (stored) {
        return stored === 'dark';
      }
    } catch {
      // Fall through to the system preference.
    }

    return this.document.defaultView?.matchMedia?.('(prefers-color-scheme: dark)').matches ?? false;
  }
}
