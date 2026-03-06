import {
  Component, input, output, signal, computed,
  ChangeDetectionStrategy, AfterViewInit, ElementRef, ViewChild, OnDestroy,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { ButtonComponent } from '../../atoms/button/button.component';
import { BadgeComponent } from '../../atoms/badge/badge.component';
import { SkeletonComponent } from '../../atoms/skeleton/skeleton.component';
import { IconComponent } from '../../atoms/icon/icon.component';
import { TabNavComponent, type TabItem } from '../../molecules/tab-nav/tab-nav.component';

export type ChartTimeframe = '1D' | '1W' | '1M' | '3M' | '1Y' | 'ALL';
export type ChartType = 'candle' | 'line' | 'area';

export interface CandlePoint {
  time: number;
  open: number;
  high: number;
  low: number;
  close: number;
  volume: number;
}


@Component({
  selector: 'app-candle-chart',
  standalone: true,
  imports: [CommonModule, ButtonComponent, BadgeComponent, SkeletonComponent, IconComponent, TabNavComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="flex flex-col h-full rounded-xl border border-border bg-surface overflow-hidden">

      <!-- Chart toolbar -->
      <div class="flex items-center justify-between px-4 py-3 border-b border-border shrink-0">
        <div class="flex items-center gap-3">
          <span class="text-title font-bold text-fg">{{ symbol() }}</span>
          <!-- Current price badge -->
          @if (lastPrice() !== null) {
            <span [class]="'text-headline font-bold font-numeric ' + lastPriceClass()">
              {{ lastPrice() | number:'1.2-2' }}
            </span>
            <app-badge [variant]="lastChangePct()! >= 0 ? 'up' : 'down'" size="sm">
              {{ lastChangePct()! >= 0 ? '+' : '' }}{{ lastChangePct() | number:'1.2-2' }}%
            </app-badge>
          }
        </div>

        <div class="flex items-center gap-2">
          <!-- Chart type toggle -->
          <div class="flex items-center gap-1 p-1 bg-surface-2 rounded-md">
            @for (type of chartTypes; track type.id) {
              <button
                [class]="'px-2 py-1 rounded text-xs font-medium transition-colors ' +
                  (chartType() === type.id ? 'bg-surface text-fg shadow-sm' : 'text-fg-muted hover:text-fg')"
                (click)="chartType.set(type.id)"
                [attr.aria-label]="type.label"
              >{{ type.label }}</button>
            }
          </div>

          <!-- Fullscreen -->
          <button
            class="p-1.5 rounded-md text-fg-muted hover:text-fg hover:bg-surface-2 transition-colors"
            aria-label="Toàn màn hình"
            (click)="fullscreen.set(!fullscreen())"
          >
            <app-icon name="external-link" size="sm" />
          </button>
        </div>
      </div>

      <!-- Timeframe tabs -->
      <div class="px-4 pt-2 shrink-0">
        <app-tab-nav
          [tabs]="timeframeTabs"
          [activeId]="timeframe()"
          variant="pills"
          (activeIdChange)="onTimeframeChange($event)"
        />
      </div>

      <!-- Chart area -->
      <div class="relative flex-1 min-h-0">
        @if (loading()) {
          <!-- Skeleton chart -->
          <div class="absolute inset-0 flex flex-col justify-end p-4 gap-2">
            <!-- Fake candlestick bars -->
            <div class="flex items-end gap-1 h-48">
              @for (bar of skeletonBars; track bar) {
                <div class="skeleton-base flex-1 rounded-sm" [style.height]="bar + '%'"></div>
              }
            </div>
            <!-- Fake volume bars -->
            <div class="flex items-end gap-1 h-12">
              @for (bar of skeletonBars; track bar) {
                <div class="skeleton-base flex-1 rounded-sm" [style.height]="(bar * 0.4) + '%'"></div>
              }
            </div>
          </div>
        } @else {
          <!-- Lightweight-charts mount point -->
          <div
            #chartContainer
            class="absolute inset-0"
            role="img"
            [attr.aria-label]="symbol() + ' biểu đồ giá'"
          ></div>

          <!-- RSI Signal overlay -->
          @if (showRsiSignal()) {
            <div class="absolute top-3 right-3 flex items-center gap-2 px-3 py-1.5 rounded-full bg-up/20 border border-up/40 animate-pulse-soft">
              <span class="w-2 h-2 rounded-full bg-up"></span>
              <span class="text-xs font-semibold text-up">RSI &lt; 30 – Oversold!</span>
            </div>
          }

          <!-- No data state -->
          @if (candles().length === 0) {
            <div class="absolute inset-0 flex items-center justify-center">
              <div class="text-center space-y-2">
                <app-icon name="bar-chart-2" size="xl" class="text-fg-muted mx-auto" />
                <p class="text-small text-fg-muted">Chưa có dữ liệu biểu đồ</p>
              </div>
            </div>
          }
        }
      </div>

      <!-- Indicator strip (RSI / MACD labels) -->
      @if (showIndicators() && !loading()) {
        <div class="border-t border-border px-4 py-2 flex items-center gap-6 text-xs text-fg-muted shrink-0">
          <span>RSI(14): <span [class]="rsiClass()">{{ rsi() | number:'1.1-1' }}</span></span>
          <span>MACD: <span [class]="macd() >= 0 ? 'text-up font-medium' : 'text-down font-medium'">
            {{ macd() >= 0 ? '+' : '' }}{{ macd() | number:'1.2-2' }}
          </span></span>
          <span>MA20: <span class="text-reference font-medium font-numeric">{{ ma20() | number:'1.2-2' }}</span></span>
          <span>MA50: <span class="text-fg font-medium font-numeric">{{ ma50() | number:'1.2-2' }}</span></span>
        </div>
      }
    </div>
  `,
})
export class CandleChartComponent implements AfterViewInit, OnDestroy {
  @ViewChild('chartContainer') chartContainer!: ElementRef<HTMLDivElement>;

  readonly symbol = input('VNM');
  readonly candles = input<CandlePoint[]>([]);
  readonly loading = input(false);
  readonly showIndicators = input(true);
  readonly rsi = input(45.5);
  readonly macd = input(0.35);
  readonly ma20 = input(65.4);
  readonly ma50 = input(63.2);
  readonly lastPrice = input<number | null>(null);
  readonly lastChangePct = input<number | null>(null);

  readonly timeframeChanged = output<ChartTimeframe>();

  readonly timeframe = signal<ChartTimeframe>('1D');
  readonly chartType = signal<ChartType>('candle');
  readonly fullscreen = signal(false);

  private chartInstance: unknown = null;

  readonly chartTypes = [
    { id: 'candle' as ChartType, label: '🕯' },
    { id: 'line' as ChartType, label: '📈' },
    { id: 'area' as ChartType, label: '◿' },
  ];

  readonly timeframeTabs: TabItem[] = [
    { id: '1D', label: '1N' },
    { id: '1W', label: '1T' },
    { id: '1M', label: '1Th' },
    { id: '3M', label: '3Th' },
    { id: '1Y', label: '1Năm' },
    { id: 'ALL', label: 'Tất cả' },
  ];

  readonly skeletonBars = Array(40).fill(0).map(() => 20 + Math.floor(Math.random() * 70));

  readonly showRsiSignal = computed(() => this.rsi() < 30);

  readonly lastPriceClass = computed(() => {
    const pct = this.lastChangePct();
    if (pct === null) return 'text-fg';
    if (pct > 0) return 'text-up';
    if (pct < 0) return 'text-down';
    return 'text-reference';
  });

  readonly rsiClass = computed(() => {
    const rsi = this.rsi();
    if (rsi >= 70) return 'text-down font-semibold';
    if (rsi <= 30) return 'text-up font-semibold';
    return 'text-fg font-medium';
  });

  async ngAfterViewInit(): Promise<void> {
    if (!this.loading() && this.candles().length > 0) {
      await this.initChart();
    }
  }

  private async initChart(): Promise<void> {
    try {
      const { createChart, ColorType } = await import('lightweight-charts');
      const container = this.chartContainer?.nativeElement;
      if (!container) return;

      const chart = createChart(container, {
        layout: {
          background: { type: ColorType.Solid, color: 'var(--color-surface)' } as never,
          textColor: 'var(--color-fg-muted)',
        },
        grid: {
          vertLines: { color: 'var(--color-border)' },
          horzLines: { color: 'var(--color-border)' },
        },
        crosshair: { mode: 1 },
        rightPriceScale: { borderColor: 'var(--color-border)' },
        timeScale: {
          borderColor: 'var(--color-border)',
          timeVisible: true,
          secondsVisible: false,
        },
        width: container.clientWidth,
        height: container.clientHeight,
      });

      const chartAny = chart as any;
      if (this.chartType() === 'candle' && chartAny.addCandlestickSeries) {
        const series = chartAny.addCandlestickSeries({
          upColor: '#10B981',
          downColor: '#DC2626',
          borderUpColor: '#10B981',
          borderDownColor: '#DC2626',
          wickUpColor: '#10B981',
          wickDownColor: '#DC2626',
        });
        series.setData(this.candles().map((c: CandlePoint) => ({
          time: c.time as never,
          open: c.open,
          high: c.high,
          low: c.low,
          close: c.close,
        })));
      }

      this.chartInstance = chart;

      const ro = new ResizeObserver(() => {
        if (container) chart.resize(container.clientWidth, container.clientHeight);
      });
      ro.observe(container);
    } catch {
    }
  }

  onTimeframeChange(id: string): void {
    this.timeframe.set(id as ChartTimeframe);
    this.timeframeChanged.emit(id as ChartTimeframe);
  }

  ngOnDestroy(): void {
    if (this.chartInstance) {
      (this.chartInstance as { remove: () => void }).remove();
    }
  }
}
