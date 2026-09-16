import { test as base } from '@playwright/test';

/**
 * Tests run without the real Google Maps script.
 *
 * Loading it would make every run depend on the network and on a live API key, and would
 * bill a real account for test traffic. Blocking it also exercises something worth proving:
 * that the page is fully usable when the map is not there, which is the promise the results
 * rail is making.
 *
 * The map itself is verified by hand rather than here, because asserting against Google's
 * rendered tiles is brittle and tests their software rather than ours.
 */
export const test = base.extend({
  page: async ({ page }, use) => {
    await page.route('https://maps.googleapis.com/**', (route) => route.abort());
    await use(page);
  },
});

export { expect } from '@playwright/test';
