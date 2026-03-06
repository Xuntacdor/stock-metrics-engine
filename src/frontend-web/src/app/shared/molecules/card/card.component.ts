import {
  Component, input, computed,
  ChangeDetectionStrategy, booleanAttribute,
} from '@angular/core';
import { CommonModule } from '@angular/common';

export type CardVariant = 'default' | 'elevated' | 'outlined' | 'ghost';
export type CardPadding = 'none' | 'sm' | 'md' | 'lg';


@Component({
  selector: 'app-card',
  standalone: true,
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div [class]="cardClasses()">

      <!-- Header: title + optional action slot -->
      @if (title() || hasHeaderAction()) {
        <div class="flex items-center justify-between mb-4"
             [class.pb-3]="true"
             [class.border-b]="divideHeader()"
             [class.border-border]="divideHeader()">
          @if (title()) {
            <div>
              <h3 class="text-title font-semibold text-fg leading-tight">{{ title() }}</h3>
              @if (subtitle()) {
                <p class="text-small text-fg-muted mt-0.5">{{ subtitle() }}</p>
              }
            </div>
          }
          <div class="flex items-center gap-2">
            <ng-content select="[slot=header-action]" />
          </div>
        </div>
      }

      <!-- Main content -->
      <div [class]="contentClasses()">
        <ng-content />
      </div>

      <!-- Footer slot -->
      <ng-content select="[slot=footer]" />
    </div>
  `,
})
export class CardComponent {
  readonly variant = input<CardVariant>('default');
  readonly padding = input<CardPadding>('md');
  readonly title = input<string>('');
  readonly subtitle = input<string>('');
  readonly hoverable = input(false, { transform: booleanAttribute });
  readonly interactive = input(false, { transform: booleanAttribute });
  readonly divideHeader = input(true, { transform: booleanAttribute });
  readonly fullHeight = input(false, { transform: booleanAttribute });
  readonly hasHeaderAction = input(false, { transform: booleanAttribute });

  readonly cardClasses = computed(() => {
    const base = ['rounded-xl border transition-all duration-200'];

    const variants: Record<CardVariant, string> = {
      default: 'bg-surface border-border',
      elevated: 'bg-surface-2 border-border shadow-card-md',
      outlined: 'bg-transparent border-border',
      ghost: 'bg-transparent border-transparent',
    };

    const paddings: Record<CardPadding, string> = {
      none: '',
      sm: 'p-3',
      md: 'p-4',
      lg: 'p-6',
    };

    return [
      ...base,
      variants[this.variant()],
      paddings[this.padding()],
      this.hoverable() ? 'hover:border-border-hover hover:shadow-card-md cursor-pointer' : '',
      this.interactive() ? 'hover:border-border-hover active:scale-[0.99]' : '',
      this.fullHeight() ? 'h-full' : '',
    ].filter(Boolean).join(' ');
  });

  readonly contentClasses = computed(() => '');
}
