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
import { AlertService, AlertRule } from '../../core/services/alert.service';

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
  templateUrl: './dashboard.component.html',
})
export class DashboardComponent implements OnInit {
  public readonly marketData = inject(MarketDataService);
  public readonly portfolio = inject(PortfolioService);
  public readonly riskSvc = inject(RiskService);
  private readonly router = inject(Router);
  private readonly alertSvc = inject(AlertService);

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

  readonly recentAlerts = signal<AlertRule[]>([]);

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

    this.alertSvc.getMyAlerts().subscribe({
      next: alerts => this.recentAlerts.set(alerts.slice(0, 3)),
      error: () => {}
    });
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

  onQuickOrder(): void {
    this.router.navigate(['/stocks']);
  }

  conditionLabel(c: string): string {
    return ({ gt: '>', gte: '≥', lt: '<', lte: '≤' } as Record<string, string>)[c] ?? c;
  }
}
