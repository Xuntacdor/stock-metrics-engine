import {
  Component, input, signal, computed,
  ChangeDetectionStrategy, inject, OnInit, effect
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { CandleChartComponent } from '../../shared/organisms/candle-chart/candle-chart.component';
import { CardComponent } from '../../shared/molecules/card/card.component';
import { PriceDisplayComponent } from '../../shared/molecules/price-display/price-display.component';
import { BadgeComponent } from '../../shared/atoms/badge/badge.component';
import { ButtonComponent } from '../../shared/atoms/button/button.component';
import { IconComponent } from '../../shared/atoms/icon/icon.component';
import { ChartService } from '../../core/services/chart.service';
import { MarketDataService } from '../../core/services/market-data.service';
import { NewsService, NewsArticle, SentimentSummary, NewsComment } from '../../core/services/news.service';
import { AuthService } from '../../core/services/auth.service';
import { ToastService } from '../../core/services/toast.service';
import { AlertService } from '../../core/services/alert.service';
import { OrderService } from '../../core/services/order.service';

export interface OrderBookLevel {
  bidPrice: number;
  bidVol: number;
  askPrice: number;
}

@Component({
  selector: 'app-stock-detail',
  standalone: true,
  imports: [
    CommonModule, FormsModule,
    CandleChartComponent, CardComponent, PriceDisplayComponent,
    BadgeComponent, ButtonComponent, IconComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './stock-detail.component.html',
  styleUrls: ['./stock-detail.component.scss'],
})
export class StockDetailComponent implements OnInit {
  public chartService = inject(ChartService);
  public marketData  = inject(MarketDataService);
  private newsSvc    = inject(NewsService);
  public authSvc     = inject(AuthService);
  private toast      = inject(ToastService);
  private alertSvc   = inject(AlertService);
  private orderSvc   = inject(OrderService);

  readonly symbol = input('');

  // ── Loading flags ──────────────────────────────────────────────────────────
  readonly loadingChart    = signal(true);
  readonly loadingNews     = signal(true);
  readonly loadingComments = signal(false);
  readonly postingComment  = signal(false);
  readonly placingOrder    = signal(false);

  // ── Data signals ───────────────────────────────────────────────────────────
  readonly chartCandles    = signal<any[]>([]);
  readonly news            = signal<NewsArticle[]>([]);
  readonly sentimentSummary = signal<SentimentSummary | null>(null);
  readonly comments        = signal<NewsComment[]>([]);
  readonly orderBook       = signal<OrderBookLevel[]>([]);

  // ── Order panel ────────────────────────────────────────────────────────────
  readonly orderPanelOpen = signal(false);
  readonly orderSide      = signal<'BUY' | 'SELL'>('BUY');
  orderType  = 'LIMIT';
  orderPrice = 0;
  orderQty   = 100;

  commentDraft = '';

  // ── Derived ────────────────────────────────────────────────────────────────
  readonly stockResult = computed(() =>
    this.marketData.stocks().find(s => s.symbol === this.symbol())
  );

  readonly totalBid = computed(() =>
    this.orderBook().reduce((sum, l) => sum + l.bidVol, 0)
  );

  readonly totalAsk = computed(() =>
    this.orderBook().reduce((sum, l) => sum + l.bidVol, 0)
  );

  readonly keyStats = computed(() => {
    const s = this.stockResult();
    if (!s) return [];
    return [
      { label: 'Mở cửa',    value: s.refPrice.toFixed(2),                         color: '' },
      { label: 'Cao nhất',  value: s.ceilPrice.toFixed(2),                        color: 'text-up' },
      { label: 'Thấp nhất', value: s.floorPrice.toFixed(2),                       color: 'text-down' },
      { label: 'KLCP',      value: (s.volume / 1_000_000).toFixed(2) + ' triệu CP', color: '' },
    ];
  });

  constructor() {
    effect(() => {
      const sym = this.symbol();
      if (sym) {
        this.loadNews(sym);
        this.loadComments(sym);
      }
    });
  }

  ngOnInit(): void {
    if (this.marketData.stocks().length === 0) {
      this.marketData.getSymbols().subscribe();
    }
    const sym = this.symbol();
    if (sym) {
      this.chartService.getCandles(sym).subscribe(data => {
        const formatted = data
          .map(c => ({
            time: new Date(c.time).getTime() / 1000,
            open: c.open, high: c.high, low: c.low, close: c.close, volume: c.volume,
          }))
          .sort((a, b) => a.time - b.time);
        this.chartCandles.set(formatted);
        this.loadingChart.set(false);
      });
    }
  }

  // ── Order panel ────────────────────────────────────────────────────────────
  openOrderPanel(side: 'BUY' | 'SELL'): void {
    this.orderSide.set(side);
    this.orderPrice = this.stockResult()?.price ?? 0;
    this.orderQty   = 100;
    this.orderType  = 'LIMIT';
    this.orderPanelOpen.set(true);
  }

  closeOrderPanel(): void {
    this.orderPanelOpen.set(false);
  }

  submitOrder(): void {
    if (this.orderQty <= 0) {
      this.toast.warning('Vui lòng nhập khối lượng hợp lệ.');
      return;
    }
    if (this.orderType === 'LIMIT' && this.orderPrice <= 0) {
      this.toast.warning('Vui lòng nhập giá hợp lệ.');
      return;
    }

    this.placingOrder.set(true);
    this.orderSvc.placeOrder({
      symbol:    this.symbol(),
      side:      this.orderSide(),
      orderType: this.orderType as 'MARKET' | 'LIMIT',
      quantity:  this.orderQty,
      price:     this.orderType === 'MARKET' ? (this.stockResult()?.price ?? 0) : this.orderPrice,
    }).subscribe({
      next: (res) => {
        this.placingOrder.set(false);
        this.closeOrderPanel();
        const side = res.side === 'BUY' ? 'MUA' : 'BÁN';
        this.toast.success(`Đặt lệnh ${side} ${res.requestQty} CP ${res.symbol} thành công!`);
      },
      error: () => {
        this.placingOrder.set(false);
      },
    });
  }

  // ── Alert ──────────────────────────────────────────────────────────────────
  createAlert(): void {
    const price = this.stockResult()?.price;
    if (!price) return;
    this.alertSvc.createAlert({
      symbol:         this.symbol(),
      alertType:      'price',
      condition:      'gt',
      thresholdValue: Math.round(price * 1.05 * 10) / 10,
      notifyOnce:     false,
    }).subscribe({
      next: () => this.toast.success(`Đã tạo cảnh báo giá cho ${this.symbol()}.`),
    });
  }

  // ── News & comments ────────────────────────────────────────────────────────
  private loadNews(symbol: string): void {
    this.loadingNews.set(true);
    this.newsSvc.getNews(symbol, 6).subscribe({
      next: (data) => { this.news.set(data); this.loadingNews.set(false); },
      error: () => this.loadingNews.set(false),
    });
    this.newsSvc.getSentimentSummary(symbol).subscribe({
      next: (s) => this.sentimentSummary.set(s),
      error: () => {},
    });
  }

  private loadComments(symbol: string): void {
    this.newsSvc.getComments(symbol).subscribe({
      next: (data) => this.comments.set(data),
      error: () => {},
    });
  }

  submitComment(): void {
    const sym = this.symbol();
    if (!this.commentDraft.trim() || !sym) return;
    this.postingComment.set(true);
    this.newsSvc.postComment(sym, this.commentDraft.trim()).subscribe({
      next: (c) => {
        this.comments.update(list => [c, ...list]);
        this.commentDraft = '';
        this.postingComment.set(false);
        this.toast.success('Bình luận đã được đăng!');
      },
      error: (err) => {
        this.postingComment.set(false);
        this.toast.error('Không thể đăng bình luận: ' + (err?.error?.message || 'Vui lòng thử lại.'));
      },
    });
  }

  deleteComment(id: number): void {
    this.newsSvc.deleteComment(id).subscribe({
      next: () => {
        this.comments.update(list => list.filter(c => c.commentId !== id));
        this.toast.info('Đã xóa bình luận.');
      },
      error: () => this.toast.error('Không thể xóa bình luận. Vui lòng thử lại.'),
    });
  }

  // ── Helpers ────────────────────────────────────────────────────────────────
  signalLabel(s: string): string {
    return ({ BULLISH: '🟢 Tích cực', BEARISH: '🔴 Tiêu cực', NEUTRAL: '⚪ Trung lập' } as Record<string, string>)[s] ?? s;
  }
  signalClass(s: string): string {
    return ({ BULLISH: 'bg-up/10 text-up', BEARISH: 'bg-down/10 text-down', NEUTRAL: 'bg-surface-2 text-fg-muted' } as Record<string, string>)[s] ?? '';
  }
}
