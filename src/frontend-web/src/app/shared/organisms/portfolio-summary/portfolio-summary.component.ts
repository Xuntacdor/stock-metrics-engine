import {
  Component, input, computed, signal,
  ChangeDetectionStrategy,
} from '@angular/core';
import { CommonModule, DecimalPipe } from '@angular/common';
import { IconComponent } from '../../atoms/icon/icon.component';
import { BadgeComponent } from '../../atoms/badge/badge.component';
import { SkeletonComponent } from '../../atoms/skeleton/skeleton.component';
import { CardComponent } from '../../molecules/card/card.component';
import { StatBoxComponent, type StatBoxData } from '../../molecules/stat-box/stat-box.component';

export interface Holding {
  symbol: string;
  name?: string;
  quantity: number;
  avgCost: number;
  currentPrice: number;
  marketValue: number;
  unrealizedPnL: number;
  unrealizedPct: number;
  sector?: string;
}

@Component({
  selector: 'app-portfolio-summary',
  standalone: true,
  imports: [CommonModule, DecimalPipe, IconComponent, BadgeComponent, SkeletonComponent, CardComponent, StatBoxComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="space-y-6">

      <!-- KPI Stats Row -->
      @if (loading()) {
        <div class="grid grid-cols-2 md:grid-cols-4 gap-4">
          @for (i of [1,2,3,4]; track i) {
            <div class="card space-y-3">
              <app-skeleton type="text" width="lg" height="sm" />
              <app-skeleton type="text" width="full" height="lg" />
              <app-skeleton type="text" width="md" height="sm" />
            </div>
          }
        </div>
      } @else {
        <div class="grid grid-cols-2 md:grid-cols-4 gap-4">
          <app-stat-box [data]="totalValueStat()" />
          <app-stat-box [data]="cashStat()" />
          <app-stat-box [data]="unrealizedStat()" />
          <app-stat-box [data]="realizedStat()" />
        </div>
      }

      <!-- Holdings Table -->
      <app-card title="Danh mục đầu tư" [hasHeaderAction]="true">
        <ng-container slot="header-action">
          <span class="text-xs text-fg-muted">{{ holdings().length }} vị thế</span>
        </ng-container>

        @if (loading()) {
          <div class="space-y-3 py-2">
            @for (i of [1,2,3,4,5]; track i) {
              <div class="flex gap-4 items-center">
                <app-skeleton type="text" width="sm" height="md" />
                <app-skeleton type="text" width="sm" height="md" />
                <app-skeleton type="price" width="md" height="md" />
                <app-skeleton type="price" width="md" height="md" />
                <app-skeleton type="price" width="md" height="md" />
                <app-skeleton type="price" width="sm" height="md" />
              </div>
            }
          </div>
        } @else {
          <!-- Desktop table -->
          <div class="overflow-x-auto -mx-4 hidden md:block">
            <table class="w-full text-small" role="grid">
              <thead>
                <tr class="text-xs text-fg-muted border-b border-border">
                  <th class="text-left px-4 py-2.5 font-medium">Mã CK</th>
                  <th class="text-right px-3 py-2.5 font-medium">SL</th>
                  <th class="text-right px-3 py-2.5 font-medium">Giá vốn</th>
                  <th class="text-right px-3 py-2.5 font-medium">Giá TT</th>
                  <th class="text-right px-3 py-2.5 font-medium">GT thị trường</th>
                  <th class="text-right px-3 py-2.5 font-medium">Lãi/Lỗ</th>
                  <th class="text-right px-3 py-2.5 font-medium">%</th>
                  <th class="text-right px-3 py-2.5 font-medium">Tỷ trọng</th>
                </tr>
              </thead>
              <tbody>
                @for (h of holdings(); track h.symbol) {
                  <tr class="border-b border-border/50 hover:bg-surface-2 transition-colors">
                    <td class="px-4 py-3">
                      <span class="font-semibold text-fg">{{ h.symbol }}</span>
                      @if (h.sector) {
                        <p class="text-xs text-fg-muted">{{ h.sector }}</p>
                      }
                    </td>
                    <td class="px-3 py-3 text-right font-numeric text-fg">{{ h.quantity | number }}</td>
                    <td class="px-3 py-3 text-right font-numeric text-fg">{{ h.avgCost | number:'1.1-2' }}</td>
                    <td class="px-3 py-3 text-right font-numeric text-fg">{{ h.currentPrice | number:'1.1-2' }}</td>
                    <td class="px-3 py-3 text-right font-numeric text-fg font-medium">
                      {{ h.marketValue | number:'1.0-0' }}
                    </td>
                    <td class="px-3 py-3 text-right font-numeric"
                        [class]="h.unrealizedPnL >= 0 ? 'text-up' : 'text-down'">
                      {{ h.unrealizedPnL >= 0 ? '+' : '' }}{{ h.unrealizedPnL | number:'1.0-0' }}
                    </td>
                    <td class="px-3 py-3 text-right">
                      <app-badge [variant]="h.unrealizedPct >= 0 ? 'up' : 'down'" size="sm">
                        {{ h.unrealizedPct >= 0 ? '+' : '' }}{{ h.unrealizedPct | number:'1.2-2' }}%
                      </app-badge>
                    </td>
                    <td class="px-3 py-3 text-right">
                      <div class="flex items-center justify-end gap-2">
                        <div class="w-16 h-1.5 rounded-full bg-surface-2 overflow-hidden">
                          <div
                            class="h-full rounded-full bg-up"
                            [style.width]="getWeight(h) + '%'"
                          ></div>
                        </div>
                        <span class="font-numeric text-xs text-fg-muted w-8 text-right">
                          {{ getWeight(h) | number:'1.1-1' }}%
                        </span>
                      </div>
                    </td>
                  </tr>
                }
              </tbody>
            </table>
          </div>

          <!-- Mobile cards -->
          <div class="md:hidden space-y-3 pt-2">
            @for (h of holdings(); track h.symbol) {
              <div class="rounded-lg border border-border p-3 space-y-2">
                <div class="flex items-center justify-between">
                  <span class="font-semibold text-fg">{{ h.symbol }}</span>
                  <app-badge [variant]="h.unrealizedPct >= 0 ? 'up' : 'down'" size="sm">
                    {{ h.unrealizedPct >= 0 ? '+' : '' }}{{ h.unrealizedPct | number:'1.2-2' }}%
                  </app-badge>
                </div>
                <div class="grid grid-cols-3 gap-2 text-xs">
                  <div>
                    <p class="text-fg-muted">Số lượng</p>
                    <p class="font-numeric text-fg">{{ h.quantity | number }}</p>
                  </div>
                  <div>
                    <p class="text-fg-muted">Giá vốn</p>
                    <p class="font-numeric text-fg">{{ h.avgCost | number:'1.1-2' }}</p>
                  </div>
                  <div>
                    <p class="text-fg-muted">Giá TT</p>
                    <p class="font-numeric text-fg">{{ h.currentPrice | number:'1.1-2' }}</p>
                  </div>
                </div>
                <div class="flex items-center justify-between text-xs pt-1 border-t border-border">
                  <span class="text-fg-muted">Lãi/Lỗ:</span>
                  <span class="font-numeric font-semibold"
                        [class]="h.unrealizedPnL >= 0 ? 'text-up' : 'text-down'">
                    {{ h.unrealizedPnL >= 0 ? '+' : '' }}{{ h.unrealizedPnL | number:'1.0-0' }} đ
                  </span>
                </div>
              </div>
            }
          </div>

          <!-- Total row -->
          <div class="flex items-center justify-between pt-4 mt-2 border-t border-border">
            <span class="text-small font-semibold text-fg">Tổng cổ phiếu</span>
            <div class="flex items-center gap-6 text-small font-numeric">
              <span class="text-fg-muted">GT: <span class="text-fg font-semibold">{{ totalMarketValue() | number:'1.0-0' }}</span></span>
              <span [class]="totalUnrealizedPnL() >= 0 ? 'text-up font-semibold' : 'text-down font-semibold'">
                {{ totalUnrealizedPnL() >= 0 ? '+' : '' }}{{ totalUnrealizedPnL() | number:'1.0-0' }} đ
              </span>
            </div>
          </div>
        }
      </app-card>
    </div>
  `,
})
export class PortfolioSummaryComponent {
  readonly holdings = input.required<Holding[]>();
  readonly cashBalance = input(0);
  readonly realizedPnL = input(0);
  readonly loading = input(false);

  readonly totalMarketValue = computed(() =>
    this.holdings().reduce((s, h) => s + h.marketValue, 0)
  );

  readonly totalUnrealizedPnL = computed(() =>
    this.holdings().reduce((s, h) => s + h.unrealizedPnL, 0)
  );

  readonly totalPortfolio = computed(() =>
    this.totalMarketValue() + this.cashBalance()
  );

  readonly totalValueStat = computed<StatBoxData>(() => ({
    title: 'Tổng tài sản',
    value: this.totalPortfolio(),
    prefix: '₫',
    icon: 'wallet',
    trend: this.totalUnrealizedPnL() >= 0 ? 'up' : 'down',
    change: this.totalPortfolio() > 0
      ? (this.totalUnrealizedPnL() / (this.totalPortfolio() - this.totalUnrealizedPnL())) * 100
      : 0,
    caption: 'CP + Tiền mặt',
  }));

  readonly cashStat = computed<StatBoxData>(() => ({
    title: 'Tiền mặt',
    value: this.cashBalance(),
    prefix: '₫',
    icon: 'dollar-sign',
    trend: 'neutral',
    caption: 'Sức mua khả dụng',
  }));

  readonly unrealizedStat = computed<StatBoxData>(() => ({
    title: 'Lãi/Lỗ chưa TH',
    value: this.totalUnrealizedPnL(),
    prefix: '₫',
    icon: this.totalUnrealizedPnL() >= 0 ? 'trending-up' : 'trending-down',
    trend: this.totalUnrealizedPnL() >= 0 ? 'up' : 'down',
    change: this.totalPortfolio() > 0
      ? (this.totalUnrealizedPnL() / this.totalPortfolio()) * 100
      : 0,
    caption: 'Unrealized P&L',
  }));

  readonly realizedStat = computed<StatBoxData>(() => ({
    title: 'Lãi/Lỗ đã TH',
    value: this.realizedPnL(),
    prefix: '₫',
    icon: this.realizedPnL() >= 0 ? 'check-circle' : 'x-circle',
    trend: this.realizedPnL() >= 0 ? 'up' : 'down',
    caption: 'Realized P&L',
  }));

  getWeight(h: Holding): number {
    const total = this.totalMarketValue();
    return total > 0 ? (h.marketValue / total) * 100 : 0;
  }
}
