import { Component, inject, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ToastService, Toast } from '../../../core/services/toast.service';

@Component({
    selector: 'app-toast',
    standalone: true,
    imports: [CommonModule],
    changeDetection: ChangeDetectionStrategy.OnPush,
    styles: [`
        .toast-container {
            position: fixed;
            top: 1.25rem;
            right: 1.25rem;
            z-index: 9999;
            display: flex;
            flex-direction: column;
            gap: .625rem;
            max-width: 22rem;
            pointer-events: none;
        }
        .toast {
            display: flex;
            align-items: flex-start;
            gap: .625rem;
            padding: .75rem 1rem;
            border-radius: .75rem;
            font-size: .8125rem;
            font-weight: 500;
            line-height: 1.4;
            box-shadow: 0 4px 24px rgba(0,0,0,.35);
            pointer-events: all;
            animation: slide-in .2s ease;
        }
        @keyframes slide-in {
            from { opacity: 0; transform: translateX(2rem); }
            to   { opacity: 1; transform: translateX(0); }
        }
        .toast-success  { background: #14532d; color: #86efac; border: 1px solid #166534; }
        .toast-error    { background: #450a0a; color: #fca5a5; border: 1px solid #7f1d1d; }
        .toast-warning  { background: #451a03; color: #fcd34d; border: 1px solid #78350f; }
        .toast-info     { background: #1e1b4b; color: #a5b4fc; border: 1px solid #312e81; }
        .toast-icon     { font-size: .95rem; line-height: 1.4; flex-shrink: 0; }
        .toast-close    { margin-left: auto; cursor: pointer; opacity: .6; flex-shrink: 0; line-height: 1; background: none; border: none; color: inherit; padding: 0; font-size: 1rem; }
        .toast-close:hover { opacity: 1; }
    `],
    template: `
        <div class="toast-container" aria-live="polite" aria-atomic="false">
            @for (t of toast.toasts(); track t.id) {
                <div [class]="'toast toast-' + t.variant" role="alert">
                    <span class="toast-icon">{{ iconFor(t.variant) }}</span>
                    <span>{{ t.message }}</span>
                    <button class="toast-close" (click)="toast.dismiss(t.id)" aria-label="Đóng">✕</button>
                </div>
            }
        </div>
    `,
})
export class ToastComponent {
    readonly toast = inject(ToastService);

    iconFor(variant: Toast['variant']): string {
        return { success: '✓', error: '✕', warning: '⚠', info: 'ℹ' }[variant];
    }
}
