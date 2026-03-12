import { Component, signal, ChangeDetectionStrategy, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { PortfolioService } from '../../core/services/portfolio.service';
import { RiskService } from '../../core/services/risk.service';
import { PortfolioSummaryComponent, type Holding } from '../../shared/organisms/portfolio-summary/portfolio-summary.component';
import { TabNavComponent, type TabItem } from '../../shared/molecules/tab-nav/tab-nav.component';

@Component({
  selector: 'app-portfolio',
  standalone: true,
  imports: [CommonModule, PortfolioSummaryComponent, TabNavComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  styles: [`
    .risk-panel {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
      gap: 1rem;
    }
    .risk-card {
      background: var(--color-surface, #1e2130);
      border: 1px solid var(--color-border, #2d3250);
      border-radius: 0.75rem;
      padding: 1rem 1.25rem;
    }
    .risk-card .label {
      font-size: 0.75rem;
      color: var(--color-fg-muted, #9ca3af);
      text-transform: uppercase;
      letter-spacing: 0.05em;
      margin-bottom: 0.25rem;
    }
    .risk-card .value {
      font-size: 1.375rem;
      font-weight: 700;
      color: var(--color-fg, #e5e7eb);
    }
    .risk-card .sub {
      font-size: 0.75rem;
      color: var(--color-fg-muted, #9ca3af);
      margin-top: 0.2rem;
    }
    .risk-safe    { color: #22c55e; }
    .risk-warning { color: #f59e0b; }
    .risk-danger  { color: #ef4444; animation: pulse 1.5s infinite; }
    .risk-neutral { color: var(--color-fg-muted, #9ca3af); }

    .alert-list { display: flex; flex-direction: column; gap: 0.5rem; }
    .alert-item {
      display: flex; align-items: flex-start; gap: 0.75rem;
      padding: 0.75rem 1rem;
      border-radius: 0.5rem;
      background: var(--color-surface, #1e2130);
      border: 1px solid var(--color-border, #2d3250);
    }
    .alert-item.CALL_MARGIN { border-left: 3px solid #f59e0b; }
    .alert-item.FORCE_SELL  { border-left: 3px solid #ef4444; }
    .alert-icon { font-size: 1.1rem; flex-shrink: 0; margin-top: 0.1rem; }
    .alert-msg  { font-size: 0.8rem; color: var(--color-fg, #e5e7eb); flex: 1; }
    .alert-time { font-size: 0.7rem; color: var(--color-fg-muted, #9ca3af); white-space: nowrap; }

    @keyframes pulse {
      0%, 100% { opacity: 1; }
      50%       { opacity: 0.5; }
    }
  `],
  template: `
    <div class="p-4 md:p-6 space-y-6 animate-fade-in">
      <div>
        <h1 class="text-headline font-bold text-fg">Danh mục đầu tư</h1>
        <p class="text-small text-fg-muted mt-0.5">Cập nhật lúc 14:42 · Phiên hôm nay</p>
      </div>

      <!-- ===== Risk Panel ===== -->
      <section>
        <h2 class="text-sm font-semibold text-fg-muted uppercase tracking-wider mb-3">
          📊 Quản trị rủi ro
        </h2>
        <div class="risk-panel">

          <!-- Buying Power Card -->
          <div class="risk-card">
            <div class="label">💰 Sức mua</div>
            @if (riskService.buyingPower(); as bp) {
              <div class="value">{{ bp.buyingPower | number:'1.0-0' }} ₫</div>
              <div class="sub">
                Tiền mặt: {{ bp.availableCash | number:'1.0-0' }} ₫
                @if (bp.marginValue > 0) {
                  · Margin: {{ bp.marginValue | number:'1.0-0' }} ₫
                }
              </div>
            } @else {
              <div class="value" style="color:var(--color-fg-muted)">Đang tải…</div>
            }
          </div>

          <!-- Rtt Card -->
          <div class="risk-card">
            <div class="label">📈 Tỷ lệ tài khoản (Rtt)</div>
            @if (riskService.rtt(); as rtt) {
              @if (rtt.loanAmount === 0) {
                <div class="value risk-safe">∞</div>
                <div class="sub">Không có dư nợ margin</div>
              } @else {
                <div class="value" [class]="riskService.getRttStatusClass(rtt.status)">
                  {{ rtt.rtt | percent:'1.1-1' }}
                </div>
                <div class="sub" [class]="riskService.getRttStatusClass(rtt.status)">
                  {{ riskService.getRttStatusLabel(rtt.status) }}
                  · Nợ: {{ rtt.loanAmount | number:'1.0-0' }} ₫
                </div>
              }
            } @else {
              <div class="value" style="color:var(--color-fg-muted)">Đang tải…</div>
            }
          </div>

          <!-- Call Margin / Force Sell Alert Card -->
          @if (riskService.rtt()?.isAtRisk) {
            <div class="risk-card"
                 [style.border-color]="riskService.rtt()?.status === 'FORCE_SELL_ZONE' ? '#ef4444' : '#f59e0b'"
                 [style.background]="riskService.rtt()?.status === 'FORCE_SELL_ZONE' ? 'rgba(239,68,68,0.08)' : 'rgba(245,158,11,0.08)'">
              <div class="label">⚠️ Cảnh báo</div>
              @if (riskService.rtt()?.status === 'FORCE_SELL_ZONE') {
                <div class="value risk-danger" style="font-size:1rem;">Đang bán giải chấp!</div>
                <div class="sub risk-danger">Rtt dưới 80% — Force Sell đã được kích hoạt.</div>
              } @else {
                <div class="value risk-warning" style="font-size:1rem;">Call Margin</div>
                <div class="sub risk-warning">Vui lòng nộp thêm tiền ký quỹ ngay.</div>
              }
            </div>
          }

        </div>
      </section>

      <!-- ===== Recent Risk Alerts ===== -->
      @if (riskService.alerts().length > 0) {
        <section>
          <h2 class="text-sm font-semibold text-fg-muted uppercase tracking-wider mb-3">
            🔔 Cảnh báo gần đây
          </h2>
          <div class="alert-list">
            @for (alert of riskService.alerts().slice(0, 5); track alert.alertId) {
              <div class="alert-item" [class]="alert.alertType">
                <span class="alert-icon">{{ alert.alertType === 'FORCE_SELL' ? '🚨' : '⚠️' }}</span>
                <span class="alert-msg">{{ alert.message }}</span>
                <span class="alert-time">{{ alert.createdAt | date:'dd/MM HH:mm' }}</span>
              </div>
            }
          </div>
        </section>
      }

      <!-- ===== Portfolio Tabs ===== -->
      <app-tab-nav [tabs]="tabs" [activeId]="activeTab()" variant="underline"
        (activeIdChange)="activeTab.set($event)" />

      @if (activeTab() === 'holdings') {
        <app-portfolio-summary
          [holdings]="portfolio.holdings()"
          [cashBalance]="portfolio.cashBalance()"
          [realizedPnL]="0"
          [loading]="loading()"
        />
      }
    </div>
  `,
})
export class PortfolioComponent implements OnInit {
  public readonly portfolio = inject(PortfolioService);
  public readonly riskService = inject(RiskService);
  readonly loading = signal(true);
  readonly activeTab = signal('holdings');

  readonly tabs: TabItem[] = [
    { id: 'holdings', label: 'Cổ phiếu' },
    { id: 'transactions', label: 'Giao dịch' },
    { id: 'performance', label: 'Hiệu suất' },
  ];

  ngOnInit(): void {
    this.loading.set(true);
    this.portfolio.loadWallet().subscribe();
    this.portfolio.loadPortfolio().subscribe({
      next: () => this.loading.set(false),
      error: () => this.loading.set(false)
    });

    this.riskService.loadBuyingPower().subscribe();
    this.riskService.loadRtt().subscribe();
    this.riskService.loadAlerts().subscribe();
  }
}
