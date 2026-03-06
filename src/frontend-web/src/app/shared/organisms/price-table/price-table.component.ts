import {
  Component, input, output, computed, signal,
  ChangeDetectionStrategy, OnChanges,
} from '@angular/core';
import { CommonModule, DecimalPipe } from '@angular/common';
import { BadgeComponent, type BadgeVariant } from '../../atoms/badge/badge.component';
import { SkeletonComponent } from '../../atoms/skeleton/skeleton.component';
import { ButtonComponent } from '../../atoms/button/button.component';
import { IconComponent } from '../../atoms/icon/icon.component';
import { type PriceColor, getPriceColorClass } from '../../../core/tokens/colors';

export interface StockRow {
  symbol: string;
  name?: string;
  price: number;
  refPrice: number;
  ceilPrice: number;
  floorPrice: number;
  change: number;
  changePct: number;
  volume: number;
  value?: number;
  high?: number;
  low?: number;
}

export type PriceTableColumn = 'symbol' | 'price' | 'change' | 'changePct' | 'volume' | 'high' | 'low' | 'value' | 'action';


@Component({
  selector: 'app-price-table',
  standalone: true,
  imports: [CommonModule, DecimalPipe, BadgeComponent, SkeletonComponent, ButtonComponent, IconComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="w-full overflow-hidden rounded-xl border border-border">

      <!-- Table toolbar -->
      <div class="flex items-center justify-between px-4 py-3 bg-surface border-b border-border">
        <div class="flex items-center gap-2">
          <app-icon name="bar-chart-2" size="md" class="text-fg-muted" />
          <span class="text-small font-semibold text-fg">{{ title() }}</span>
          @if (!loading()) {
            <span class="text-xs text-fg-muted">({{ stocks().length }} mã)</span>
          }
        </div>
        <div class="flex items-center gap-2">
          <!-- Live indicator -->
          <span class="flex items-center gap-1.5 text-xs text-fg-muted">
            <span class="inline-block w-2 h-2 rounded-full bg-up animate-pulse-soft"></span>
            Live
          </span>
          <button
            class="p-1.5 rounded-md text-fg-muted hover:text-fg hover:bg-surface-2 transition-colors"
            aria-label="Làm mới bảng giá"
            (click)="refreshed.emit()"
          >
            <app-icon name="refresh-cw" size="sm" />
          </button>
        </div>
      </div>

      <!-- Table -->
      <div class="overflow-x-auto">
        <table class="w-full text-small" role="grid" aria-label="Bảng giá cổ phiếu">
          <thead>
            <tr class="bg-surface-2 border-b border-border">
              <th class="text-left px-4 py-2.5 text-xs text-fg-muted font-medium w-20">
                <button class="flex items-center gap-1 hover:text-fg transition-colors"
                        (click)="sort('symbol')">
                  Mã CK
                  <app-icon [name]="sortIcon('symbol')" size="xs" class="opacity-60" />
                </button>
              </th>
              <th class="text-right px-3 py-2.5 text-xs text-fg-muted font-medium">
                <span class="text-limit-up">Trần</span>
              </th>
              <th class="text-right px-3 py-2.5 text-xs text-fg-muted font-medium">
                <span class="text-limit-down">Sàn</span>
              </th>
              <th class="text-right px-3 py-2.5 text-xs text-fg-muted font-medium">
                <span class="text-reference">TC</span>
              </th>
              <th class="text-right px-3 py-2.5 text-xs text-fg-muted font-medium">
                <button class="flex items-center gap-1 ml-auto hover:text-fg transition-colors"
                        (click)="sort('price')">
                  Khớp lệnh
                  <app-icon [name]="sortIcon('price')" size="xs" class="opacity-60" />
                </button>
              </th>
              <th class="text-right px-3 py-2.5 text-xs text-fg-muted font-medium">+/-</th>
              <th class="text-right px-3 py-2.5 text-xs text-fg-muted font-medium">%</th>
              <th class="text-right px-3 py-2.5 text-xs text-fg-muted font-medium">
                <button class="flex items-center gap-1 ml-auto hover:text-fg transition-colors"
                        (click)="sort('volume')">
                  Khối lượng
                  <app-icon [name]="sortIcon('volume')" size="xs" class="opacity-60" />
                </button>
              </th>
              @if (showActions()) {
                <th class="px-3 py-2.5 text-xs text-fg-muted font-medium text-center">Lệnh</th>
              }
            </tr>
          </thead>

          <tbody>
            <!-- Skeleton rows while loading -->
            @if (loading()) {
              @for (i of skeletonRows; track i) {
                <tr class="border-b border-border/50">
                  <td class="px-4 py-3"><app-skeleton type="text" width="sm" height="md" /></td>
                  <td class="px-3 py-3 text-right"><app-skeleton type="price" width="sm" height="md" /></td>
                  <td class="px-3 py-3 text-right"><app-skeleton type="price" width="sm" height="md" /></td>
                  <td class="px-3 py-3 text-right"><app-skeleton type="price" width="sm" height="md" /></td>
                  <td class="px-3 py-3 text-right"><app-skeleton type="price" width="md" height="md" /></td>
                  <td class="px-3 py-3 text-right"><app-skeleton type="price" width="sm" height="md" /></td>
                  <td class="px-3 py-3 text-right"><app-skeleton type="price" width="sm" height="md" /></td>
                  <td class="px-3 py-3 text-right"><app-skeleton type="price" width="md" height="md" /></td>
                  @if (showActions()) {
                    <td class="px-3 py-3"><app-skeleton type="rect" width="md" height="md" /></td>
                  }
                </tr>
              }
            }

            <!-- Data rows -->
            @if (!loading()) {
              @for (stock of sortedStocks(); track stock.symbol) {
                <tr
                  class="border-b border-border/50 hover:bg-surface-2 transition-colors cursor-pointer group"
                  [attr.aria-label]="stock.symbol + ': ' + stock.price"
                  (click)="rowClicked.emit(stock)"
                >
                  <!-- Symbol -->
                  <td class="px-4 py-3">
                    <span class="font-semibold text-fg text-body">{{ stock.symbol }}</span>
                    @if (stock.name) {
                      <p class="text-xs text-fg-muted truncate max-w-[80px]">{{ stock.name }}</p>
                    }
                  </td>

                  <!-- Trần -->
                  <td class="px-3 py-3 text-right font-numeric text-limit-up text-small">
                    {{ stock.ceilPrice | number:'1.1-2' }}
                  </td>
                  <!-- Sàn -->
                  <td class="px-3 py-3 text-right font-numeric text-limit-down text-small">
                    {{ stock.floorPrice | number:'1.1-2' }}
                  </td>
                  <!-- TC -->
                  <td class="px-3 py-3 text-right font-numeric text-reference text-small">
                    {{ stock.refPrice | number:'1.1-2' }}
                  </td>

                  <!-- Giá khớp -->
                  <td class="px-3 py-3 text-right">
                    <span [class]="'font-numeric font-bold text-body ' + getPriceTextClass(stock)">
                      {{ stock.price | number:'1.1-2' }}
                    </span>
                  </td>

                  <!-- Thay đổi tuyệt đối -->
                  <td class="px-3 py-3 text-right">
                    <span [class]="'font-numeric text-small ' + getPriceTextClass(stock)">
                      {{ stock.change >= 0 ? '+' : '' }}{{ stock.change | number:'1.2-2' }}
                    </span>
                  </td>

                  <!-- % thay đổi -->
                  <td class="px-3 py-3 text-right">
                    <app-badge [variant]="getPriceBadgeVariant(stock)" size="sm">
                      {{ stock.change >= 0 ? '+' : '' }}{{ stock.changePct | number:'1.2-2' }}%
                    </app-badge>
                  </td>

                  <!-- Khối lượng -->
                  <td class="px-3 py-3 text-right">
                    <span class="font-numeric text-small text-fg">
                      {{ stock.volume | number }}
                    </span>
                  </td>

                  <!-- Actions -->
                  @if (showActions()) {
                    <td class="px-3 py-3">
                      <div class="flex items-center justify-center gap-1 opacity-0 group-hover:opacity-100 transition-opacity">
                        <app-btn
                          variant="primary" size="sm" label="Mua"
                          (clicked)="$event.stopPropagation(); buyClicked.emit(stock)"
                        />
                        <app-btn
                          variant="danger" size="sm" label="Bán"
                          (clicked)="$event.stopPropagation(); sellClicked.emit(stock)"
                        />
                      </div>
                    </td>
                  }
                </tr>
              }
            }

            <!-- Empty state -->
            @if (!loading() && stocks().length === 0) {
              <tr>
                <td [attr.colspan]="showActions() ? 9 : 8">
                  <div class="flex flex-col items-center justify-center py-12 gap-3 text-fg-muted">
                    <app-icon name="search" size="xl" />
                    <p class="text-small">Không có dữ liệu</p>
                  </div>
                </td>
              </tr>
            }
          </tbody>
        </table>
      </div>

      <!-- Market breadth footer -->
      @if (!loading() && showBreadth()) {
        <div class="flex items-center gap-4 px-4 py-2.5 bg-surface border-t border-border text-xs">
          <span class="text-fg-muted">Tổng hợp:</span>
          <span class="text-up font-medium">↑ {{ upCount() }} tăng</span>
          <span class="text-reference font-medium">= {{ refCount() }} đứng</span>
          <span class="text-down font-medium">↓ {{ downCount() }} giảm</span>
          <span class="text-limit-up font-medium">⌃ {{ ceilCount() }} trần</span>
          <span class="text-limit-down font-medium">⌄ {{ floorCount() }} sàn</span>
        </div>
      }
    </div>
  `,
})
export class PriceTableComponent {
  readonly stocks = input.required<StockRow[]>();
  readonly title = input('Bảng giá');
  readonly loading = input(false);
  readonly showActions = input(false);
  readonly showBreadth = input(true);

  readonly rowClicked = output<StockRow>();
  readonly buyClicked = output<StockRow>();
  readonly sellClicked = output<StockRow>();
  readonly refreshed = output<void>();

  readonly sortBy = signal<keyof StockRow>('symbol');
  readonly sortAsc = signal(true);

  readonly skeletonRows = Array(8).fill(0).map((_, i) => i);

  readonly sortedStocks = computed(() => {
    const key = this.sortBy();
    const asc = this.sortAsc();
    return [...this.stocks()].sort((a, b) => {
      const av = a[key] as number | string ?? '';
      const bv = b[key] as number | string ?? '';
      const cmp = av < bv ? -1 : av > bv ? 1 : 0;
      return asc ? cmp : -cmp;
    });
  });

  readonly upCount = computed(() => this.stocks().filter(s => s.change > 0 && s.price < s.ceilPrice).length);
  readonly downCount = computed(() => this.stocks().filter(s => s.change < 0 && s.price > s.floorPrice).length);
  readonly refCount = computed(() => this.stocks().filter(s => s.change === 0).length);
  readonly ceilCount = computed(() => this.stocks().filter(s => s.price >= s.ceilPrice).length);
  readonly floorCount = computed(() => this.stocks().filter(s => s.price <= s.floorPrice).length);

  sort(key: keyof StockRow): void {
    if (this.sortBy() === key) {
      this.sortAsc.update(v => !v);
    } else {
      this.sortBy.set(key);
      this.sortAsc.set(true);
    }
  }

  sortIcon(key: keyof StockRow): string {
    if (this.sortBy() !== key) return 'minus';
    return this.sortAsc() ? 'arrow-up' : 'arrow-down';
  }

  getPriceColor(stock: StockRow): PriceColor {
    return getPriceColorClass(stock.price, stock.refPrice, stock.ceilPrice, stock.floorPrice);
  }

  getPriceTextClass(stock: StockRow): string {
    const map: Record<PriceColor, string> = {
      up: 'text-up',
      down: 'text-down',
      'limit-up': 'text-limit-up',
      'limit-down': 'text-limit-down',
      reference: 'text-reference',
      neutral: 'text-fg-muted',
    };
    return map[this.getPriceColor(stock)];
  }

  getPriceBadgeVariant(stock: StockRow): BadgeVariant {
    const color = this.getPriceColor(stock);
    const map: Record<PriceColor, BadgeVariant> = {
      up: 'up',
      down: 'down',
      'limit-up': 'limit-up',
      'limit-down': 'limit-down',
      reference: 'reference',
      neutral: 'neutral',
    };
    return map[color];
  }
}
