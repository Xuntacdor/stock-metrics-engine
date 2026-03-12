import { Component, input, signal, computed, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { CandleChartComponent, type CandlePoint } from '../../shared/organisms/candle-chart/candle-chart.component';
import { CardComponent } from '../../shared/molecules/card/card.component';
import { PriceDisplayComponent, type PriceData } from '../../shared/molecules/price-display/price-display.component';
import { BadgeComponent } from '../../shared/atoms/badge/badge.component';
import { ButtonComponent } from '../../shared/atoms/button/button.component';
import { IconComponent } from '../../shared/atoms/icon/icon.component';
import { ChartService } from '../../core/services/chart.service';
import { MarketDataService } from '../../core/services/market-data.service';
import { inject, OnInit } from '@angular/core';

@Component({
  selector: 'app-stock-detail',
  standalone: true,
  imports: [CommonModule, CandleChartComponent, CardComponent, PriceDisplayComponent, BadgeComponent, ButtonComponent, IconComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="p-4 md:p-6 space-y-6 animate-fade-in">
      <!-- Header -->
      <div class="flex items-start justify-between gap-4 flex-wrap">
        <div>
          <div class="flex items-center gap-3 mb-2">
            <h1 class="text-headline font-bold text-fg">{{ symbol() }}</h1>
            <app-badge variant="info" size="sm">HOSE</app-badge>
            <app-badge variant="neutral" size="sm">{{ stockResult()?.name || symbol() }}</app-badge>
          </div>
          @if (stockResult()) {
            <app-price-display [data]="{
              symbol: stockResult()!.symbol,
              price: stockResult()!.price,
              refPrice: stockResult()!.refPrice,
              ceilPrice: stockResult()!.ceilPrice,
              floorPrice: stockResult()!.floorPrice,
              change: stockResult()!.change,
              changePct: stockResult()!.changePct,
              volume: stockResult()!.volume
            }" layout="full" [showVolume]="true" />
          }
        </div>
        <div class="flex gap-2">
          <app-btn variant="primary" size="md" label="Đặt lệnh MUA" />
          <app-btn variant="danger"  size="md" label="Đặt lệnh BÁN" />
          <app-btn variant="outline" size="md">
            <app-icon name="bell" size="md" />
          </app-btn>
        </div>
      </div>

      <!-- Layout: Chart + Sidebar -->
      <div class="grid grid-cols-1 xl:grid-cols-3 gap-6">
        <!-- Chart (2/3) -->
        <div class="xl:col-span-2 h-[480px]">
          @if (stockResult()) {
            <app-candle-chart
              [symbol]="symbol()"
              [candles]="chartCandles()"
              [loading]="loadingChart()"
              [showIndicators]="true"
              [lastPrice]="stockResult()!.price"
              [lastChangePct]="stockResult()!.changePct"
              [rsi]="28.5"
              [macd]="0.42"
              [ma20]="stockResult()!.price * 0.98"
              [ma50]="stockResult()!.price * 0.96"
            />
          }
        </div>

        <!-- Sidebar (1/3) -->
        <div class="space-y-4">
          <!-- Order book stub -->
          <app-card title="Sổ lệnh" variant="elevated">
            <div class="space-y-1 text-xs font-numeric">
              <div class="grid grid-cols-3 text-fg-muted mb-2 font-medium">
                <span>Giá mua</span><span class="text-center">KL</span><span class="text-right">Giá bán</span>
              </div>
              @for (level of orderBook; track level.bidPrice) {
                <div class="grid grid-cols-3">
                  <span class="text-up">{{ level.bidPrice | number:'1.1-2' }}</span>
                  <span class="text-center text-fg-muted">{{ level.bidVol | number }}</span>
                  <span class="text-right text-down">{{ level.askPrice | number:'1.1-2' }}</span>
                </div>
              }
              <div class="border-t border-border my-2"></div>
              <div class="flex justify-between text-fg-muted">
                <span>Tổng mua: <span class="text-up font-medium">{{ totalBid | number }}</span></span>
                <span>Tổng bán: <span class="text-down font-medium">{{ totalAsk | number }}</span></span>
              </div>
            </div>
          </app-card>

          <!-- Key stats -->
          <app-card title="Thông tin giao dịch" variant="default">
            <div class="space-y-2 text-small">
              @for (stat of keyStats(); track stat.label) {
                <div class="flex items-center justify-between py-1 border-b border-border/50 last:border-0">
                  <span class="text-fg-muted">{{ stat.label }}</span>
                  <span class="font-numeric font-medium" [class]="stat.color || 'text-fg'">{{ stat.value }}</span>
                </div>
              }
            </div>
          </app-card>
        </div>
      </div>
    </div>
  `,
})
export class StockDetailComponent implements OnInit {
  public chartService = inject(ChartService);
  public marketData = inject(MarketDataService);

  readonly symbol = input('VNM');

  readonly loadingChart = signal(true);
  readonly chartCandles = signal<any[]>([]);

  readonly stockResult = computed(() => {
    return this.marketData.stocks().find(s => s.symbol === this.symbol());
  });

  readonly orderBook = [
    { bidPrice: 65.8, bidVol: 48_200, askPrice: 65.9 },
    { bidPrice: 65.7, bidVol: 72_100, askPrice: 66.0 },
    { bidPrice: 65.6, bidVol: 35_400, askPrice: 66.1 },
  ];
  readonly totalBid = 155_700;
  readonly totalAsk = 198_400;

  readonly keyStats = computed(() => {
    const s = this.stockResult();
    if (!s) return [];
    return [
      { label: 'Mở cửa', value: s.refPrice.toFixed(2), color: '' },
      { label: 'Cao nhất', value: s.ceilPrice.toFixed(2), color: 'text-up' },
      { label: 'Thấp nhất', value: s.floorPrice.toFixed(2), color: 'text-down' },
      { label: 'P/E', value: '18.5x', color: '' },
      { label: 'EPS', value: '3,562 đ', color: '' },
      { label: 'KLCP', value: (s.volume / 1000000).toFixed(2) + ' triệu CP', color: '' },
      { label: 'Vốn hóa', value: '137,463 tỷ đ', color: '' },
      { label: 'Room NN', value: '7.28%', color: 'text-reference' },
    ];
  });

  ngOnInit(): void {
    if (this.marketData.stocks().length === 0) {
      this.marketData.getSymbols().subscribe();
    }

    this.chartService.getCandles(this.symbol()).subscribe(data => {
      const formatted = data.map(c => ({
        time: new Date(c.time).getTime() / 1000,
        open: c.open,
        high: c.high,
        low: c.low,
        close: c.close,
        volume: c.volume
      }));
      formatted.sort((a, b) => a.time - b.time);
      this.chartCandles.set(formatted);
      this.loadingChart.set(false);
    });
  }
}
