import { Component, signal, computed, ChangeDetectionStrategy, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { PortfolioService } from '../../core/services/portfolio.service';
import { RiskService } from '../../core/services/risk.service';
import { TransactionService, TransactionResponse } from '../../core/services/transaction.service';
import { PortfolioSummaryComponent } from '../../shared/organisms/portfolio-summary/portfolio-summary.component';
import { TabNavComponent, type TabItem } from '../../shared/molecules/tab-nav/tab-nav.component';
import { CardComponent } from '../../shared/molecules/card/card.component';
import { BadgeComponent } from '../../shared/atoms/badge/badge.component';
import { SkeletonComponent } from '../../shared/atoms/skeleton/skeleton.component';

@Component({
  selector: 'app-portfolio',
  standalone: true,
  imports: [CommonModule, RouterLink, PortfolioSummaryComponent, TabNavComponent, CardComponent, BadgeComponent, SkeletonComponent],
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
    .risk-card .label { font-size: .75rem; color: var(--color-fg-muted, #9ca3af); text-transform: uppercase; letter-spacing: .05em; margin-bottom: .25rem; }
    .risk-card .value { font-size: 1.375rem; font-weight: 700; color: var(--color-fg, #e5e7eb); }
    .risk-card .sub   { font-size: .75rem; color: var(--color-fg-muted, #9ca3af); margin-top: .2rem; }
    .risk-safe    { color: #22c55e; }
    .risk-warning { color: #f59e0b; }
    .risk-danger  { color: #ef4444; animation: pulse 1.5s infinite; }
    .risk-neutral { color: var(--color-fg-muted, #9ca3af); }
    @keyframes pulse { 0%, 100% { opacity: 1; } 50% { opacity: .5; } }

    /* tx table */
    .tx-type-badge {
      display: inline-flex; align-items: center; gap: .3rem;
      padding: .15rem .6rem; border-radius: 999px;
      font-size: .7rem; font-weight: 600;
    }
    .tx-BUY    { background: rgba(34,197, 94,.12); color: #22c55e; }
    .tx-SELL   { background: rgba(239, 68, 68,.12); color: #ef4444; }
    .tx-DEPOSIT  { background: rgba(99,102,241,.12); color: #818cf8; }
    .tx-WITHDRAW { background: rgba(245,158, 11,.12); color: #fbbf24; }
    .tx-other  { background: rgba(156,163,175,.12); color: #9ca3af; }

    /* perf */
    .perf-card { background: var(--color-surface, #1e2130); border: 1px solid var(--color-border, #2d3250); border-radius: .75rem; padding: 1.25rem; }
    .alloc-bar { height: 8px; border-radius: 4px; background: var(--color-surface-2, #2d3250); overflow: hidden; }
    .alloc-fill { height: 100%; border-radius: 4px; transition: width .6s ease; }
  `],
  template: `
    <div class="p-4 md:p-6 space-y-6 animate-fade-in">

      <!-- Header -->
      <div>
        <h1 class="text-headline font-bold text-fg">Danh mục đầu tư</h1>
        <p class="text-small text-fg-muted mt-0.5">
          Cập nhật lúc {{ now }} · Phiên hôm nay
        </p>
      </div>

      <!-- Risk Panel -->
      <section>
        <h2 class="text-sm font-semibold text-fg-muted uppercase tracking-wider mb-3">📊 Quản trị rủi ro</h2>
        <div class="risk-panel">
          <!-- Buying Power -->
          <div class="risk-card">
            <div class="label">💰 Sức mua</div>
            @if (riskService.buyingPower(); as bp) {
              <div class="value">{{ bp.buyingPower | number:'1.0-0' }} ₫</div>
              <div class="sub">Tiền mặt: {{ bp.availableCash | number:'1.0-0' }} ₫
                @if (bp.marginValue > 0) { · Margin: {{ bp.marginValue | number:'1.0-0' }} ₫ }
              </div>
            } @else {
              <div class="value" style="color:var(--color-fg-muted)">Đang tải…</div>
            }
          </div>

          <!-- Rtt -->
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
                  {{ riskService.getRttStatusLabel(rtt.status) }} · Nợ: {{ rtt.loanAmount | number:'1.0-0' }} ₫
                </div>
              }
            } @else {
              <div class="value" style="color:var(--color-fg-muted)">Đang tải…</div>
            }
          </div>

          <!-- Alert banner -->
          @if (riskService.rtt()?.isAtRisk) {
            <div class="risk-card"
                 [style.border-color]="riskService.rtt()?.status === 'FORCE_SELL_ZONE' ? '#ef4444' : '#f59e0b'"
                 [style.background]="riskService.rtt()?.status === 'FORCE_SELL_ZONE' ? 'rgba(239,68,68,.08)' : 'rgba(245,158,11,.08)'">
              <div class="label">⚠️ Cảnh báo</div>
              @if (riskService.rtt()?.status === 'FORCE_SELL_ZONE') {
                <div class="value risk-danger" style="font-size:1rem;">Đang bán giải chấp!</div>
                <div class="sub risk-danger">Rtt dưới 80% — Force Sell đã kích hoạt.</div>
              } @else {
                <div class="value risk-warning" style="font-size:1rem;">Call Margin</div>
                <div class="sub risk-warning">Vui lòng nộp thêm tiền ký quỹ ngay.</div>
              }
            </div>
          }
        </div>
      </section>

      <!-- Tabs -->
      <app-tab-nav [tabs]="tabs" [activeId]="activeTab()" variant="underline"
        (activeIdChange)="activeTab.set($event)" />

      <!-- ── Tab: Cổ phiếu ── -->
      @if (activeTab() === 'holdings') {
        <app-portfolio-summary
          [holdings]="portfolio.holdings()"
          [cashBalance]="portfolio.cashBalance()"
          [realizedPnL]="0"
          [loading]="loading()"
        />
      }

      <!-- ── Tab: Giao dịch ── -->
      @if (activeTab() === 'transactions') {
        <app-card title="Lịch sử giao dịch" variant="default" [hasHeaderAction]="true">
          <ng-container slot="header-action">
            <span class="text-xs text-fg-muted">{{ transactions().length }} giao dịch</span>
          </ng-container>

          @if (loadingTx()) {
            <div class="space-y-3 mt-2">
              @for (i of [1,2,3,4]; track i) { <app-skeleton height="48px" /> }
            </div>
          } @else if (transactions().length === 0) {
            <div class="flex flex-col items-center py-12 text-center text-fg-muted">
              <span class="text-4xl mb-3">📋</span>
              <p class="font-medium text-fg">Chưa có giao dịch nào</p>
              <p class="text-small mt-1">Đặt lệnh mua/bán để lịch sử xuất hiện ở đây.</p>
            </div>
          } @else {
            <div class="overflow-x-auto mt-2">
              <table class="w-full text-small">
                <thead>
                  <tr class="border-b border-border text-xs text-fg-muted text-left">
                    <th class="pb-2 font-medium">Loại</th>
                    <th class="pb-2 font-medium">Mô tả</th>
                    <th class="pb-2 font-medium text-right">Số tiền</th>
                    <th class="pb-2 font-medium text-right">Số dư sau</th>
                    <th class="pb-2 font-medium text-right">Thời gian</th>
                  </tr>
                </thead>
                <tbody>
                  @for (tx of transactions(); track tx.transId) {
                    <tr class="border-b border-border/40 hover:bg-surface-2 transition-colors">
                      <td class="py-2.5">
                        <span [class]="'tx-type-badge tx-' + tx.transType">
                          {{ txTypeLabel(tx.transType) }}
                        </span>
                      </td>
                      <td class="py-2.5 text-fg-muted max-w-[200px] truncate">{{ tx.description }}</td>
                      <td class="py-2.5 text-right font-numeric"
                          [class]="tx.amount >= 0 ? 'text-up' : 'text-down'">
                        {{ tx.amount >= 0 ? '+' : '' }}{{ tx.amount | number:'1.0-0' }} ₫
                      </td>
                      <td class="py-2.5 text-right font-numeric text-fg">
                        {{ tx.balanceAfter | number:'1.0-0' }} ₫
                      </td>
                      <td class="py-2.5 text-right text-fg-muted">
                        {{ tx.transTime | date:'dd/MM HH:mm' }}
                      </td>
                    </tr>
                  }
                </tbody>
              </table>
            </div>
          }
        </app-card>
      }

      <!-- ── Tab: Hiệu suất ── -->
      @if (activeTab() === 'performance') {
        <div class="grid grid-cols-1 sm:grid-cols-3 gap-4 mb-6">
          <!-- Tổng tài sản -->
          <div class="perf-card">
            <p class="text-xs text-fg-muted uppercase tracking-wider mb-1">Tổng tài sản</p>
            <p class="text-title font-bold font-numeric text-fg">
              {{ portfolio.totalPortfolioValue() | number:'1.0-0' }} ₫
            </p>
            <p class="text-xs text-fg-muted mt-1">Tiền mặt + Cổ phiếu</p>
          </div>

          <!-- Lãi/Lỗ chưa thực hiện -->
          <div class="perf-card">
            <p class="text-xs text-fg-muted uppercase tracking-wider mb-1">Lãi/Lỗ tạm tính</p>
            <p class="text-title font-bold font-numeric"
               [class]="portfolio.totalUnrealizedPnL() >= 0 ? 'text-up' : 'text-down'">
              {{ portfolio.totalUnrealizedPnL() >= 0 ? '+' : '' }}{{ portfolio.totalUnrealizedPnL() | number:'1.0-0' }} ₫
            </p>
            <p class="text-xs mt-1"
               [class]="portfolio.totalUnrealizedPnL() >= 0 ? 'text-up' : 'text-down'">
              {{ unrealizedPct() >= 0 ? '+' : '' }}{{ unrealizedPct() | number:'1.2-2' }}%
            </p>
          </div>

          <!-- Tiền mặt -->
          <div class="perf-card">
            <p class="text-xs text-fg-muted uppercase tracking-wider mb-1">Tiền mặt khả dụng</p>
            <p class="text-title font-bold font-numeric text-fg">
              {{ portfolio.cashBalance() | number:'1.0-0' }} ₫
            </p>
            <p class="text-xs text-fg-muted mt-1">
              {{ cashPct() | number:'1.1-1' }}% danh mục
            </p>
          </div>
        </div>

        <!-- Phân bổ danh mục -->
        @if (portfolio.holdings().length > 0) {
          <app-card title="Phân bổ danh mục" variant="default">
            <div class="space-y-4 mt-2">
              @for (h of topHoldings(); track h.symbol) {
                <div>
                  <div class="flex justify-between text-small mb-1.5">
                    <div class="flex items-center gap-2">
                      <span class="font-semibold text-fg">{{ h.symbol }}</span>
                      <span class="text-xs text-fg-muted">{{ h.quantity | number }} CP</span>
                    </div>
                    <div class="text-right">
                      <span class="font-numeric text-fg">{{ h.marketValue | number:'1.0-0' }} ₫</span>
                      <span class="ml-2 text-xs"
                            [class]="h.unrealizedPnL >= 0 ? 'text-up' : 'text-down'">
                        ({{ h.unrealizedPnL >= 0 ? '+' : '' }}{{ h.unrealizedPct | number:'1.1-1' }}%)
                      </span>
                    </div>
                  </div>
                  <div class="alloc-bar">
                    <div class="alloc-fill"
                         [style.width.%]="holdingPct(h.marketValue)"
                         [style.background]="h.unrealizedPnL >= 0 ? '#22c55e' : '#ef4444'">
                    </div>
                  </div>
                </div>
              }
            </div>
          </app-card>
        } @else {
          <div class="flex flex-col items-center py-16 text-fg-muted text-center">
            <span class="text-4xl mb-3">📊</span>
            <p class="font-medium text-fg">Danh mục trống</p>
            <p class="text-small mt-1">Mua cổ phiếu để xem hiệu suất đầu tư.</p>
          </div>
        }
      }
    </div>
  `,
})
export class PortfolioComponent implements OnInit {
  public readonly portfolio = inject(PortfolioService);
  public readonly riskService = inject(RiskService);
  private readonly txService = inject(TransactionService);

  readonly loading = signal(true);
  readonly loadingTx = signal(false);
  readonly activeTab = signal('holdings');
  readonly transactions = signal<TransactionResponse[]>([]);

  readonly now = new Date().toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' });

  readonly tabs: TabItem[] = [
    { id: 'holdings', label: 'Cổ phiếu' },
    { id: 'transactions', label: 'Giao dịch' },
    { id: 'performance', label: 'Hiệu suất' },
  ];

  // Cost basis = sum of (avgCost × quantity) for each holding
  private get costBasis(): number {
    return this.portfolio.holdings().reduce((s, h) => s + h.avgCost * h.quantity, 0);
  }

  readonly unrealizedPct = computed(() => {
    const cost = this.costBasis;
    if (cost === 0) return 0;
    return (this.portfolio.totalUnrealizedPnL() / cost) * 100;
  });

  readonly cashPct = computed(() => {
    const total = this.portfolio.totalPortfolioValue();
    if (total === 0) return 0;
    return (this.portfolio.cashBalance() / total) * 100;
  });

  readonly topHoldings = computed(() =>
    [...this.portfolio.holdings()]
      .sort((a, b) => b.marketValue - a.marketValue)
      .slice(0, 8)
  );

  holdingPct(marketValue: number): number {
    const total = this.portfolio.totalPortfolioValue();
    return total > 0 ? Math.min(100, (marketValue / total) * 100) : 0;
  }

  txTypeLabel(type: string): string {
    const map: Record<string, string> = {
      BUY: '📈 Mua', SELL: '📉 Bán',
      DEPOSIT: '💰 Nạp', WITHDRAW: '🏦 Rút',
      DIVIDEND: '💵 Cổ tức', FEE: '🔧 Phí',
    };
    return map[type] ?? type;
  }

  ngOnInit(): void {
    this.loading.set(true);
    this.portfolio.loadWallet().subscribe();
    this.portfolio.loadPortfolio().subscribe({
      next: () => this.loading.set(false),
      error: () => this.loading.set(false),
    });
    this.riskService.loadBuyingPower().subscribe();
    this.riskService.loadRtt().subscribe();
    this.riskService.loadAlerts().subscribe();
    this.loadTransactions();
  }

  private loadTransactions(): void {
    this.loadingTx.set(true);
    this.txService.getMyTransactions().subscribe({
      next: (txs) => {
        const sorted = [...txs].sort(
          (a, b) => new Date(b.transTime).getTime() - new Date(a.transTime).getTime()
        );
        this.transactions.set(sorted);
        this.loadingTx.set(false);
      },
      error: () => this.loadingTx.set(false),
    });
  }
}
