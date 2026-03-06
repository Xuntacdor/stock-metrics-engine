import { Component, signal, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { PortfolioSummaryComponent, type Holding } from '../../shared/organisms/portfolio-summary/portfolio-summary.component';
import { TabNavComponent, type TabItem } from '../../shared/molecules/tab-nav/tab-nav.component';

@Component({
    selector: 'app-portfolio',
    standalone: true,
    imports: [CommonModule, PortfolioSummaryComponent, TabNavComponent],
    changeDetection: ChangeDetectionStrategy.OnPush,
    template: `
    <div class="p-4 md:p-6 space-y-6 animate-fade-in">
      <div>
        <h1 class="text-headline font-bold text-fg">Danh mục đầu tư</h1>
        <p class="text-small text-fg-muted mt-0.5">Cập nhật lúc 14:42 · Phiên hôm nay</p>
      </div>

      <app-tab-nav [tabs]="tabs" [activeId]="activeTab()" variant="underline"
        (activeIdChange)="activeTab.set($event)" />

      @if (activeTab() === 'holdings') {
        <app-portfolio-summary
          [holdings]="holdings"
          [cashBalance]="42000000"
          [realizedPnL]="8120000"
          [loading]="false"
        />
      }
    </div>
  `,
})
export class PortfolioComponent {
    readonly activeTab = signal('holdings');

    readonly tabs: TabItem[] = [
        { id: 'holdings', label: 'Cổ phiếu' },
        { id: 'transactions', label: 'Giao dịch' },
        { id: 'performance', label: 'Hiệu suất' },
    ];

    readonly holdings: Holding[] = [
        { symbol: 'VNM', name: 'Vinamilk', quantity: 1000, avgCost: 63.0, currentPrice: 65.8, marketValue: 65_800_000, unrealizedPnL: 2_800_000, unrealizedPct: 4.44, sector: 'Tiêu dùng' },
        { symbol: 'FPT', name: 'FPT Corp', quantity: 500, avgCost: 125.0, currentPrice: 120.5, marketValue: 60_250_000, unrealizedPnL: -2_250_000, unrealizedPct: -3.60, sector: 'Công nghệ' },
        { symbol: 'VIC', name: 'Vingroup', quantity: 800, avgCost: 44.0, currentPrice: 47.2, marketValue: 37_760_000, unrealizedPnL: 2_560_000, unrealizedPct: 7.27, sector: 'Bất động sản' },
        { symbol: 'HPG', name: 'Hòa Phát', quantity: 2000, avgCost: 26.5, currentPrice: 27.35, marketValue: 54_700_000, unrealizedPnL: 1_700_000, unrealizedPct: 3.21, sector: 'Vật liệu' },
        { symbol: 'TCB', name: 'Techcombank', quantity: 1500, avgCost: 23.0, currentPrice: 24.5, marketValue: 36_750_000, unrealizedPnL: 2_250_000, unrealizedPct: 6.52, sector: 'Ngân hàng' },
    ];
}
