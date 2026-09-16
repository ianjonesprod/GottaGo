import { defineConfig, devices } from '@playwright/test';

/**
 * The API and the dev server are expected to already be running. Starting them here as well
 * would fight with a developer's own terminals, and the seeded database has to exist first
 * for any of these assertions to mean anything.
 */
export default defineConfig({
  testDir: './specs',
  fullyParallel: true,
  forbidOnly: !!process.env['CI'],
  retries: process.env['CI'] ? 2 : 0,
  reporter: [['list']],
  use: {
    baseURL: 'http://localhost:4200',
    trace: 'on-first-retry',
    // Deterministic location, so "near you" results never depend on where the machine is.
    geolocation: { latitude: 41.4993, longitude: -81.6944 },
    permissions: ['geolocation'],
    locale: 'en-US',
    timezoneId: 'America/New_York',
  },
  projects: [
    { name: 'desktop', use: { ...devices['Desktop Chrome'] } },
    { name: 'mobile', use: { ...devices['Pixel 7'] } },
  ],
});
