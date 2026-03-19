import { Component, input, signal, computed, ChangeDetectionStrategy, inject, OnInit, effect } from '@angular/core';
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

@Component({
  selector: 'app-stock-detail',
  standalone: true,
  imports: [CommonModule, FormsModule, CandleChartComponent, CardComponent, PriceDisplayComponent, BadgeComponent, ButtonComponent, IconComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  styles: [`
    .art-row { padding: .6rem 0; border-bottom: 1px solid var(--color-border, #2d3250); cursor: pointer; }
    .art-row:last-child { border-bottom: none; }
    .art-row:hover .art-title { text-decoration: underline; }
    .art-title { font-size: .8rem; font-weight: 500; color: var(--color-fg, #e5e7eb); line-height: 1.4; display: -webkit-box; -webkit-line-clamp: 2; -webkit-box-orient: vertical; overflow: hidden; }
    .sent-dot { width: 8px; height: 8px; border-radius: 50%; flex-shrink: 0; }
    .positive { background: #22c55e; }
    .negative { background: #ef4444; }
    .neutral  { background: #6b7280; }
    .comment-item { padding: .75rem 0; border-bottom: 1px solid var(--color-border,#2d3250); }
    .comment-item:last-child { border-bottom: none; }
  `],
  template: `
    <div class="p-4 md:p-6 space-y-6 animate-fade-in">
      <!-- Header -->
      <div class="flex items-start justify-between gap-4 flex-wrap">
        <div>
          <div class="flex items-center gap-3 mb-2">
            <h1 class="text-headline font-bold text-fg">{{ symbol() }}</h1>
            <app-badge variant="info" size="sm">HOSE</app-badge>
            <app-badge variant="neutral" size="sm">{{ stockResult()?.name || symbol() }}</app-badge>

            <!-- Sentiment badge -->
            @if (sentimentSummary()) {
              <span [class]="'px-2 py-0.5 rounded-full text-xs font-semibold ' + signalClass(sentimentSummary()!.overallSignal)">
                {{ signalLabel(sentimentSummary()!.overallSignal) }}
              </span>
            }
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
          <!-- Order book -->
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

          <!-- News panel -->
          <app-card title="📰 Tin tức & Sentiment" variant="default" [hasHeaderAction]="true">
            <ng-container slot="header-action">
              @if (sentimentSummary()) {
                <div class="flex items-center gap-1.5 text-xs">
                  <span class="text-up">{{ sentimentSummary()!.bullishPct }}%📈</span>
                  <span class="text-fg-muted">·</span>
                  <span class="text-down">{{ sentimentSummary()!.bearishPct }}%📉</span>
                </div>
              }
            </ng-container>
            <div class="mt-1">
              @if (loadingNews()) {
                <p class="text-xs text-fg-muted py-4 text-center">Đang tải tin tức…</p>
              } @else if (news().length === 0) {
                <p class="text-xs text-fg-muted py-4 text-center">Chưa có tin tức cho {{ symbol() }}.</p>
              } @else {
                @for (art of news(); track art.articleId) {
                  <a [href]="art.url" target="_blank" rel="noopener" class="art-row flex items-start gap-2 no-underline">
                    <span [class]="'sent-dot mt-1.5 ' + (art.sentiment ?? 'neutral')"></span>
                    <span class="art-title">{{ art.title }}</span>
                  </a>
                }
              }
            </div>
          </app-card>
        </div>
      </div>

      <!-- Comments Section -->
      <section>
        <h2 class="text-base font-bold text-fg mb-4">💬 Thảo luận — {{ symbol() }}</h2>

        <!-- Post comment -->
        @if (authSvc.user()) {
          <div class="flex gap-3 mb-4">
            <div class="w-9 h-9 rounded-full bg-up/20 flex items-center justify-center shrink-0">
              <app-icon name="user" size="sm" class="text-up" />
            </div>
            <div class="flex-1 flex gap-2">
              <textarea
                class="flex-1 min-h-[60px] px-3 py-2 text-small bg-surface-2 border border-border rounded-xl text-fg resize-none focus:outline-none focus:border-up"
                placeholder="Nhận định của bạn về {{ symbol() }}…"
                [(ngModel)]="commentDraft"
                (keydown.ctrl.enter)="submitComment()"
              ></textarea>
              <app-btn variant="primary" size="sm" label="Gửi" [loading]="postingComment()" (clicked)="submitComment()" />
            </div>
          </div>
        } @else {
          <p class="text-xs text-fg-muted mb-4">Đăng nhập để tham gia thảo luận.</p>
        }

        <!-- Comment list -->
        @if (loadingComments()) {
          <p class="text-xs text-fg-muted text-center py-4">Đang tải bình luận…</p>
        } @else if (comments().length === 0) {
          <p class="text-xs text-fg-muted text-center py-6">Chưa có bình luận nào. Hãy là người đầu tiên! 🚀</p>
        } @else {
          @for (c of comments(); track c.commentId) {
            <div class="comment-item flex items-start gap-3">
              <div class="w-8 h-8 rounded-full bg-surface-2 border border-border flex items-center justify-center text-sm font-bold text-fg-muted shrink-0">
                {{ c.username.charAt(0).toUpperCase() }}
              </div>
              <div class="flex-1 min-w-0">
                <div class="flex items-center gap-2 mb-0.5">
                  <span class="text-small font-semibold text-fg">{{ c.username }}</span>
                  <span class="text-xs text-fg-muted">{{ c.createdAt | date:'dd/MM HH:mm' }}</span>
                </div>
                <p class="text-small text-fg">{{ c.content }}</p>
              </div>
              @if (c.userId === authSvc.user()?.userId) {
                <button class="text-fg-muted hover:text-down transition-colors p-1" (click)="deleteComment(c.commentId)" title="Xóa">
                  <app-icon name="x" size="sm" />
                </button>
              }
            </div>
          }
        }
      </section>
    </div>
  `,
})
export class StockDetailComponent implements OnInit {
  public chartService = inject(ChartService);
  public marketData = inject(MarketDataService);
  private newsSvc = inject(NewsService);
  public authSvc = inject(AuthService);

  readonly symbol = input('');

  readonly loadingChart = signal(true);
  readonly loadingNews = signal(true);
  readonly loadingComments = signal(false);
  readonly postingComment = signal(false);
  readonly chartCandles = signal<any[]>([]);
  readonly news = signal<NewsArticle[]>([]);
  readonly sentimentSummary = signal<SentimentSummary | null>(null);
  readonly comments = signal<NewsComment[]>([]);

  commentDraft = '';

  readonly stockResult = computed(() =>
    this.marketData.stocks().find(s => s.symbol === this.symbol())
  );

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
      { label: 'KLCP', value: (s.volume / 1_000_000).toFixed(2) + ' triệu CP', color: '' },
      { label: 'Room NN', value: '7.28%', color: 'text-reference' },
    ];
  });

  constructor() {
    // React when symbol route param changes
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
          .map(c => ({ time: new Date(c.time).getTime() / 1000, open: c.open, high: c.high, low: c.low, close: c.close, volume: c.volume }))
          .sort((a, b) => a.time - b.time);
        this.chartCandles.set(formatted);
        this.loadingChart.set(false);
      });
    }
  }

  private loadNews(symbol: string): void {
    this.loadingNews.set(true);
    this.newsSvc.getNews(symbol, 6).subscribe({
      next: (data) => { this.news.set(data); this.loadingNews.set(false); },
      error: () => this.loadingNews.set(false),
    });
    this.newsSvc.getSentimentSummary(symbol).subscribe({
      next: (s) => this.sentimentSummary.set(s),
      error: () => { },
    });
  }

  private loadComments(symbol: string): void {
    this.newsSvc.getComments(symbol).subscribe({
      next: (data) => this.comments.set(data),
      error: () => { },
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
      },
      error: () => this.postingComment.set(false),
    });
  }

  deleteComment(id: number): void {
    this.newsSvc.deleteComment(id).subscribe({
      next: () => this.comments.update(list => list.filter(c => c.commentId !== id)),
      error: () => { },
    });
  }

  signalLabel(s: string): string { return { BULLISH: '🟢 Tích cực', BEARISH: '🔴 Tiêu cực', NEUTRAL: '⚪ Trung lập' }[s] ?? s; }
  signalClass(s: string): string {
    return { BULLISH: 'bg-up/10 text-up', BEARISH: 'bg-down/10 text-down', NEUTRAL: 'bg-surface-2 text-fg-muted' }[s] ?? '';
  }
}
