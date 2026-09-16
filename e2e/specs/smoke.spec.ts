import { expect, test } from '../support/fixtures';

import { expectNoA11yViolations } from '../support/a11y';

test.describe('the app loads and shows seeded data', () => {
  test('map page lists Cleveland bathrooms in the results rail', async ({ page }) => {
    await page.goto('/map');

    const rail = page.getByRole('region', { name: 'Results' });
    await expect(rail).toBeVisible();

    // The seeded dataset is 25 bathrooms and the generator is deterministic, so this is a
    // stable number rather than a guess.
    await expect(rail.getByRole('link')).toHaveCount(25, { timeout: 15_000 });
    await expect(page.getByText(/25 bathrooms/)).toBeVisible();
  });

  test('map page has no accessibility violations', async ({ page }) => {
    await page.goto('/map');
    await expect(page.getByRole('region', { name: 'Results' }).getByRole('link').first()).toBeVisible();

    await expectNoA11yViolations(page, 'the map page');
  });

  test('reviews page lists reviews', async ({ page }) => {
    await page.goto('/reviews');

    await expect(page.getByRole('heading', { name: 'Reviews', level: 1 })).toBeVisible();
    await expect(page.getByRole('listitem').first()).toBeVisible({ timeout: 15_000 });
  });

  test('reviews page has no accessibility violations', async ({ page }) => {
    await page.goto('/reviews');
    await expect(page.getByRole('listitem').first()).toBeVisible({ timeout: 15_000 });

    await expectNoA11yViolations(page, 'the reviews page');
  });

  test('high scores page ranks bathrooms', async ({ page }) => {
    await page.goto('/scores');

    await expect(page.getByRole('heading', { name: 'High scores', level: 1 })).toBeVisible();
    await expect(page.getByRole('table')).toBeVisible({ timeout: 15_000 });
  });

  test('high scores page has no accessibility violations', async ({ page }) => {
    await page.goto('/scores');
    await expect(page.getByRole('table')).toBeVisible({ timeout: 15_000 });

    await expectNoA11yViolations(page, 'the high scores page');
  });
});

test.describe('bathroom detail', () => {
  test('opens from the rail and shows ratings and gallery', async ({ page }) => {
    await page.goto('/map/west-side-market');

    await expect(page.getByRole('heading', { name: 'West Side Market' })).toBeVisible({ timeout: 15_000 });
    await expect(page.getByRole('heading', { name: 'Ratings' })).toBeVisible();
    await expect(page.getByRole('heading', { name: 'Gallery' })).toBeVisible();

    // The access note is the whole point of the field: this one is closed two days a week.
    await expect(page.locator('.detail__access')).toContainText('Market hours only');
  });

  test('detail panel has no accessibility violations', async ({ page }) => {
    await page.goto('/map/west-side-market');
    await expect(page.getByRole('heading', { name: 'West Side Market' })).toBeVisible({ timeout: 15_000 });

    await expectNoA11yViolations(page, 'the bathroom detail panel');
  });

  test('every gallery image has alt text', async ({ page }) => {
    await page.goto('/map/west-side-market');
    await expect(page.getByRole('heading', { name: 'Gallery' })).toBeVisible({ timeout: 15_000 });

    const images = page.locator('.detail__gallery img');
    const count = await images.count();

    expect(count).toBeGreaterThan(0);

    for (let i = 0; i < count; i++) {
      const alt = await images.nth(i).getAttribute('alt');
      expect(alt?.trim()).toBeTruthy();
    }
  });

  test('escape closes the panel and returns to the map', async ({ page }) => {
    await page.goto('/map/west-side-market');
    await expect(page.getByRole('heading', { name: 'West Side Market' })).toBeVisible({ timeout: 15_000 });

    await page.keyboard.press('Escape');

    await expect(page).toHaveURL(/\/map$/);
  });
});

test.describe('keyboard access', () => {
  test('skip link is the first thing you reach and jumps to the content', async ({ page }) => {
    await page.goto('/map');

    // Wait for the app to finish starting. It fetches its config before the first render,
    // and tabbing mid-bootstrap races that.
    await expect(page.getByRole('heading', { name: 'Bathrooms near you', level: 1 })).toBeVisible();

    await page.keyboard.press('Tab');

    const skipLink = page.getByRole('link', { name: 'Skip to main content' });
    await expect(skipLink).toBeFocused();

    // And it actually goes somewhere: activating it moves focus to the main region.
    await page.keyboard.press('Enter');
    await expect(page.locator('#main-content')).toBeFocused();
  });
});
