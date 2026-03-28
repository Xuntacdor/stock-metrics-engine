import { defineConfig, devices } from '@playwright/test';

export default defineConfig({
    testDir: './e2e',
    fullyParallel: false,
    forbidOnly: !!process.env['CI'],
    retries: process.env['CI'] ? 2 : 0,
    workers: 1,
    reporter: process.env['CI'] ? 'github' : 'list',

    use: {
        baseURL: process.env['E2E_BASE_URL'] ?? 'http://localhost:4200',
        trace: 'on-first-retry',
        screenshot: 'only-on-failure',
        video: 'retain-on-failure',
    },

    projects: [
        { name: 'chromium', use: { ...devices['Desktop Chrome'] } },
    ],

    // Start the dev server automatically when running locally
    webServer: process.env['CI'] ? undefined : {
        command: 'ng serve --configuration development',
        url: 'http://localhost:4200',
        reuseExistingServer: true,
        timeout: 120_000,
    },
});
