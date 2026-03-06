import { Injectable, signal, effect, inject, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';

export type Theme = 'dark' | 'light';

@Injectable({ providedIn: 'root' })
export class ThemeService {
    private readonly platformId = inject(PLATFORM_ID);

    private readonly _theme = signal<Theme>(this._getInitialTheme());
    readonly theme = this._theme.asReadonly();

    readonly isDark = () => this._theme() === 'dark';

    constructor() {
        effect(() => {
            if (isPlatformBrowser(this.platformId)) {
                const theme = this._theme();
                document.documentElement.classList.toggle('light-theme', theme === 'light');
                localStorage.setItem('quantiq-theme', theme);
            }
        });
    }

    toggle(): void {
        this._theme.update(t => (t === 'dark' ? 'light' : 'dark'));
    }

    setTheme(theme: Theme): void {
        this._theme.set(theme);
    }

    init(): void { }

    private _getInitialTheme(): Theme {
        if (!isPlatformBrowser(this.platformId)) return 'dark';
        const saved = localStorage.getItem('quantiq-theme') as Theme | null;
        if (saved === 'dark' || saved === 'light') return saved;
        return window.matchMedia('(prefers-color-scheme: light)').matches ? 'light' : 'dark';
    }
}
