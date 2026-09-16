import { expect, test } from '../support/fixtures';

/** A fresh address per run, so re-running does not trip the duplicate-email rule. */
const uniqueEmail = () => `e2e+${Date.now()}${Math.floor(Math.random() * 1000)}@example.com`;

test.describe('sign up and sign in', () => {
  test('sign-in page has no accessibility violations', async ({ page, expectNoA11yViolations }) => {
    await page.goto('/sign-in');
    await expect(page.getByRole('heading', { name: 'Sign in', level: 1 })).toBeVisible();

    await expectNoA11yViolations(page, 'the sign-in page');
  });

  test('sign-up page has no accessibility violations', async ({ page, expectNoA11yViolations }) => {
    await page.goto('/sign-up');
    await expect(page.getByRole('heading', { name: 'Create an account', level: 1 })).toBeVisible();

    await expectNoA11yViolations(page, 'the sign-up page');
  });

  test('form errors are announced and stay accessible', async ({ page, expectNoA11yViolations }) => {
    await page.goto('/sign-up');

    await page.getByRole('button', { name: 'Create account' }).click();

    // Errors are the state most likely to break accessibility and least likely to be checked.
    await expect(page.getByText('Choose a name to show on your reviews.')).toBeVisible();
    await expectNoA11yViolations(page, 'the sign-up page showing validation errors');
  });

  test('a failed sign-in says so without revealing whether the account exists', async ({ page }) => {
    await page.goto('/sign-in');

    await page.getByLabel('Email').fill('definitely-not-registered@example.com');
    await page.getByLabel('Password').fill('somepassword123');
    await page.locator('form').getByRole('button', { name: 'Sign in' }).click();

    const alert = page.getByRole('alert');
    await expect(alert).toBeVisible();
    await expect(alert).toContainText("didn't work");
    // Must not hint that the address is unknown.
    await expect(alert).not.toContainText(/no account|not found|unknown/i);
  });

  test('a new account can sign up, and the menu then offers sign out', async ({ page }) => {
    await page.goto('/sign-up');

    await page.getByLabel('Display name').fill('E2E Tester');
    await page.getByLabel('Email').fill(uniqueEmail());
    await page.getByLabel('Password').fill('correcthorsebattery');
    await page.getByRole('button', { name: 'Create account' }).click();

    await expect(page).toHaveURL(/\/map$/);

    await page.getByRole('button', { name: /Account menu for/ }).click();
    await expect(page.getByRole('menuitem', { name: 'Sign out' })).toBeVisible();
  });
});

