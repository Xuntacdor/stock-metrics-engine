import { test, expect } from '@playwright/test';

/**
 * Auth flow: guest redirect, login, logout.
 *
 * Requires a seeded test user. Override credentials via env vars:
 *   E2E_USER=test@example.com  E2E_PASS=Test@1234
 */
const USER  = process.env['E2E_USER'] ?? 'test@quantiq.vn';
const PASS  = process.env['E2E_PASS'] ?? 'Test@1234';

test.describe('Authentication', () => {

    test('unauthenticated user is redirected to /auth/login', async ({ page }) => {
        await page.goto('/dashboard');
        await expect(page).toHaveURL(/\/auth\/login/);
    });

    test('login with invalid credentials shows error', async ({ page }) => {
        await page.goto('/auth/login');
        await page.getByLabel(/email/i).fill('nobody@nowhere.vn');
        await page.getByRole('textbox', { name: 'Mật khẩu' }).fill('wrongpass');
        await page.getByRole('button', { name: /đăng nhập|login/i }).click();

        // Inline error alert shown by the login form
        await expect(
            page.locator('[role="alert"]').filter({ hasText: /not correct|sai|không đúng|invalid/i })
        ).toBeVisible({ timeout: 8_000 });
    });

    test('login with valid credentials navigates to dashboard', async ({ page }) => {
        await page.goto('/auth/login');
        await page.getByLabel(/email/i).fill(USER);
        await page.getByRole('textbox', { name: 'Mật khẩu' }).fill(PASS);
        await page.getByRole('button', { name: /đăng nhập|login/i }).click();

        await expect(page).toHaveURL(/\/dashboard/, { timeout: 10_000 });
        // Sidebar or heading confirms authenticated state
        await expect(page.getByText(/tổng quan|dashboard/i).first()).toBeVisible();
    });

    test('authenticated user cannot visit /auth/login (guest guard)', async ({ page, context }) => {
        // Login first
        await page.goto('/auth/login');
        await page.getByLabel(/email/i).fill(USER);
        await page.getByRole('textbox', { name: 'Mật khẩu' }).fill(PASS);
        await page.getByRole('button', { name: /đăng nhập|login/i }).click();
        await expect(page).toHaveURL(/\/dashboard/, { timeout: 10_000 });

        // Now try to visit login — should redirect away
        await page.goto('/auth/login');
        await expect(page).not.toHaveURL(/\/auth\/login/, { timeout: 5_000 });
    });
});
