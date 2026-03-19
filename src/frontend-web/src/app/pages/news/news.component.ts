import { Component, signal, computed, ChangeDetectionStrategy, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { NewsService, NewsArticle, SentimentSummary } from '../../core/services/news.service';
import { MarketDataService } from '../../core/services/market-data.service';
import { CardComponent } from '../../shared/molecules/card/card.component';
import { BadgeComponent } from '../../shared/atoms/badge/badge.component';
import { InputComponent } from '../../shared/atoms/input/input.component';
import { SkeletonComponent } from '../../shared/atoms/skeleton/skeleton.component';
import { IconComponent } from '../../shared/atoms/icon/icon.component';

type SentFilter = 'all' | 'positive' | 'negative' | 'neutral';

@Component({
    selector: 'app-news',
    standalone: true,
    imports: [CommonModule, FormsModule, CardComponent, BadgeComponent, InputComponent, SkeletonComponent, IconComponent],
    changeDetection: ChangeDetectionStrategy.OnPush,
    styles: [`
    .sent-bar { display: flex; height: 8px; border-radius: 4px; overflow: hidden; gap: 2px; }
    .sent-bar span { border-radius: 4px; transition: width .5s ease; }
    .art-card { border: 1px solid var(--color-border, #2d3250); border-radius: .75rem; padding: 1rem 1.25rem; cursor: pointer; transition: border-color .2s, background .2s; }
    .art-card:hover { border-color: var(--color-border-hover, #4a5178); background: var(--color-surface-2, #252840); }
    .art-card .source { font-size: .7rem; text-transform: uppercase; letter-spacing: .05em; }
    .art-title { font-size: .9rem; font-weight: 600; line-height: 1.45; margin: .35rem 0 .5rem; color: var(--color-fg, #e5e7eb); }
    .art-summary { font-size: .78rem; color: var(--color-fg-muted, #9ca3af); line-height: 1.5; display: -webkit-box; -webkit-line-clamp: 2; -webkit-box-orient: vertical; overflow: hidden; }
    .sent-chip { display: inline-flex; align-items: center; gap: .25rem; padding: .1rem .5rem; border-radius: 999px; font-size: .7rem; font-weight: 600; }
    .chip-positive { background: rgba(34,197,94,.12); color: #22c55e; }
    .chip-negative { background: rgba(239,68,68,.12); color: #ef4444; }
    .chip-neutral  { background: rgba(156,163,175,.12); color: #9ca3af; }
    .filter-btn { padding: .35rem .9rem; border-radius: 999px; font-size: .75rem; font-weight: 500; border: 1px solid var(--color-border); cursor: pointer; transition: all .2s; }
    .filter-btn.active { background: var(--color-up, #22c55e); border-color: var(--color-up); color: #000; }
  `],
    template: `
    <div class="p-4 md:p-6 space-y-6 animate-fade-in">
      <div>
        <h1 class="text-headline font-bold text-fg">📰 Tin tức thị trường</h1>
        <p class="text-small text-fg-muted mt-0.5">Tin tức tổng hợp từ CafeF, VnEconomy, Tin nhanh Chứng khoán</p>
      </div>

      <div class="grid grid-cols-1 xl:grid-cols-4 gap-6">

        <!-- Left: filters -->
        <div class="xl:col-span-1 space-y-4">
          <!-- Symbol filter -->
          <app-card title="Lọc tin tức" variant="elevated">
            <div class="space-y-3 mt-2">
              <div>
                <label class="text-xs text-fg-muted mb-1 block">Mã cổ phiếu</label>
                <input class="w-full h-9 px-3 text-small bg-surface-2 border border-border rounded-lg text-fg focus:outline-none focus:border-up"
                       placeholder="FPT, VNM…" [(ngModel)]="symbolFilter" (ngModelChange)="onSymbolChange($event)" />
              </div>
              <div>
                <p class="text-xs text-fg-muted mb-2">Cảm xúc</p>
                <div class="flex flex-wrap gap-1.5">
                  @for (f of sentFilters; track f.id) {
                    <button [class]="'filter-btn ' + (sentFilter() === f.id ? 'active' : 'text-fg-muted bg-surface-2')"
                            (click)="sentFilter.set(f.id)">{{ f.label }}</button>
                  }
                </div>
              </div>
            </div>
          </app-card>

          <!-- Sentiment summary -->
          @if (summary()) {
            <app-card title="Sentiment {{ summary()!.symbol }}" variant="default">
              <div class="mt-3 space-y-3">
                <div class="sent-bar">
                  <span [style.width.%]="summary()!.bullishPct" style="background:#22c55e"></span>
                  <span [style.width.%]="summary()!.neutralPct" style="background:#6b7280"></span>
                  <span [style.width.%]="summary()!.bearishPct" style="background:#ef4444"></span>
                </div>
                <div class="flex justify-between text-xs">
                  <span class="text-up">📈 {{ summary()!.bullishPct }}%</span>
                  <span class="text-fg-muted">⚖ {{ summary()!.neutralPct }}%</span>
                  <span class="text-down">📉 {{ summary()!.bearishPct }}%</span>
                </div>
                <div class="text-center">
                  <span [class]="'text-sm font-bold ' + signalClass(summary()!.overallSignal)">
                    {{ signalLabel(summary()!.overallSignal) }}
                  </span>
                  <p class="text-xs text-fg-muted mt-0.5">7 ngày gần nhất · {{ summary()!.total }} tin</p>
                </div>
              </div>
            </app-card>
          }
        </div>

        <!-- Right: article list -->
        <div class="xl:col-span-3 space-y-3">
          @if (loading()) {
            @for (i of [1,2,3,4,5,6]; track i) { <app-skeleton height="100px" /> }
          } @else if (filtered().length === 0) {
            <div class="flex flex-col items-center py-16 text-center text-fg-muted">
              <span class="text-4xl mb-3">📭</span>
              <p class="font-medium text-fg">Không có tin tức nào</p>
              <p class="text-small mt-1">Crawler chưa chạy hoặc không tìm thấy tin cho mã này.</p>
            </div>
          } @else {
            @for (art of filtered(); track art.articleId) {
              <a [href]="art.url" target="_blank" rel="noopener" class="art-card block no-underline">
                <div class="flex items-start justify-between gap-3">
                  <div class="flex-1 min-w-0">
                    <div class="flex items-center gap-2">
                      <span class="source text-fg-muted">{{ art.source ?? 'tin tức' }}</span>
                      @if (art.symbol) {
                        <span class="px-1.5 py-0.5 rounded text-xs font-bold bg-up/10 text-up">{{ art.symbol }}</span>
                      }
                    </div>
                    <p class="art-title">{{ art.title }}</p>
                    @if (art.summary) {
                      <p class="art-summary">{{ art.summary }}</p>
                    }
                  </div>
                  <div class="shrink-0 text-right space-y-1.5">
                    @if (art.sentiment) {
                      <span [class]="'sent-chip chip-' + art.sentiment">
                        {{ sentimentEmoji(art.sentiment) }} {{ sentimentLabel(art.sentiment) }}
                      </span>
                    }
                    <p class="text-xs text-fg-muted">{{ art.publishedAt | date:'dd/MM HH:mm' }}</p>
                  </div>
                </div>
              </a>
            }
          }
        </div>
      </div>
    </div>
  `,
})
export class NewsComponent implements OnInit {
    private readonly newsSvc = inject(NewsService);

    readonly loading = signal(true);
    readonly articles = signal<NewsArticle[]>([]);
    readonly summary = signal<SentimentSummary | null>(null);
    readonly sentFilter = signal<SentFilter>('all');

    symbolFilter = '';

    readonly sentFilters = [
        { id: 'all' as SentFilter, label: 'Tất cả' },
        { id: 'positive' as SentFilter, label: '📈 Tích cực' },
        { id: 'negative' as SentFilter, label: '📉 Tiêu cực' },
        { id: 'neutral' as SentFilter, label: '⚖ Trung lập' },
    ];

    readonly filtered = computed(() => {
        const f = this.sentFilter();
        return f === 'all' ? this.articles() : this.articles().filter(a => a.sentiment === f);
    });

    ngOnInit(): void { this.load(); }

    load(symbol?: string): void {
        this.loading.set(true);
        this.newsSvc.getNews(symbol, 50).subscribe({
            next: (data) => { this.articles.set(data); this.loading.set(false); },
            error: () => this.loading.set(false),
        });
        if (symbol) {
            this.newsSvc.getSentimentSummary(symbol).subscribe({
                next: (s) => this.summary.set(s),
                error: () => this.summary.set(null),
            });
        } else {
            this.summary.set(null);
        }
    }

    onSymbolChange(val: string): void {
        clearTimeout((this as any)._timer);
        (this as any)._timer = setTimeout(() => this.load(val?.trim().toUpperCase() || undefined), 500);
    }

    sentimentLabel(s: string): string { return { positive: 'Tích cực', negative: 'Tiêu cực', neutral: 'Trung lập' }[s] ?? s; }
    sentimentEmoji(s: string): string { return { positive: '📈', negative: '📉', neutral: '⚖' }[s] ?? ''; }
    signalLabel(s: string): string { return { BULLISH: '🟢 Thị trường lạc quan', BEARISH: '🔴 Thị trường bi quan', NEUTRAL: '⚪ Trung lập' }[s] ?? s; }
    signalClass(s: string): string { return { BULLISH: 'text-up', BEARISH: 'text-down', NEUTRAL: 'text-fg-muted' }[s] ?? 'text-fg-muted'; }
}
