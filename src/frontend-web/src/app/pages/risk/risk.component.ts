import {
  Component, signal, computed,
  ChangeDetectionStrategy, OnInit, inject
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { RiskService, RiskAlertItem } from '../../core/services/risk.service';
import { CardComponent } from '../../shared/molecules/card/card.component';
import { StatBoxComponent } from '../../shared/molecules/stat-box/stat-box.component';
import { SkeletonComponent } from '../../shared/atoms/skeleton/skeleton.component';
import { ButtonComponent } from '../../shared/atoms/button/button.component';
import { BadgeComponent } from '../../shared/atoms/badge/badge.component';
import { TabNavComponent, type TabItem } from '../../shared/molecules/tab-nav/tab-nav.component';

@Component({
  selector: 'app-risk',
  standalone: true,
  imports: [
    CommonModule, RouterLink,
    CardComponent, StatBoxComponent, SkeletonComponent,
    ButtonComponent, BadgeComponent, TabNavComponent
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  styles: [`
    /* ── Rtt Gauge ── */
    .gauge-wrap { position: relative; width: 160px; height: 80px; overflow: hidden; }
    .gauge-bg {
      position: absolute; bottom: 0; left: 0;
      width: 160px; height: 160px;
      border-radius: 50%;
      background: conic-gradient(
        #ef4444 0deg 36deg,     /* Force Sell < 80% */
        #f59e0b 36deg 54deg,    /* Call Margin 80-85% */
        #22c55e 54deg 180deg    /* Safe > 85% */
      );
      clip-path: inset(50% 0 0 0);
    }
    .gauge-needle {
      position: absolute;
      bottom: 0; left: 50%;
      width: 2px; height: 76px;
      transform-origin: bottom center;
      background: var(--needle-color, #e5e7eb);
      border-radius: 2px;
      transition: transform 0.7s cubic-bezier(.4,0,.2,1);
    }
    .gauge-cover {
      position: absolute; bottom: 0; left: 50%;
      transform: translateX(-50%);
      width: 28px; height: 28px;
      border-radius: 50%;
      background: var(--color-surface, #1e2130);
      border: 3px solid var(--color-border, #2d3250);
    }
    /* ── Progress bar ── */
    .bp-track {
      height: 8px; border-radius: 4px;
      background: var(--color-surface-2, #2d3250);
      overflow: hidden;
    }
    .bp-fill {
      height: 100%; border-radius: 4px;
      background: linear-gradient(90deg, #22c55e, #4ade80);
      transition: width 0.6s ease;
    }
    /* ── Alert items ── */
    .alert-row {
      display: flex; align-items: flex-start; gap: .75rem;
      padding: .75rem 1rem;
      border-radius: .5rem;
      background: var(--color-surface, #1e2130);
      border: 1px solid var(--color-border, #2d3250);
      transition: border-color .15s;
    }
    .alert-row:hover { border-color: var(--color-border-hover, #3d4570); }
    .alert-row.CALL_MARGIN { border-left: 3px solid #f59e0b; }
    .alert-row.FORCE_SELL  { border-left: 3px solid #ef4444; }
    .tl-dot {
      width: 10px; height: 10px; border-radius: 50%; margin-top: 4px;
      flex-shrink: 0;
    }
    .tl-dot.CALL_MARGIN { background: #f59e0b; box-shadow: 0 0 6px #f59e0b; }
    .tl-dot.FORCE_SELL  { background: #ef4444; box-shadow: 0 0 6px #ef4444; }

    .status-chip {
      display: inline-flex; align-items: center; gap: .35rem;
      padding: .25rem .75rem; border-radius: 999px;
      font-size: .75rem; font-weight: 600;
    }
    .chip-safe    { background: rgba(34,197,94,.12); color: #22c55e; }
    .chip-warn    { background: rgba(245,158,11,.12); color: #f59e0b; }
    .chip-danger  { background: rgba(239,68,68,.12);  color: #ef4444; }
    .chip-neutral { background: rgba(156,163,175,.12);color: #9ca3af; }
  `],
  template: `
    <div class="p-4 md:p-6 space-y-6 animate-fade-in">

      <!-- ── Header ── -->
      <div class="flex items-center justify-between">
        <div>
          <h1 class="text-headline font-bold text-fg">Quản trị rủi ro</h1>
          <p class="text-small text-fg-muted mt-0.5">
            Margin · Sức mua · Call Margin · Force Sell
          </p>
        </div>
        <app-btn variant="ghost" size="sm" label="Làm mới" (clicked)="refresh()">
        </app-btn>
      </div>

      <!-- ── KPI Row ── -->
      <div class="grid grid-cols-1 sm:grid-cols-3 gap-4">

        <!-- Buying Power -->
        <app-card variant="elevated" padding="md">
          <div class="space-y-3">
            <p class="text-small text-fg-muted uppercase tracking-wider">💰 Sức mua</p>
            @if (loading()) {
              <app-skeleton height="2rem" />
              <app-skeleton height="1rem" width="70%" />
            }
            @if (!loading()) {
              @if (riskSvc.buyingPower(); as bp) {
                <p class="text-headline font-bold font-numeric text-fg">
                  {{ bp.buyingPower | number:'1.0-0' }}&nbsp;₫
                </p>
                <div class="space-y-1.5">
                  <div class="flex justify-between text-xs text-fg-muted">
                    <span>Tiền mặt</span>
                    <span class="font-numeric">{{ bp.availableCash | number:'1.0-0' }} ₫</span>
                  </div>
                  @if (bp.marginValue > 0) {
                    <div class="flex justify-between text-xs text-fg-muted">
                      <span>Margin value</span>
                      <span class="font-numeric text-up">+{{ bp.marginValue | number:'1.0-0' }} ₫</span>
                    </div>
                  }
                  <div class="bp-track mt-2">
                    <div class="bp-fill" [style.width.%]="bpUsedPct()"></div>
                  </div>
                  <p class="text-xs text-fg-muted text-right">
                    {{ bpUsedPct() | number:'1.0-0' }}% đã dùng
                  </p>
                </div>
              }
              @if (!riskSvc.buyingPower()) {
                <p class="text-fg-muted text-small">Không có dữ liệu</p>
              }
            }
          </div>
        </app-card>

        <!-- Rtt Gauge -->
        <app-card variant="elevated" padding="md">
          <div class="space-y-2 flex flex-col items-center">
            <p class="text-small text-fg-muted uppercase tracking-wider self-start">📊 Tỷ lệ TK (Rtt)</p>
            @if (loading()) {
              <app-skeleton height="80px" width="160px" />
            }
            @if (!loading()) {
              @if (riskSvc.rtt(); as rttData) {
                <!-- Gauge -->
                <div class="gauge-wrap">
                  <div class="gauge-bg"></div>
                  <div class="gauge-needle"
                    [style.transform]="'translateX(-50%) rotate(' + needleDeg() + 'deg)'"
                    [style.--needle-color]="needleColor()">
                  </div>
                  <div class="gauge-cover"></div>
                </div>
                @if (rttData.loanAmount === 0) {
                  <span class="status-chip chip-safe">✅ Không có nợ</span>
                  <p class="text-xs text-fg-muted">Chưa sử dụng margin</p>
                } @else {
                  <p class="text-title font-bold font-numeric" [style.color]="needleColor()">
                    {{ rttData.rtt | percent:'1.1-1' }}
                  </p>
                  <span class="status-chip" [class]="chipClass()">
                    {{ riskSvc.getRttStatusLabel(rttData.status) }}
                  </span>
                  <p class="text-xs text-fg-muted">
                    Dư nợ: {{ rttData.loanAmount | number:'1.0-0' }} ₫
                  </p>
                }
              }
            }
          </div>
        </app-card>

        <!-- Alert Summary -->
        <app-card variant="elevated" padding="md">
          <div class="space-y-3">
            <p class="text-small text-fg-muted uppercase tracking-wider">🔔 Cảnh báo</p>
            @if (loading()) {
              <app-skeleton height="1.5rem" />
              <app-skeleton height="1rem" width="60%" />
            } @else {
              <div class="flex items-end gap-2">
                <span class="text-headline font-bold text-fg">
                  {{ riskSvc.alerts().length }}
                </span>
                <span class="text-small text-fg-muted mb-1">cảnh báo</span>
              </div>
              <div class="space-y-1.5">
                <div class="flex justify-between text-xs">
                  <span class="text-fg-muted">Force Sell</span>
                  <span class="font-numeric text-down font-semibold">
                    {{ forceSellCount() }}
                  </span>
                </div>
                <div class="flex justify-between text-xs">
                  <span class="text-fg-muted">Call Margin</span>
                  <span class="font-numeric text-warn font-semibold">
                    {{ callMarginCount() }}
                  </span>
                </div>
              </div>
            }
          </div>
        </app-card>
      </div>

      <!-- ── Active Warning Banner ── -->
      @if (!loading() && riskSvc.rtt()?.isAtRisk) {
        <div
          class="rounded-xl p-4 flex items-start gap-3 border"
          [class]="riskSvc.rtt()?.status === 'FORCE_SELL_ZONE'
            ? 'bg-red-950/20 border-red-500/40'
            : 'bg-amber-950/20 border-amber-500/40'"
        >
          <span class="text-2xl flex-shrink-0">
            {{ riskSvc.rtt()?.status === 'FORCE_SELL_ZONE' ? '🚨' : '⚠️' }}
          </span>
          <div>
            @if (riskSvc.rtt()?.status === 'FORCE_SELL_ZONE') {
              <p class="font-semibold text-red-400">Force Sell đã được kích hoạt</p>
              <p class="text-sm text-red-300/80 mt-0.5">
                Tỷ lệ tài khoản {{ riskSvc.rtt()?.rtt | percent:'1.1-1' }} xuống dưới ngưỡng 80%.
                Hệ thống đang tự động bán các vị thế để thu hồi nợ. Vui lòng kiểm tra giao dịch.
              </p>
            } @else {
              <p class="font-semibold text-amber-400">Yêu cầu nộp thêm tiền ký quỹ (Call Margin)</p>
              <p class="text-sm text-amber-300/80 mt-0.5">
                Tỷ lệ tài khoản {{ riskSvc.rtt()?.rtt | percent:'1.1-1' }} dưới ngưỡng 85%.
                Vui lòng nộp tiền hoặc bán bớt cổ phiếu để đưa Rtt về an toàn.
              </p>
            }
            <div class="flex gap-2 mt-3">
              <a routerLink="/deposit">
                <app-btn size="sm" variant="primary" label="Nạp tiền ngay" />
              </a>
              <a routerLink="/portfolio">
                <app-btn size="sm" variant="secondary" label="Xem danh mục" />
              </a>
            </div>
          </div>
        </div>
      }

      <!-- ── Alerts History + Tab ── -->
      <app-card title="Lịch sử cảnh báo rủi ro" variant="default" [hasHeaderAction]="true">
        <ng-container slot="header-action">
          <app-tab-nav [tabs]="alertTabs" [activeId]="activeAlertTab()"
            variant="pills" (activeIdChange)="onAlertTabChange($event)" />
        </ng-container>

        @if (loading()) {
          <div class="space-y-3 mt-2">
            @for (i of [1,2,3]; track i) {
              <app-skeleton height="56px" />
            }
          </div>
        } @else if (filteredAlerts().length === 0) {
          <div class="flex flex-col items-center justify-center py-12 text-center">
            <span class="text-4xl mb-3">✅</span>
            <p class="text-fg font-medium">Không có cảnh báo</p>
            <p class="text-fg-muted text-small mt-1">Tài khoản đang trong trạng thái an toàn.</p>
          </div>
        } @else {
          <div class="space-y-2 mt-2">
            @for (alert of filteredAlerts(); track alert.alertId) {
              <div class="alert-row" [class]="alert.alertType">
                <span class="tl-dot" [class]="alert.alertType"></span>
                <div class="flex-1 min-w-0">
                  <div class="flex items-center gap-2 flex-wrap">
                    <span class="status-chip text-xs px-2 py-0.5 rounded-full"
                      [class]="alert.alertType === 'FORCE_SELL' ? 'chip-danger' : 'chip-warn'">
                      {{ alert.alertType === 'FORCE_SELL' ? '🚨 Force Sell' : '⚠️ Call Margin' }}
                    </span>
                    <span class="text-xs text-fg-muted font-numeric">
                      Rtt = {{ alert.rtt | percent:'1.1-1' }}
                    </span>
                  </div>
                  <p class="text-small text-fg mt-1 leading-relaxed">{{ alert.message }}</p>
                </div>
                <span class="text-xs text-fg-muted white-space-nowrap flex-shrink-0">
                  {{ alert.createdAt | date:'dd/MM HH:mm' }}
                </span>
              </div>
            }
          </div>
        }
      </app-card>

      <!-- ── Margin Ratios Info ── -->
      <app-card title="Tỷ lệ ký quỹ (Margin Ratios)" variant="default">
        <p class="text-small text-fg-muted mb-3">
          Sức mua = Tiền mặt + (Giá thị trường × Số lượng × <span class="text-fg font-semibold">Tỷ lệ ban đầu</span>)
        </p>
        <div class="overflow-x-auto">
          <table class="w-full text-sm">
            <thead>
              <tr class="border-b border-border text-fg-muted text-xs text-left">
                <th class="pb-2 font-medium">Mã CK</th>
                <th class="pb-2 font-medium text-right">Tỷ lệ ban đầu</th>
                <th class="pb-2 font-medium text-right">Tỷ lệ duy trì</th>
                <th class="pb-2 font-medium text-right">Trạng thái</th>
              </tr>
            </thead>
            <tbody>
              @for (r of riskSvc.marginRatios(); track r.symbol) {
                <tr class="border-b border-border/40 hover:bg-surface-2 transition-colors">
                  <td class="py-2.5 font-semibold text-fg">{{ r.symbol }}</td>
                  <td class="py-2.5 text-right font-numeric text-up">{{ r.initialRate }}%</td>
                  <td class="py-2.5 text-right font-numeric text-warn">{{ r.maintenanceRate }}%</td>
                  <td class="py-2.5 text-right">
                    <span class="status-chip chip-safe text-xs">Hiệu lực</span>
                  </td>
                </tr>
              }
            </tbody>
          </table>
        </div>
        <p class="text-xs text-fg-muted mt-3">
          * Penny stocks (dưới 5,000 ₫) không được hỗ trợ margin (tỷ lệ 0%).
        </p>
      </app-card>

    </div>
  `,
})
export class RiskComponent implements OnInit {
  public readonly riskSvc = inject(RiskService);
  readonly loading = signal(true);
  readonly activeAlertTab = signal<string>('all');

  readonly alertTabs: TabItem[] = [
    { id: 'all', label: 'Tất cả' },
    { id: 'CALL_MARGIN', label: 'Call Margin' },
    { id: 'FORCE_SELL', label: 'Force Sell' },
  ];

  // Buying-power used % (based on available cash vs total buying power)
  readonly bpUsedPct = computed(() => {
    const bp = this.riskSvc.buyingPower();
    if (!bp || bp.buyingPower <= 0) return 0;
    const used = bp.buyingPower - bp.availableCash;
    return Math.min(100, Math.max(0, (used / bp.buyingPower) * 100));
  });


  readonly needleDeg = computed(() => {
    const rtt = this.riskSvc.rtt();
    if (!rtt || rtt.loanAmount === 0) return 90;
    const clamped = Math.min(1.5, Math.max(0, rtt.rtt));
    return -90 + (clamped / 1.5) * 180;
  });

  readonly needleColor = computed(() => {
    const rtt = this.riskSvc.rtt();
    if (!rtt || rtt.loanAmount === 0) return '#22c55e';
    if (rtt.rtt < 0.80) return '#ef4444';
    if (rtt.rtt < 0.85) return '#f59e0b';
    return '#22c55e';
  });

  readonly chipClass = computed(() => {
    const status = this.riskSvc.rtt()?.status;
    if (status === 'FORCE_SELL_ZONE') return 'status-chip chip-danger';
    if (status === 'CALL_MARGIN_ZONE') return 'status-chip chip-warn';
    if (status === 'SAFE') return 'status-chip chip-safe';
    return 'status-chip chip-neutral';
  });

  readonly forceSellCount = computed(() =>
    this.riskSvc.alerts().filter(a => a.alertType === 'FORCE_SELL').length
  );

  readonly callMarginCount = computed(() =>
    this.riskSvc.alerts().filter(a => a.alertType === 'CALL_MARGIN').length
  );

  readonly filteredAlerts = computed(() => {
    const tab = this.activeAlertTab();
    const all = this.riskSvc.alerts();
    if (tab === 'all') return all;
    return all.filter(a => a.alertType === tab as 'CALL_MARGIN' | 'FORCE_SELL');
  });

  onAlertTabChange(tab: string): void {
    this.activeAlertTab.set(tab);
  }

  ngOnInit(): void {
    this.refresh();
  }

  refresh(): void {
    this.loading.set(true);
    let done = 0;
    const tryDone = () => { if (++done >= 4) this.loading.set(false); };

    this.riskSvc.loadBuyingPower().subscribe({ next: tryDone, error: tryDone });
    this.riskSvc.loadRtt().subscribe({ next: tryDone, error: tryDone });
    this.riskSvc.loadAlerts(50).subscribe({ next: tryDone, error: tryDone });
    this.riskSvc.loadMarginRatios().subscribe({ next: tryDone, error: tryDone });
  }
}
