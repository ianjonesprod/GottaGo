import AxeBuilder from '@axe-core/playwright';
import { expect, type Page } from '@playwright/test';

/**
 * Asserts a page state has no WCAG 2.1 AA violations axe can detect.
 *
 * Google's own map DOM contains violations we cannot fix, so `.gm-style` is excluded. That
 * exclusion is only acceptable because it is compensated: every spec that hides the map from
 * axe also asserts the same task is completable through the results rail. An exclusion
 * without a compensating assertion is just a suppressed failure.
 *
 * Automated checks catch a minority of real barriers, so these assertions sit alongside
 * keyboard walkthroughs rather than replacing them.
 */
export async function expectNoA11yViolations(page: Page, context?: string): Promise<void> {
  const results = await new AxeBuilder({ page })
    .withTags(['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa'])
    .exclude('.gm-style')
    .analyze();

  const summary = results.violations.map(
    (v) => `${v.id} (${v.impact}): ${v.help} — ${v.nodes.length} element(s)\n    ${v.nodes[0]?.html ?? ''}`,
  );

  expect(summary, `Accessibility violations${context ? ` on ${context}` : ''}`).toEqual([]);
}
