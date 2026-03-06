/** @type {import('tailwindcss').Config} */
module.exports = {
  content: [
    './src/**/*.{html,ts}',
  ],
  darkMode: 'class',
  theme: {
    extend: {
      colors: {
        // Semantic Vietnamese Stock Market Colors
        up: 'var(--color-up)',
        down: 'var(--color-down)',
        'limit-up': 'var(--color-limit-up)',
        'limit-down': 'var(--color-limit-down)',
        reference: 'var(--color-reference)',
        // Surface System
        bg: 'var(--color-bg)',
        surface: 'var(--color-surface)',
        'surface-2': 'var(--color-surface-2)',
        fg: 'var(--color-fg)',
        'fg-muted': 'var(--color-fg-muted)',
        border: 'var(--color-border)',
        // Brand
        primary: 'var(--color-primary)',
        'primary-fg': 'var(--color-primary-fg)',
      },
      fontFamily: {
        sans: ['Inter', 'system-ui', 'sans-serif'],
        mono: ['JetBrains Mono', 'Fira Code', 'monospace'],
      },
      fontSize: {
        'display': ['2rem', { lineHeight: '1.2', fontWeight: '700' }],
        'headline': ['1.5rem', { lineHeight: '1.3', fontWeight: '600' }],
        'title': ['1.125rem', { lineHeight: '1.4', fontWeight: '600' }],
        'body': ['0.875rem', { lineHeight: '1.5', fontWeight: '400' }],
        'small': ['0.75rem', { lineHeight: '1.5', fontWeight: '400' }],
        'xs': ['0.6875rem', { lineHeight: '1.4', fontWeight: '400' }],
      },
      spacing: {
        'xs': '0.25rem',  // 4px
        'sm': '0.5rem',   // 8px
        'md': '1rem',     // 16px
        'lg': '1.5rem',   // 24px
        'xl': '2rem',     // 32px
        '2xl': '3rem',     // 48px
      },
      borderRadius: {
        'sm': '0.25rem',
        'md': '0.5rem',
        'lg': '0.75rem',
        'xl': '1rem',
        '2xl': '1.5rem',
      },
      keyframes: {
        'fade-in': {
          '0%': { opacity: '0', transform: 'translateY(4px)' },
          '100%': { opacity: '1', transform: 'translateY(0)' },
        },
        'slide-in': {
          '0%': { opacity: '0', transform: 'translateX(-8px)' },
          '100%': { opacity: '1', transform: 'translateX(0)' },
        },
        'price-up': {
          '0%': { backgroundColor: 'transparent' },
          '30%': { backgroundColor: 'oklch(0.54 0.24 120 / 0.3)' },
          '100%': { backgroundColor: 'transparent' },
        },
        'price-down': {
          '0%': { backgroundColor: 'transparent' },
          '30%': { backgroundColor: 'oklch(0.54 0.22 15 / 0.3)' },
          '100%': { backgroundColor: 'transparent' },
        },
        'pulse-soft': {
          '0%, 100%': { opacity: '1' },
          '50%': { opacity: '0.4' },
        },
        'shimmer': {
          '0%': { backgroundPosition: '-200% 0' },
          '100%': { backgroundPosition: '200% 0' },
        },
      },
      animation: {
        'fade-in': 'fade-in 0.2s ease-out',
        'slide-in': 'slide-in 0.2s ease-out',
        'price-up': 'price-up 0.8s ease-out',
        'price-down': 'price-down 0.8s ease-out',
        'pulse-soft': 'pulse-soft 1.5s ease-in-out infinite',
        'shimmer': 'shimmer 1.5s linear infinite',
      },
      boxShadow: {
        'card': '0 1px 3px 0 rgb(0 0 0 / 0.4), 0 1px 2px -1px rgb(0 0 0 / 0.4)',
        'card-md': '0 4px 6px -1px rgb(0 0 0 / 0.5), 0 2px 4px -2px rgb(0 0 0 / 0.5)',
        'glow-up': '0 0 12px oklch(0.54 0.24 120 / 0.4)',
        'glow-down': '0 0 12px oklch(0.54 0.22 15 / 0.4)',
      },
    },
  },
  plugins: [],
};
