import {
  Component, signal, computed,
  ChangeDetectionStrategy, OnInit, inject
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { PriceTableComponent, type StockRow } from '../../shared/organisms/price-table/price-table.component';
import { PortfolioSummaryComponent, type Holding } from '../../shared/organisms/portfolio-summary/portfolio-summary.component';
import { StatBoxComponent, type StatBoxData } from '../../shared/molecules/stat-box/stat-box.component';
import { TabNavComponent, type TabItem } from '../../shared/molecules/tab-nav/tab-nav.component';
import { CardComponent } from '../../shared/molecules/card/card.component';
import { BadgeComponent } from '../../shared/atoms/badge/badge.component';
import { ButtonComponent } from '../../shared/atoms/button/button.component';
import { IconComponent } from '../../shared/atoms/icon/icon.component';
import { SkeletonComponent } from '../../shared/atoms/skeleton/skeleton.component';
import { MarketDataService } from '../../core/services/market-data.service';
import { PortfolioService } from '../../core/services/portfolio.service';
import { RiskService } from '../../core/services/risk.service';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [
    CommonModule, RouterLink,
    PriceTableComponent, PortfolioSummaryComponent,
    StatBoxComponent, TabNavComponent, CardComponent,
    BadgeComponent, ButtonComponent, IconComponent, SkeletonComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="p-4 md:p-6 space-y-6 animate-fade-in">

      <!-- ── Page header ── -->
      <div class="flex items-center justify-between">
        <div>
          <h1 class="text-headline font-bold text-fg">Tổng quan thị trường</h1>
          <p class="text-small text-fg-muted mt-0.5">{{ todayLabel() }} · Sàn HOSE</p>
        </div>
        <app-btn variant="primary" size="sm" label="Đặt lệnh nhanh">
          <app-icon slot="icon-left" name="trending-up" size="sm" />
        </app-btn>
      </div>

      <!-- ── Market indices ── -->
      <div class="grid grid-cols-2 lg:grid-cols-4 gap-4">
        <!-- Replace hardcoded cards with signals -->
        <!-- VN-Index -->
        <app-stat-box [data]="{ 
          title: 'VN-Index', value: marketData.market().vnIndex, change: 0, trend: 'up', icon: 'bar-chart-2', caption: '' 
        }" />
        <!-- HNX-Index -->
        <app-stat-box [data]="{ 
          title: 'HNX-Index', value: marketData.market().hnxIndex, change: 0, trend: 'up', icon: 'bar-chart-2', caption: '' 
        }" />
        <!-- UP Count -->
        <app-stat-box [data]="{ 
          title: 'Mã tăng', value: marketData.market().upCount, suffix: 'CP', trend: 'up', icon: 'arrow-up', caption: 'trong phiên' 
        }" />
        <!-- DOWN Count -->
        <app-stat-box [data]="{ 
          title: 'Mã giảm', value: marketData.market().downCount, suffix: 'CP', trend: 'down', icon: 'arrow-down', caption: 'trong phiên' 
        }" />
      </div>

      <!-- ── Main content grid ── -->
      <div class="grid grid-cols-1 xl:grid-cols-3 gap-6">

        <!-- Left col: Price Table (2/3) -->
        <div class="xl:col-span-2 space-y-4">

          <!-- Tab: Watchlist / HOSE / HNX -->
          <div class="flex items-center justify-between">
            <app-tab-nav
              [tabs]="marketTabs"
              [activeId]="activeMarketTab()"
              variant="underline"
              (activeIdChange)="activeMarketTab.set($event)"
            />
            <div class="flex items-center gap-2">
              <app-btn variant="ghost" size="sm" (clicked)="loadingTable.set(true); simulateLoad()">
                <app-icon name="refresh-cw" size="sm" />
              </app-btn>
            </div>
          </div>

          <app-price-table
            [stocks]="currentStocks()"
            [loading]="loadingTable()"
            [showActions]="true"
            [showBreadth]="true"
            [title]="activeMarketTab() === 'watchlist' ? 'Danh sách theo dõi' : activeMarketTab()"
            (rowClicked)="onStockClick($event)"
            (refreshed)="simulateLoad()"
          />
        </div>

        <!-- Right col: Portfolio snapshot + News (1/3) -->
        <div class="space-y-4">

          <!-- Portfolio quick view -->
          <app-card title="Tài khoản" variant="elevated" [hasHeaderAction]="true">
            <ng-container slot="header-action">
              <a routerLink="/portfolio" class="text-xs text-up hover:underline">Xem chi tiết</a>
            </ng-container>

            <div class="space-y-3">
              <div class="flex items-center justify-between">
                <span class="text-small text-fg-muted">Tổng tài sản</span>
                <span class="text-body font-bold font-numeric text-fg">₫{{ portfolio.totalPortfolioValue() | number:'1.0-0' }}</span>
              </div>
              <div class="flex items-center justify-between">
                <span class="text-small text-fg-muted">Lãi/Lỗ tạm tính</span>
                <span [class]="'text-body font-semibold font-numeric ' + (portfolio.totalUnrealizedPnL() >= 0 ? 'text-up' : 'text-down')">
                  {{ portfolio.totalUnrealizedPnL() >= 0 ? '+' : '' }}₫{{ portfolio.totalUnrealizedPnL() | number:'1.0-0' }}
                </span>
              </div>
              <div class="flex items-center justify-between">
                <span class="text-small text-fg-muted">Tiền mặt</span>
                <span class="text-body font-medium font-numeric text-fg">₫{{ portfolio.cashBalance() | number:'1.0-0' }}</span>
              </div>
              <hr class="border-border">
              <div class="grid grid-cols-2 gap-2">
                <app-btn variant="primary" size="sm" label="Nạp tiền" [fullWidth]="true" />
                <app-btn variant="secondary" size="sm" label="Rút tiền" [fullWidth]="true" />
              </div>
            </div>
          </app-card>

          <!-- Top movers -->
          <app-card title="Top tăng/giảm" variant="default" [hasHeaderAction]="true">
            <ng-container slot="header-action">
              <app-tab-nav
                [tabs]="moverTabs"
                [activeId]="activeMoverTab()"
                variant="pills"
                (activeIdChange)="activeMoverTab.set($event)"
              />
            </ng-container>

            <div class="mt-3 space-y-2">
              @for (stock of topMovers(); track stock.symbol) {
                <a
                  [routerLink]="['/stocks', stock.symbol]"
                  class="flex items-center justify-between py-2 border-b border-border/50 last:border-0 hover:bg-surface-2 -mx-4 px-4 transition-colors"
                >
                  <div>
                    <p class="text-small font-semibold text-fg">{{ stock.symbol }}</p>
                    <p class="text-xs text-fg-muted font-numeric">{{ stock.price | number:'1.1-2' }}</p>
                  </div>
                  <app-badge [variant]="stock.changePct >= 0 ? 'up' : 'down'" size="sm">
                    {{ stock.changePct >= 0 ? '+' : '' }}{{ stock.changePct | number:'1.2-2' }}%
                  </app-badge>
                </a>
              }
            </div>
          </app-card>

          <!-- Risk quick-card -->
          <app-card title="⚡ Rủi ro tài khoản" variant="default" [hasHeaderAction]="true">
            <ng-container slot="header-action">
              <a routerLink="/risk" class="text-xs text-up hover:underline">Chi tiết</a>
            </ng-container>
            <div class="space-y-2.5">
              <!-- Buying Power -->
              <div class="flex items-center justify-between">
                <span class="text-small text-fg-muted">💰 Sức mua</span>
                @if (riskSvc.buyingPower(); as bp) {
                  <span class="text-small font-semibold font-numeric text-fg">
                    {{ bp.buyingPower | number:'1.0-0' }} ₫
                  </span>
                } @else {
                  <span class="text-xs text-fg-muted">—</span>
                }
              </div>
              <!-- Rtt -->
              <div class="flex items-center justify-between">
                <span class="text-small text-fg-muted">📊 Tỷ lệ TK (Rtt)</span>
                @if (riskSvc.rtt(); as rtt) {
                  @if (rtt.loanAmount === 0) {
                    <span class="text-xs font-semibold" style="color:#22c55e">Không có nợ</span>
                  } @else {
                    <span class="text-small font-semibold font-numeric"
                      [style.color]="rtt.rtt < 0.80 ? '#ef4444' : rtt.rtt < 0.85 ? '#f59e0b' : '#22c55e'">
                      {{ rtt.rtt | percent:'1.1-1' }}
                    </span>
                  }
                } @else {
                  <span class="text-xs text-fg-muted">—</span>
                }
              </div>
              <!-- Warning banner inline -->
              @if (riskSvc.rtt()?.isAtRisk) {
                <div class="rounded-lg p-2.5 text-xs flex items-start gap-2"
                  [style.background]="riskSvc.rtt()?.status === 'FORCE_SELL_ZONE' ? 'rgba(239,68,68,.1)' : 'rgba(245,158,11,.1)'"
                  [style.color]="riskSvc.rtt()?.status === 'FORCE_SELL_ZONE' ? '#f87171' : '#fbbf24'">
                  <span>{{ riskSvc.rtt()?.status === 'FORCE_SELL_ZONE' ? '🚨' : '⚠️' }}</span>
                  <span>{{ riskSvc.getRttStatusLabel(riskSvc.rtt()!.status) }}</span>
                </div>
              }
            </div>
          </app-card>

          <!-- Latest alerts teaser -->
          <app-card title="Cảnh báo gần đây" variant="default" [hasHeaderAction]="true">
            <ng-container slot="header-action">
              <a routerLink="/alerts" class="text-xs text-up hover:underline">Xem tất cả</a>
            </ng-container>
            <div class="space-y-2 mt-1">
              @for (alert of recentAlerts; track alert.id) {
                <div class="flex items-start gap-2.5 py-2 border-b border-border/50 last:border-0">
                  <span [class]="'w-2 h-2 rounded-full mt-1.5 shrink-0 ' + (alert.triggered ? 'bg-down' : 'bg-up')"></span>
                  <div class="flex-1 min-w-0">
                    <p class="text-small text-fg font-medium">{{ alert.title }}</p>
                    <p class="text-xs text-fg-muted truncate">{{ alert.desc }}</p>
                  </div>
                </div>
              }
            </div>
          </app-card>
        </div>
      </div>
    </div>
  `,

})
export class DashboardComponent implements OnInit {
  public readonly marketData = inject(MarketDataService);
  public readonly portfolio = inject(PortfolioService);
  public readonly riskSvc = inject(RiskService);
  private readonly router = inject(Router);

  readonly todayLabel = computed(() => {
    const now = new Date();
    const days = ['Chủ nhật', 'Thứ hai', 'Thứ ba', 'Thứ tư', 'Thứ năm', 'Thứ sáu', 'Thứ bảy'];
    const d = String(now.getDate()).padStart(2, '0');
    const m = String(now.getMonth() + 1).padStart(2, '0');
    const y = now.getFullYear();
    return `${days[now.getDay()]}, ${d}/${m}/${y}`;
  });

  readonly loadingTable = signal(true);
  readonly activeMarketTab = signal('hose');
  readonly activeMoverTab = signal('gainers');

  readonly marketTabs: TabItem[] = [
    { id: 'watchlist', label: 'Theo dõi', badge: 12 },
    { id: 'hose', label: 'Thị trường chung' }
  ];

  readonly moverTabs: TabItem[] = [
    { id: 'gainers', label: 'Tăng' },
    { id: 'losers', label: 'Giảm' },
  ];

  readonly currentStocks = computed(() => {
    return this.marketData.stocks();
  });

  readonly topMovers = computed(() => {
    const list = [...this.marketData.stocks()];
    list.sort((a, b) =>
      this.activeMoverTab() === 'gainers'
        ? b.changePct - a.changePct
        : a.changePct - b.changePct
    );
    return list.slice(0, 5);
  });

  readonly recentAlerts = [
    { id: 1, title: 'VNM vượt 65.5', desc: 'Giá đã chạm ngưỡng cảnh báo 65.5', triggered: true },
    { id: 2, title: 'RSI FPT < 30', desc: 'Tín hiệu RSI oversold – xem xét mua', triggered: true },
    { id: 3, title: 'TCB target 25.0', desc: 'Chờ giá đạt 25.0 để kích hoạt', triggered: false },
  ];

  ngOnInit(): void {
    this.loadingTable.set(true);

    this.marketData.getSymbols().subscribe({
      next: () => this.loadingTable.set(false),
      error: () => this.loadingTable.set(false)
    });

    this.portfolio.loadWallet().subscribe();
    this.portfolio.loadPortfolio().subscribe();

    // Load risk data for sidebar widget
    this.riskSvc.loadBuyingPower().subscribe();
    this.riskSvc.loadRtt().subscribe();
  }

  simulateLoad(): void {
    this.loadingTable.set(true);
    this.marketData.getSymbols().subscribe({
      next: () => this.loadingTable.set(false),
      error: () => this.loadingTable.set(false)
    });
  }

  onStockClick(stock: StockRow): void {
    this.router.navigate(['/stocks', stock.symbol]);
  }
}
