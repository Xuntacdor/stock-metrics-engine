import {
  Component, input, output, computed,
  ChangeDetectionStrategy, booleanAttribute,
} from '@angular/core';
import { CommonModule } from '@angular/common';

export type ButtonVariant = 'primary' | 'secondary' | 'outline' | 'ghost' | 'danger';
export type ButtonSize = 'sm' | 'md' | 'lg';

@Component({
  selector: 'app-btn',
  standalone: true,
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <button
      [type]="type()"
      [disabled]="disabled() || loading()"
      [attr.aria-disabled]="disabled() || loading()"
      [attr.aria-label]="ariaLabel() || null"
      [attr.aria-busy]="loading()"
      [class]="buttonClasses()"
      (click)="onClick($event)"
    >
      <!-- Loading spinner -->
      @if (loading()) {
        <svg
          class="animate-spin shrink-0"
          [class]="iconSizeClass()"
          xmlns="http://www.w3.org/2000/svg"
          fill="none" viewBox="0 0 24 24"
          aria-hidden="true"
        >
          <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"/>
          <path class="opacity-75" fill="currentColor"
            d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"/>
        </svg>
      }

      <!-- Left icon slot -->
      @if (!loading() && iconLeft()) {
        <span class="shrink-0" [class]="iconSizeClass()" aria-hidden="true">
          <ng-content select="[slot=icon-left]" />
        </span>
      }

      <!-- Label -->
      @if (label()) {
        <span>{{ label() }}</span>
      } @else {
        <ng-content />
      }

      <!-- Right icon slot -->
      @if (iconRight()) {
        <span class="shrink-0" [class]="iconSizeClass()" aria-hidden="true">
          <ng-content select="[slot=icon-right]" />
        </span>
      }
    </button>
  `,
})
export class ButtonComponent {
  readonly variant = input<ButtonVariant>('primary');
  readonly size = input<ButtonSize>('md');
  readonly type = input<'button' | 'submit' | 'reset'>('button');
  readonly label = input<string>('');
  readonly ariaLabel = input<string>('');
  readonly disabled = input(false, { transform: booleanAttribute });
  readonly loading = input(false, { transform: booleanAttribute });
  readonly fullWidth = input(false, { transform: booleanAttribute });
  readonly iconLeft = input(false, { transform: booleanAttribute });
  readonly iconRight = input(false, { transform: booleanAttribute });

  readonly clicked = output<MouseEvent>();

  readonly buttonClasses = computed(() => {
    const base = [
      'inline-flex items-center justify-center gap-2',
      'font-medium rounded-md',
      'transition-all duration-150 ease-out',
      'focus-visible:outline-2 focus-visible:outline-offset-2',
      'disabled:opacity-40 disabled:cursor-not-allowed disabled:pointer-events-none',
      'min-touch select-none',
    ];

    const sizes: Record<ButtonSize, string> = {
      sm: 'h-8  px-3  text-xs  gap-1.5',
      md: 'h-10 px-4  text-body gap-2',
      lg: 'h-12 px-6  text-sm  gap-2',
    };

    const variants: Record<ButtonVariant, string> = {
      primary: 'bg-up text-primary-fg hover:opacity-90 active:scale-[0.98] shadow-sm',
      secondary: 'bg-surface-2 text-fg border border-border hover:border-border-hover hover:bg-surface active:scale-[0.98]',
      outline: 'bg-transparent text-fg border border-border hover:border-border-hover hover:bg-surface-2 active:scale-[0.98]',
      ghost: 'bg-transparent text-fg hover:bg-surface-2 active:scale-[0.98]',
      danger: 'bg-down text-white hover:opacity-90 active:scale-[0.98] shadow-sm',
    };

    return [
      ...base,
      sizes[this.size()],
      variants[this.variant()],
      this.fullWidth() ? 'w-full' : '',
    ].filter(Boolean).join(' ');
  });

  readonly iconSizeClass = computed(() => ({
    sm: 'w-3.5 h-3.5',
    md: 'w-4 h-4',
    lg: 'w-5 h-5',
  }[this.size()]));

  onClick(event: MouseEvent): void {
    if (!this.disabled() && !this.loading()) {
      this.clicked.emit(event);
    }
  }
}
