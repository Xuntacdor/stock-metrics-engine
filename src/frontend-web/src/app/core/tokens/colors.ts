/**
 * QuantIQ Color Token Constants
 * Maps CSS custom properties to TypeScript constants for programmatic use.
 * Use these in component logic (Charts, dynamic styles, etc.)
 */

export const COLORS = {
    // Vietnamese Stock Market Semantic
    up: 'oklch(0.54 0.24 120)',
    down: 'oklch(0.54 0.22 15)',
    limitUp: 'oklch(0.68 0.22 295)',
    limitDown: 'oklch(0.65 0.14 200)',
    reference: 'oklch(0.80 0.12 85)',

    // UI System (CSS var references)
    bg: 'var(--color-bg)',
    surface: 'var(--color-surface)',
    surface2: 'var(--color-surface-2)',
    fg: 'var(--color-fg)',
    fgMuted: 'var(--color-fg-muted)',
    border: 'var(--color-border)',
    primary: 'var(--color-primary)',
} as const;

export type PriceColor = 'up' | 'down' | 'limit-up' | 'limit-down' | 'reference' | 'neutral';

/**
 * Resolve color class names for a price change value.
 * Used by PriceDisplay, Badge, and table cells.
 */
export function getPriceColorClass(value: number, refPrice?: number, ceilPrice?: number, floorPrice?: number): PriceColor {
    if (value === 0) return 'neutral';
    if (ceilPrice !== undefined && value >= ceilPrice) return 'limit-up';
    if (floorPrice !== undefined && value <= floorPrice) return 'limit-down';
    if (value > (refPrice ?? 0)) return 'up';
    return 'down';
}

/**
 * Tailwind CSS class map for price colors.
 */
export const PRICE_COLOR_CLASSES: Record<PriceColor, { text: string; bg: string; border: string }> = {
    up: { text: 'text-up', bg: 'bg-price-up-10', border: 'border-up' },
    down: { text: 'text-down', bg: 'bg-price-down-10', border: 'border-down' },
    'limit-up': { text: 'text-limit-up', bg: 'bg-limit-up/10', border: 'border-limit-up' },
    'limit-down': { text: 'text-limit-down', bg: 'bg-limit-down/10', border: 'border-limit-down' },
    reference: { text: 'text-reference', bg: 'bg-reference/10', border: 'border-reference' },
    neutral: { text: 'text-fg-muted', bg: 'bg-surface', border: 'border-border' },
};
