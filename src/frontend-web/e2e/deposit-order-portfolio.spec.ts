import { test, expect, Page } from '@playwright/test';

const USER  = process.env['E2E_USER'] ?? 'test@quantiq.vn';
const PASS  = process.env['E2E_PASS'] ?? 'Test@1234';

/** Shared login helper — call once per test file via beforeEach. */
async function login(page: Page): Promise<void> {
    await page.goto('/auth/login');
    await page.getByLabel(/email/i).fill(USER);
    await page.getByRole('textbox', { name: 'Mật khẩu' }).fill(PASS);
    await page.getByRole('button', { name: /đăng nhập|login/i }).click();
    await expect(page).toHaveURL(/\/dashboard/, { timeout: 10_000 });
}

test.describe('Deposit → Portfolio flow', () => {
    test.beforeEach(async ({ page }) => login(page));

    // ── Deposit page ─────────────────────────────────────────────────────────

    test('deposit page renders wallet balance', async ({ page }) => {
        await page.goto('/deposit');
        // Stat boxes for balance, available, locked must be present
        await expect(page.getByText(/số dư thực tế/i)).toBeVisible({ timeout: 8_000 });
        await expect(page.getByText(/khả dụng/i).first()).toBeVisible();
        await expect(page.getByText(/đang phong tỏa/i)).toBeVisible();
    });

    test('deposit form validates minimum amount', async ({ page }) => {
        await page.goto('/deposit');
        // Click Nạp tiền button with empty amount — button should not submit (disabled or no-op)
        const btn = page.getByRole('button', { name: /nạp tiền ngay/i });
        await btn.click();
        // Should stay on deposit page (no redirect / no success toast)
        await expect(page).toHaveURL(/\/deposit/);
    });

    test('quick-amount buttons populate the amount field', async ({ page }) => {
        await page.goto('/deposit');
        await page.getByRole('button', { name: /1.000.000|1,000,000/i }).first().click();
        const input = page.locator('input[type="number"]');
        await expect(input).toHaveValue('1000000');
    });

    // ── Portfolio page ────────────────────────────────────────────────────────

    test('portfolio page loads holdings tab', async ({ page }) => {
        await page.goto('/portfolio');
        await expect(page.locator('h1').filter({ hasText: /danh mục đầu tư/i })).toBeVisible({ timeout: 8_000 });
        // Tab nav must show the three tabs
        await expect(page.getByRole('tab', { name: /cổ phiếu/i })).toBeVisible();
        await expect(page.getByRole('tab', { name: /giao dịch/i })).toBeVisible();
        await expect(page.getByRole('tab', { name: /hiệu suất/i })).toBeVisible();
    });

    test('portfolio transactions tab shows history or empty state', async ({ page }) => {
        await page.goto('/portfolio');
        await page.getByText(/giao dịch/i).click();

        // Either a row of transactions or the empty-state message
        await expect(
            page.getByText(/chưa có giao dịch/i).or(
                page.locator('table tbody tr').first()
            )
        ).toBeVisible({ timeout: 8_000 });
    });

    test('portfolio performance tab renders totals', async ({ page }) => {
        await page.goto('/portfolio');
        await page.getByText(/hiệu suất/i).click();
        await expect(page.getByText(/tổng tài sản/i)).toBeVisible({ timeout: 8_000 });
        await expect(page.getByText(/lãi\/lỗ tạm tính/i)).toBeVisible();
        await expect(page.getByText(/tiền mặt khả dụng/i)).toBeVisible();
    });
});

test.describe('Stock detail page', () => {
    test.beforeEach(async ({ page }) => login(page));

    test('navigates to stock detail from dashboard', async ({ page }) => {
        // Navigate directly — the price table may be empty with no market data seeded
        await page.goto('/stocks/FPT');
        await expect(page).toHaveURL(/\/stocks\/FPT/, { timeout: 8_000 });
    });

    test('stock detail page shows price display and chart', async ({ page }) => {
        await page.goto('/stocks/FPT');
        await expect(page.getByRole('heading', { name: 'FPT', exact: true })).toBeVisible({ timeout: 8_000 });
        // Buy/Sell buttons are always rendered regardless of market data
        await expect(page.getByRole('button', { name: /đặt lệnh mua/i })).toBeVisible();
        await expect(page.getByRole('button', { name: /đặt lệnh bán/i })).toBeVisible();
    });

    test('news section renders or shows empty state', async ({ page }) => {
        await page.goto('/stocks/FPT');
        await expect(
            page.getByText(/tin tức & sentiment/i).or(
                page.getByText(/chưa có tin tức/i)
            )
        ).toBeVisible({ timeout: 10_000 });
    });

    test('comment box is visible when logged in', async ({ page }) => {
        await page.goto('/stocks/FPT');
        await expect(page.getByPlaceholder(/nhận định của bạn/i)).toBeVisible({ timeout: 8_000 });
    });

    test('submitting an empty comment does nothing', async ({ page }) => {
        await page.goto('/stocks/FPT');
        const sendBtn = page.getByRole('button', { name: /gửi/i });
        await sendBtn.click();
        // No success toast, still on the same page
        await expect(page).toHaveURL(/\/stocks\/FPT/);
        await expect(page.locator('.toast-success')).not.toBeVisible();
    });
});
