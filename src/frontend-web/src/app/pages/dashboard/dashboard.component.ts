import {
  Component, signal, computed,
  ChangeDetectionStrategy, OnInit,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { PriceTableComponent, type StockRow } from '../../shared/organisms/price-table/price-table.component';
import { PortfolioSummaryComponent, type Holding } from '../../shared/organisms/portfolio-summary/portfolio-summary.component';
import { StatBoxComponent, type StatBoxData } from '../../shared/molecules/stat-box/stat-box.component';
import { TabNavComponent, type TabItem } from '../../shared/molecules/tab-nav/tab-nav.component';
import { CardComponent } from '../../shared/molecules/card/card.component';
import { BadgeComponent } from '../../shared/atoms/badge/badge.component';
import { ButtonComponent } from '../../shared/atoms/button/button.component';
import { IconComponent } from '../../shared/atoms/icon/icon.component';
import { SkeletonComponent } from '../../shared/atoms/skeleton/skeleton.component';

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
          <p class="text-small text-fg-muted mt-0.5">Thứ 5, 06/03/2026 · Phiên HOSE đang mở</p>
        </div>
        <app-btn variant="primary" size="sm" label="Đặt lệnh nhanh">
          <app-icon slot="icon-left" name="trending-up" size="sm" />
        </app-btn>
      </div>

      <!-- ── Market indices ── -->
      <div class="grid grid-cols-2 lg:grid-cols-4 gap-4">
        @for (stat of marketStats; track stat.title) {
          <app-stat-box [data]="stat" />
        }
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
                <span class="text-body font-bold font-numeric text-fg">₫245,820,000</span>
              </div>
              <div class="flex items-center justify-between">
                <span class="text-small text-fg-muted">Lãi/Lỗ hôm nay</span>
                <span class="text-body font-semibold font-numeric text-up">+₫1,234,500 (+0.50%)</span>
              </div>
              <div class="flex items-center justify-between">
                <span class="text-small text-fg-muted">Tiền mặt</span>
                <span class="text-body font-medium font-numeric text-fg">₫42,000,000</span>
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
  readonly loadingTable = signal(true);
  readonly activeMarketTab = signal('hose');
  readonly activeMoverTab = signal('gainers');

  readonly marketTabs: TabItem[] = [
    { id: 'watchlist', label: 'Theo dõi', badge: 12 },
    { id: 'hose', label: 'HOSE' },
    { id: 'hnx', label: 'HNX' },
    { id: 'upcom', label: 'UPCOM' },
  ];

  readonly moverTabs: TabItem[] = [
    { id: 'gainers', label: 'Tăng' },
    { id: 'losers', label: 'Giảm' },
  ];

  readonly marketStats: StatBoxData[] = [
    { title: 'VN-Index', value: 1274.52, change: +0.68, trend: 'up', icon: 'bar-chart-2', caption: '+8.67 điểm' },
    { title: 'HNX-Index', value: 231.85, change: -0.31, trend: 'down', icon: 'bar-chart-2', caption: '-0.72 điểm' },
    { title: 'Mã tăng', value: 187, suffix: 'CP', trend: 'up', icon: 'arrow-up', caption: 'trong phiên' },
    { title: 'Mã giảm', value: 124, suffix: 'CP', trend: 'down', icon: 'arrow-down', caption: 'trong phiên' },
  ];

  private readonly allStocks: StockRow[] = [
    { symbol: 'VNM', name: 'Vinamilk', price: 65.8, refPrice: 65.0, ceilPrice: 69.55, floorPrice: 60.45, change: +0.80, changePct: +1.23, volume: 1_250_400, value: 82.2 },
    { symbol: 'FPT', name: 'FPT Corporation', price: 120.5, refPrice: 124.0, ceilPrice: 132.7, floorPrice: 115.3, change: -3.50, changePct: -2.82, volume: 3_870_100, value: 466.4 },
    { symbol: 'HPG', name: 'Hòa Phát Group', price: 27.35, refPrice: 27.35, ceilPrice: 29.26, floorPrice: 25.44, change: 0.00, changePct: 0.00, volume: 8_420_000, value: 230.3 },
    { symbol: 'VIC', name: 'Vingroup', price: 47.2, refPrice: 45.5, ceilPrice: 48.69, floorPrice: 42.31, change: +1.70, changePct: +3.74, volume: 2_100_000, value: 99.1 },
    { symbol: 'VHM', name: 'Vinhomes', price: 38.1, refPrice: 38.8, ceilPrice: 41.52, floorPrice: 36.08, change: -0.70, changePct: -1.80, volume: 5_630_000, value: 214.5 },
    { symbol: 'TCB', name: 'Techcombank', price: 24.5, refPrice: 24.0, ceilPrice: 25.68, floorPrice: 22.32, change: +0.50, changePct: +2.08, volume: 6_410_000, value: 157.1 },
    { symbol: 'VCB', name: 'Vietcombank', price: 78.3, refPrice: 79.5, ceilPrice: 85.07, floorPrice: 73.93, change: -1.20, changePct: -1.51, volume: 1_890_000, value: 147.9 },
    { symbol: 'MSN', name: 'Masan Group', price: 63.4, refPrice: 61.0, ceilPrice: 65.27, floorPrice: 56.73, change: +2.40, changePct: +3.93, volume: 980_000, value: 62.1 },
    { symbol: 'GVR', name: 'Cao su Việt Nam', price: 15.25, refPrice: 16.1, ceilPrice: 17.23, floorPrice: 14.97, change: -0.85, changePct: -5.28, volume: 4_200_000, value: 64.1 },
    { symbol: 'PLX', name: 'Petrolimex', price: 32.6, refPrice: 31.5, ceilPrice: 33.71, floorPrice: 29.29, change: +1.10, changePct: +3.49, volume: 1_560_000, value: 50.9 },
  ];

  readonly currentStocks = computed(() => this.allStocks);

  readonly topMovers = computed(() => {
    const sorted = [...this.allStocks].sort((a, b) =>
      this.activeMoverTab() === 'gainers'
        ? b.changePct - a.changePct
        : a.changePct - b.changePct
    );
    return sorted.slice(0, 5);
  });

  readonly recentAlerts = [
    { id: 1, title: 'VNM vượt 65.5', desc: 'Giá đã chạm ngưỡng cảnh báo 65.5', triggered: true },
    { id: 2, title: 'RSI FPT < 30', desc: 'Tín hiệu RSI oversold – xem xét mua', triggered: true },
    { id: 3, title: 'TCB target 25.0', desc: 'Chờ giá đạt 25.0 để kích hoạt', triggered: false },
  ];

  ngOnInit(): void {
    this.simulateLoad();
  }

  simulateLoad(): void {
    this.loadingTable.set(true);
    setTimeout(() => this.loadingTable.set(false), 1200);
  }

  onStockClick(stock: StockRow): void {
    console.log('Navigate to:', stock.symbol);
  }
}
