import {
  Component, input, computed,
  ChangeDetectionStrategy, OnChanges,
} from '@angular/core';
import { CommonModule, DecimalPipe } from '@angular/common';
import { IconComponent } from '../../atoms/icon/icon.component';
import { BadgeComponent } from '../../atoms/badge/badge.component';
import { type PriceColor, getPriceColorClass } from '../../../core/tokens/colors';

export interface PriceData {
  symbol: string;
  price: number;
  refPrice: number;
  ceilPrice: number;
  floorPrice: number;
  change: number;
  changePct: number;
  volume: number;
}


@Component({
  selector: 'app-price-display',
  standalone: true,
  imports: [CommonModule, DecimalPipe, IconComponent, BadgeComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <!-- COMPACT layout (dùng trong bảng giá) -->
    @if (layout() === 'compact') {
      <div class="flex items-center gap-1.5 font-numeric">
        <span [class]="'text-body font-semibold ' + textColorClass()">
          {{ data().price | number:'1.1-2' }}
        </span>
        <span [class]="'text-xs ' + textColorClass()">
          {{ changeSign() }}{{ data().changePct | number:'1.2-2' }}%
        </span>
      </div>
    }

    <!-- FULL layout (dùng trong header Stock Detail) -->
    @if (layout() === 'full') {
      <div class="space-y-1">
        <!-- Main price -->
        <div class="flex items-baseline gap-2">
          <span [class]="'text-display font-bold font-numeric ' + textColorClass()">
            {{ data().price | number:'1.2-2' }}
          </span>
          <!-- Trần/Sàn badge -->
          @if (priceColor() === 'limit-up') {
            <app-badge variant="limit-up" size="sm">TRẦN</app-badge>
          }
          @if (priceColor() === 'limit-down') {
            <app-badge variant="limit-down" size="sm">SÀN</app-badge>
          }
        </div>

        <!-- Change + Pct -->
        <div class="flex items-center gap-2">
          <app-icon
            [name]="data().change >= 0 ? 'trending-up' : 'trending-down'"
            size="sm"
            [class]="textColorClass()"
          />
          <span [class]="'text-small font-medium font-numeric ' + textColorClass()">
            {{ changeSign() }}{{ data().change | number:'1.2-2' }}
            ({{ changeSign() }}{{ data().changePct | number:'1.2-2' }}%)
          </span>
        </div>

        <!-- Reference / Ceil / Floor row -->
        <div class="flex items-center gap-4 text-xs text-fg-muted font-numeric">
          <span>TC: <span class="text-reference font-medium">{{ data().refPrice | number:'1.2-2' }}</span></span>
          <span>Trần: <span class="text-limit-up font-medium">{{ data().ceilPrice | number:'1.2-2' }}</span></span>
          <span>Sàn: <span class="text-limit-down font-medium">{{ data().floorPrice | number:'1.2-2' }}</span></span>
        </div>

        <!-- Volume -->
        @if (showVolume()) {
          <div class="text-xs text-fg-muted">
            KL: <span class="text-fg font-medium font-numeric">{{ data().volume | number }}</span>
          </div>
        }
      </div>
    }

    <!-- BADGE layout (dùng trong watchlist / trending) -->
    @if (layout() === 'badge') {
      <div class="flex items-center gap-2">
        <span class="text-body font-semibold font-numeric text-fg">{{ data().symbol }}</span>
        <span [class]="'text-body font-semibold font-numeric ' + textColorClass()">
          {{ data().price | number:'1.2-2' }}
        </span>
        <app-badge [variant]="priceColor()" size="sm">
          {{ changeSign() }}{{ data().changePct | number:'1.2-2' }}%
        </app-badge>
      </div>
    }
  `,
})
export class PriceDisplayComponent {
  readonly data = input.required<PriceData>();
  readonly layout = input<'compact' | 'full' | 'badge'>('compact');
  readonly showVolume = input(false);
  readonly animate = input(true);

  readonly priceColor = computed<PriceColor>(() => {
    const d = this.data();
    return getPriceColorClass(d.price, d.refPrice, d.ceilPrice, d.floorPrice);
  });

  readonly textColorClass = computed(() => {
    const map: Record<PriceColor, string> = {
      up: 'text-up',
      down: 'text-down',
      'limit-up': 'text-limit-up',
      'limit-down': 'text-limit-down',
      reference: 'text-reference',
      neutral: 'text-fg-muted',
    };
    return map[this.priceColor()];
  });

  readonly changeSign = computed(() => this.data().change >= 0 ? '+' : '');
}
