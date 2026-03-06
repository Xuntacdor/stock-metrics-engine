import {
  Component, input, computed,
  ChangeDetectionStrategy,
} from '@angular/core';
import { CommonModule, DecimalPipe } from '@angular/common';
import { IconComponent } from '../../atoms/icon/icon.component';

export type StatTrend = 'up' | 'down' | 'neutral';

export interface StatBoxData {
  title: string;
  value: string | number;
  prefix?: string;
  suffix?: string;
  change?: number;
  trend?: StatTrend;
  icon?: string;
  caption?: string;
}


@Component({
  selector: 'app-stat-box',
  standalone: true,
  imports: [CommonModule, DecimalPipe, IconComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="card flex flex-col gap-3 h-full">

      <!-- Header: title + icon -->
      <div class="flex items-start justify-between">
        <span class="text-small text-fg-muted font-medium">{{ data().title }}</span>
        @if (data().icon) {
          <span [class]="'p-2 rounded-lg ' + iconBgClass()">
            <app-icon [name]="data().icon!" size="md" [class]="iconColorClass()" />
          </span>
        }
      </div>

      <!-- Value -->
      <div class="flex items-baseline gap-1 font-numeric">
        @if (data().prefix) {
          <span class="text-small text-fg-muted">{{ data().prefix }}</span>
        }
        <span class="text-headline font-bold text-fg">
          @if (isNumeric()) {
            {{ +data().value | number:'1.0-2' }}
          } @else {
            {{ data().value }}
          }
        </span>
        @if (data().suffix) {
          <span class="text-small text-fg-muted">{{ data().suffix }}</span>
        }
      </div>

      <!-- Change badge + caption -->
      <div class="flex items-center justify-between gap-2">
        @if (data().change !== undefined) {
          <div [class]="'flex items-center gap-1 text-xs font-medium ' + changeColorClass()">
            <app-icon [name]="trendIcon()" size="xs" />
            <span class="font-numeric">{{ changeSign() }}{{ data().change | number:'1.2-2' }}%</span>
          </div>
        }
        @if (data().caption) {
          <span class="text-xs text-fg-muted truncate">{{ data().caption }}</span>
        }
      </div>
    </div>
  `,
})
export class StatBoxComponent {
  readonly data = input.required<StatBoxData>();

  readonly isNumeric = computed(() => typeof this.data().value === 'number' || !isNaN(Number(this.data().value)));

  readonly effectiveTrend = computed<StatTrend>(() => {
    if (this.data().trend) return this.data().trend!;
    if (this.data().change !== undefined) {
      return this.data().change! > 0 ? 'up' : this.data().change! < 0 ? 'down' : 'neutral';
    }
    return 'neutral';
  });

  readonly changeColorClass = computed(() => ({
    up: 'text-up',
    down: 'text-down',
    neutral: 'text-fg-muted',
  }[this.effectiveTrend()]));

  readonly iconBgClass = computed(() => ({
    up: 'bg-price-up-10',
    down: 'bg-price-down-10',
    neutral: 'bg-surface-2',
  }[this.effectiveTrend()]));

  readonly iconColorClass = computed(() => ({
    up: 'text-up',
    down: 'text-down',
    neutral: 'text-fg-muted',
  }[this.effectiveTrend()]));

  readonly trendIcon = computed(() => ({
    up: 'trending-up',
    down: 'trending-down',
    neutral: 'minus',
  }[this.effectiveTrend()]));

  readonly changeSign = computed(() => (this.data().change ?? 0) >= 0 ? '+' : '');
}