test.describe('writing a review', () => {
  test('signed-out visitors are sent to sign in and back again', async ({ page }) => {
    await page.goto('/map/west-side-market/review');

    await expect(page).toHaveURL(/\/sign-in/);
    // The destination is remembered rather than dumping them on the home page.
    await expect(page).toHaveURL(/returnUrl=/);
  });

  test('a signed-in user can rate and post, and the average moves', async ({ page }) => {
    await page.goto('/sign-up');
    await page.getByLabel('Display name').fill('Reviewer');
    await page.getByLabel('Email').fill(uniqueEmail());
    await page.getByLabel('Password').fill('correcthorsebattery');
    await page.getByRole('button', { name: 'Create account' }).click();
    await expect(page).toHaveURL(/\/map$/);

    await page.goto('/map/public-square/review');
    await expect(page.getByRole('heading', { name: /Review Public Square/ })).toBeVisible();

    // Each dimension is a real radio group, so it can be selected by its accessible name.
    for (const dimension of ['Smell', 'Cleanliness', 'Amenities', 'Accessibility', 'Ambience']) {
      await page.getByRole('group', { name: dimension }).getByRole('radio', { name: /4 stars/ }).check();
    }

    await page.getByLabel('Your review').fill('Perfectly reasonable facilities, found them easily.');
    await page.getByRole('button', { name: 'Post review' }).click();

    await expect(page).toHaveURL(/\/map\/public-square$/);
    await expect(page.getByRole('heading', { name: 'Public Square' })).toBeVisible();
  });

  test('review form has no accessibility violations, including with errors showing', async ({
    page,
    expectNoA11yViolations,
  }) => {
    await page.goto('/sign-up');
    await page.getByLabel('Display name').fill('A11y Tester');
    await page.getByLabel('Email').fill(uniqueEmail());
    await page.getByLabel('Password').fill('correcthorsebattery');
    await page.getByRole('button', { name: 'Create account' }).click();
    await expect(page).toHaveURL(/\/map$/);

    await page.goto('/map/west-side-market/review');
    await expect(page.getByRole('heading', { name: /Review West Side Market/ })).toBeVisible();

    await expectNoA11yViolations(page, 'the review form');

    await page.getByRole('button', { name: 'Post review' }).click();
    await expect(page.getByText(/Choose a Smell rating/)).toBeVisible();

    await expectNoA11yViolations(page, 'the review form showing validation errors');
  });

  test('ratings are operable by keyboard alone', async ({ page }) => {
    await page.goto('/sign-up');
    await page.getByLabel('Display name').fill('Keyboard Tester');
    await page.getByLabel('Email').fill(uniqueEmail());
    await page.getByLabel('Password').fill('correcthorsebattery');
    await page.getByRole('button', { name: 'Create account' }).click();
    await expect(page).toHaveURL(/\/map$/);

    await page.goto('/map/west-side-market/review');
    const smell = page.getByRole('group', { name: 'Smell' });
    await expect(smell).toBeVisible();

    // Radio groups give arrow-key navigation for free. This is exactly what a row of
    // clickable divs would have had to reimplement, and usually gets wrong.
    await smell.getByRole('radio', { name: /1 star/ }).focus();
    await page.keyboard.press('ArrowRight');
    await page.keyboard.press('ArrowRight');

    await expect(smell.getByRole('radio', { name: /3 stars/ })).toBeChecked();
  });
});

test.describe('adding a bathroom', () => {
  test('signed-out visitors are sent to sign in', async ({ page }) => {
    await page.goto('/map/new?lat=41.5&lng=-81.7');

    await expect(page).toHaveURL(/\/sign-in/);
  });

  test('a signed-in user can add one and it appears in the results', async ({ page }) => {
    await page.goto('/sign-up');
    await page.getByLabel('Display name').fill('Adder');
    await page.getByLabel('Email').fill(uniqueEmail());
    await page.getByLabel('Password').fill('correcthorsebattery');
    await page.getByRole('button', { name: 'Create account' }).click();
    await expect(page).toHaveURL(/\/map$/);

    await page.goto('/map/new?lat=41.5044&lng=-81.6912');
    await expect(page.getByRole('heading', { name: 'Add a bathroom' })).toBeVisible();

    const name = `Test Bathroom ${Date.now()}`;
    await page.getByLabel('Name').fill(name);
    await page.getByLabel('Street').fill('123 Test St');
    await page.getByLabel('Before you go').fill('Open weekdays only.');
    await page.getByRole('button', { name: 'Add bathroom' }).click();

    // Lands on the new bathroom's own page, which means it got a slug and is reachable.
    await expect(page.getByRole('heading', { name })).toBeVisible({ timeout: 15_000 });
    await expect(page.getByText('Open weekdays only.')).toBeVisible();
  });

  test('the add form has no accessibility violations', async ({ page, expectNoA11yViolations }) => {
    await page.goto('/sign-up');
    await page.getByLabel('Display name').fill('Form Checker');
    await page.getByLabel('Email').fill(uniqueEmail());
    await page.getByLabel('Password').fill('correcthorsebattery');
    await page.getByRole('button', { name: 'Create account' }).click();
    await expect(page).toHaveURL(/\/map$/);

    await page.goto('/map/new?lat=41.5&lng=-81.7');
    await expect(page.getByRole('heading', { name: 'Add a bathroom' })).toBeVisible();

    await expectNoA11yViolations(page, 'the add bathroom form');
  });
});
