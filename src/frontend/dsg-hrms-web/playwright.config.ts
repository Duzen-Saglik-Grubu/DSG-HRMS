import { defineConfig, devices } from '@playwright/test';

/**
 * Uctan uca testler (ADR-0011 §1, #146).
 *
 * Testler `docker/compose.e2e.yml` yigininda calisir: sentetik veri, Mailpit, gercek API ve
 * gercek web sunucusu. Yigini kurmak icin depo kokunde: `bash docker/e2e/up.sh`.
 *
 * Senaryolar hesap durumunu paylasir (uyelik -> giris -> parola), bu yuzden sirayla calisir.
 * Yerelde kurulu Edge kullanilabilir: `E2E_BROWSER_CHANNEL=msedge`.
 */
export default defineConfig({
  testDir: './e2e',
  fullyParallel: false,
  workers: 1,
  forbidOnly: Boolean(process.env.CI),
  retries: 0,
  timeout: 60_000,
  expect: { timeout: 15_000 },
  reporter: process.env.CI ? [['list'], ['html', { open: 'never' }]] : 'list',
  use: {
    baseURL: process.env.E2E_BASE_URL ?? 'http://localhost:8090',
    locale: 'tr-TR',
    timezoneId: 'Europe/Istanbul',
    trace: 'retain-on-failure',
    screenshot: 'only-on-failure',
  },
  projects: [
    {
      name: 'chromium',
      use: {
        ...devices['Desktop Chrome'],
        ...(process.env.E2E_BROWSER_CHANNEL ? { channel: process.env.E2E_BROWSER_CHANNEL } : {}),
      },
    },
  ],
});
