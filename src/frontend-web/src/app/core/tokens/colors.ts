
export const COLORS = {
    up: 'oklch(0.54 0.24 120)',
    down: 'oklch(0.54 0.22 15)',
    limitUp: 'oklch(0.68 0.22 295)',
    limitDown: 'oklch(0.65 0.14 200)',
    reference: 'oklch(0.80 0.12 85)',

    bg: 'var(--color-bg)',
    surface: 'var(--color-surface)',
    surface2: 'var(--color-surface-2)',
    fg: 'var(--color-fg)',
    fgMuted: 'var(--color-fg-muted)',
    border: 'var(--color-border)',
    primary: 'var(--color-primary)',
} as const;

export type PriceColor = 'up' | 'down' | 'limit-up' | 'limit-down' | 'reference' | 'neutral';


export function getPriceColorClass(value: number, refPrice?: number, ceilPrice?: number, floorPrice?: number): PriceColor {
    if (value === 0) return 'neutral';
    if (ceilPrice !== undefined && value >= ceilPrice) return 'limit-up';
    if (floorPrice !== undefined && value <= floorPrice) return 'limit-down';
    if (value > (refPrice ?? 0)) return 'up';
    return 'down';
}


export const PRICE_COLOR_CLASSES: Record<PriceColor, { text: string; bg: string; border: string }> = {
    up: { text: 'text-up', bg: 'bg-price-up-10', border: 'border-up' },
    down: { text: 'text-down', bg: 'bg-price-down-10', border: 'border-down' },
    'limit-up': { text: 'text-limit-up', bg: 'bg-limit-up/10', border: 'border-limit-up' },
    'limit-down': { text: 'text-limit-down', bg: 'bg-limit-down/10', border: 'border-limit-down' },
    reference: { text: 'text-reference', bg: 'bg-reference/10', border: 'border-reference' },
    neutral: { text: 'text-fg-muted', bg: 'bg-surface', border: 'border-border' },
};
