import { Injectable, signal } from '@angular/core';

export type ToastVariant = 'success' | 'error' | 'warning' | 'info';

export interface Toast {
    id: number;
    message: string;
    variant: ToastVariant;
}

let nextId = 0;

@Injectable({ providedIn: 'root' })
export class ToastService {
    private readonly _toasts = signal<Toast[]>([]);
    readonly toasts = this._toasts.asReadonly();

    show(message: string, variant: ToastVariant = 'info', duration = 4000): void {
        const id = ++nextId;
        this._toasts.update(list => [...list, { id, message, variant }]);
        setTimeout(() => this.dismiss(id), duration);
    }

    success(message: string, duration?: number): void { this.show(message, 'success', duration); }
    error(message: string, duration?: number): void    { this.show(message, 'error', duration ?? 6000); }
    warning(message: string, duration?: number): void  { this.show(message, 'warning', duration); }
    info(message: string, duration?: number): void     { this.show(message, 'info', duration); }

    dismiss(id: number): void {
        this._toasts.update(list => list.filter(t => t.id !== id));
    }
}
