import {
  Component, input, computed,
  ChangeDetectionStrategy,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { type PriceColor, PRICE_COLOR_CLASSES } from '../../../core/tokens/colors';

export type BadgeVariant = PriceColor | 'default' | 'info' | 'warning';
export type BadgeSize = 'sm' | 'md';

@Component({
  selector: 'app-badge',
  standalone: true,
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <span
      [class]="badgeClasses()"
      [attr.aria-label]="ariaLabel() || null"
    >
      <!-- Optional dot indicator -->
      @if (dot()) {
        <span
          class="inline-block rounded-full shrink-0"
          [class]="dotClass()"
          aria-hidden="true"
        ></span>
      }

      <!-- Label or content -->
      @if (label()) {
        {{ label() }}
      } @else {
        <ng-content />
      }
    </span>
  `,
})
export class BadgeComponent {
  readonly variant = input<BadgeVariant>('default');
  readonly size = input<BadgeSize>('md');
  readonly label = input<string>('');
  readonly ariaLabel = input<string>('');
  readonly dot = input(false);
  readonly pill = input(true);

  readonly badgeClasses = computed(() => {
    const base = [
      'inline-flex items-center gap-1 font-medium',
      'border',
      this.pill() ? 'rounded-full' : 'rounded-md',
    ];

    const sizes: Record<BadgeSize, string> = {
      sm: 'px-1.5 py-0.5 text-xs',
      md: 'px-2   py-0.5 text-xs',
    };

    const variants: Record<BadgeVariant, string> = {
      up: 'text-up bg-price-up-10 border-up/30',
      down: 'text-down bg-price-down-10 border-down/30',
      'limit-up': 'text-limit-up bg-limit-up/10 border-limit-up/30',
      'limit-down': 'text-limit-down bg-limit-down/10 border-limit-down/30',
      reference: 'text-reference bg-reference/10 border-reference/30',
      neutral: 'text-fg-muted bg-surface-2 border-border',
      default: 'text-fg-muted bg-surface-2 border-border',
      info: 'text-sky-400 bg-sky-400/10 border-sky-400/30',
      warning: 'text-amber-400 bg-amber-400/10 border-amber-400/30',
    };

    return [
      ...base,
      sizes[this.size()],
      variants[this.variant()],
    ].join(' ');
  });

  readonly dotClass = computed(() => {
    const size = { sm: 'w-1.5 h-1.5', md: 'w-2 h-2' }[this.size()];
    const colors: Record<BadgeVariant, string> = {
      up: 'bg-up',
      down: 'bg-down',
      'limit-up': 'bg-limit-up',
      'limit-down': 'bg-limit-down',
      reference: 'bg-reference',
      neutral: 'bg-fg-muted',
      default: 'bg-fg-muted',
      info: 'bg-sky-400',
      warning: 'bg-amber-400',
    };
    return `${size} ${colors[this.variant()]}`;
  });
}
