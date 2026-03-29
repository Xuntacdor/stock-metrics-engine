import {
  Component, signal, inject, OnInit, OnDestroy,
  ChangeDetectionStrategy,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { CardComponent } from '../../shared/molecules/card/card.component';
import { TabNavComponent, type TabItem } from '../../shared/molecules/tab-nav/tab-nav.component';
import { FormFieldComponent } from '../../shared/molecules/form-field/form-field.component';
import { BadgeComponent } from '../../shared/atoms/badge/badge.component';
import { ButtonComponent } from '../../shared/atoms/button/button.component';
import { InputComponent } from '../../shared/atoms/input/input.component';
import { LabelComponent } from '../../shared/atoms/label/label.component';
import { IconComponent } from '../../shared/atoms/icon/icon.component';
import { AlertService, type AlertRule, type AlertTriggeredNotification } from '../../core/services/alert.service';

@Component({
  selector: 'app-alerts',
  standalone: true,
  imports: [CommonModule, FormsModule, CardComponent, TabNavComponent, FormFieldComponent, BadgeComponent, ButtonComponent, InputComponent, LabelComponent, IconComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="p-4 md:p-6 space-y-6 animate-fade-in"
      role="region" aria-label="Quản lý cảnh báo">

      <!-- Header -->
      <div>
        <h1 class="text-headline font-bold text-fg">Cảnh báo</h1>
        <p class="text-small text-fg-muted mt-0.5">Thiết lập cảnh báo giá, RSI, và khối lượng</p>
      </div>

      <!-- ARIA live region for real-time alerts -->
      <div role="status" aria-live="polite" aria-atomic="true" class="sr-only">
        {{ latestAlert() }}
      </div>

      <!-- Toast notification when alert fires -->
      @if (toastMessage()) {
        <div class="fixed top-4 right-4 z-50 flex items-center gap-3 px-4 py-3 rounded-xl border border-down/40 bg-down/10 shadow-lg text-down text-small font-medium animate-fade-in">
          <app-icon name="bell" size="sm" />
          {{ toastMessage() }}
        </div>
      }

      <div class="grid grid-cols-1 xl:grid-cols-3 gap-6">

        <!-- Create Alert Form (1/3) -->
        <div class="xl:col-span-1">
          <app-card title="Tạo cảnh báo mới" variant="elevated">
            <div class="space-y-4 mt-2">

              <!-- Symbol -->
              <app-form-field label="Mã cổ phiếu" fieldId="alert-sym" [required]="true">
                <app-input inputId="alert-sym" placeholder="VD: VNM, FPT, HPG..."
                  [(value)]="newAlert.symbol" />
              </app-form-field>

              <!-- Trigger type -->
              <div>
                <p class="text-small font-medium text-fg mb-2">Loại cảnh báo</p>
                <div class="grid grid-cols-2 gap-2">
                  @for (t of alertTypes; track t.id) {
                    <button
                      [class]="'flex flex-col items-center gap-1.5 p-3 rounded-lg border text-center transition-all ' +
                        (newAlert.type === t.id ? 'border-up bg-up/10 text-up' : 'border-border hover:border-border-hover text-fg-muted hover:text-fg')"
                      (click)="setAlertType(t.id)"
                    >
                      <app-icon [name]="t.icon" size="md" />
                      <span class="text-xs font-medium">{{ t.label }}</span>
                    </button>
                  }
                </div>
              </div>

              <!-- Condition -->
              <div class="grid grid-cols-2 gap-3">
                <div>
                  <label class="text-small font-medium text-fg block mb-1.5">Điều kiện</label>
                  <select class="w-full h-10 px-3 text-body bg-surface-2 border border-border rounded-md text-fg focus:outline-none focus:border-up"
                    [(ngModel)]="newAlert.condition" [ngModelOptions]="{standalone: true}">
                    <option value="gt">&gt; Lớn hơn</option>
                    <option value="gte">&gt;= Lớn hơn hoặc bằng</option>
                    <option value="lt">&lt; Nhỏ hơn</option>
                    <option value="lte">&lt;= Nhỏ hơn hoặc bằng</option>
                  </select>
                </div>
                <app-form-field label="Giá trị" fieldId="alert-val">
                  <app-input type="number" inputId="alert-val" placeholder="65.5"
                    [(value)]="newAlert.value" />
                </app-form-field>
              </div>

              <!-- Frequency -->
              <div>
                <p class="text-small font-medium text-fg mb-2">Tần suất thông báo</p>
                <div class="space-y-1.5">
                  <label class="flex items-center gap-2 cursor-pointer text-small">
                    <input type="radio" name="freq" value="once"
                      [checked]="newAlert.notifyOnce" (change)="newAlert.notifyOnce = true" class="accent-up" />
                    <span>Một lần (tự động tắt sau khi kích hoạt)</span>
                  </label>
                  <label class="flex items-center gap-2 cursor-pointer text-small">
                    <input type="radio" name="freq" value="always"
                      [checked]="!newAlert.notifyOnce" (change)="newAlert.notifyOnce = false" class="accent-up" />
                    <span>Mỗi lần điều kiện thỏa mãn</span>
                  </label>
                </div>
              </div>

              <app-btn variant="primary" size="md" label="Thêm cảnh báo" [fullWidth]="true"
                [loading]="isSaving()" (clicked)="saveAlert()" />
            </div>
          </app-card>
        </div>

        <!-- Alert List (2/3) -->
        <div class="xl:col-span-2 space-y-4">

          <!-- Tabs -->
          <app-tab-nav
            [tabs]="alertTabs()"
            [activeId]="activeTab()"
            variant="underline"
            (activeIdChange)="activeTab.set($event)"
          />

          <!-- Loading -->
          @if (isLoading()) {
            <div class="flex items-center justify-center py-12 text-fg-muted">
              <span class="text-small">Đang tải...</span>
            </div>
          }

          <!-- Active alerts -->
          @if (!isLoading() && activeTab() === 'active') {
            <div class="space-y-3">
              @for (alert of activeAlerts(); track alert.alertId) {
                <div [class]="'rounded-xl border p-4 transition-all ' +
                  (alert.isTriggered ? 'border-down/40 bg-down/5' : 'border-border hover:border-border-hover')"
                >
                  <div class="flex items-start justify-between gap-4">
                    <div class="flex items-start gap-3">
                      <!-- Type icon -->
                      <div [class]="'p-2 rounded-lg shrink-0 ' + alertIconBg(alert.alertType)">
                        <app-icon [name]="alertIcon(alert.alertType)" size="sm" [class]="alertIconColor(alert.alertType)" />
                      </div>
                      <!-- Info -->
                      <div>
                        <div class="flex items-center gap-2 mb-0.5">
                          <span class="font-bold text-fg">{{ alert.symbol }}</span>
                          <app-badge [variant]="alert.isTriggered ? 'down' : 'neutral'" size="sm" [dot]="alert.isTriggered">
                            {{ alert.isTriggered ? 'Đã kích hoạt' : 'Đang chờ' }}
                          </app-badge>
                        </div>
                        <p class="text-small text-fg-muted">{{ conditionLabel(alert.condition) }} {{ alert.thresholdValue }}</p>
                        <p class="text-xs text-fg-muted mt-0.5">{{ alert.createdAt | date:'dd/MM HH:mm' }}</p>
                      </div>
                    </div>

                    <!-- Controls -->
                    <div class="flex items-center gap-2 shrink-0">
                      <!-- Toggle -->
                      <button
                        [class]="'w-10 h-6 rounded-full transition-colors relative ' +
                          (alert.isActive ? 'bg-up' : 'bg-border')"
                        (click)="toggleAlert(alert)"
                        [attr.aria-label]="alert.isActive ? 'Tắt cảnh báo' : 'Bật cảnh báo'"
                        [attr.aria-checked]="alert.isActive"
                        role="switch"
                      >
                        <span [class]="'absolute top-1 w-4 h-4 rounded-full bg-white shadow transition-all ' +
                          (alert.isActive ? 'left-5' : 'left-1')"></span>
                      </button>
                      <button
                        class="p-1.5 rounded-md text-fg-muted hover:text-down hover:bg-down/10 transition-colors"
                        aria-label="Xóa cảnh báo"
                        (click)="deleteAlert(alert.alertId)"
                      >
                        <app-icon name="x" size="sm" />
                      </button>
                    </div>
                  </div>
                </div>
              }

              @if (activeAlerts().length === 0) {
                <div class="flex flex-col items-center py-12 gap-3 text-fg-muted">
                  <app-icon name="bell" size="xl" />
                  <p class="text-small">Chưa có cảnh báo nào. Tạo cảnh báo đầu tiên →</p>
                </div>
              }
            </div>
          }

          <!-- Triggered history -->
          @if (!isLoading() && activeTab() === 'history') {
            <div class="space-y-2">
              @for (h of triggeredAlerts(); track h.alertId) {
                <div class="flex items-center gap-4 p-4 rounded-xl border border-border hover:bg-surface-2 transition-colors">
                  <div class="w-2 h-2 rounded-full shrink-0 bg-down"></div>
                  <div class="flex-1 min-w-0">
                    <p class="text-small text-fg font-medium">{{ h.symbol }} – {{ conditionLabel(h.condition) }} {{ h.thresholdValue }}</p>
                    <p class="text-xs text-fg-muted">{{ h.triggeredAt | date:'dd/MM/yy HH:mm' }}</p>
                  </div>
                  <app-badge variant="down" size="sm">Kích hoạt</app-badge>
                </div>
              }
              @if (triggeredAlerts().length === 0) {
                <p class="text-center py-8 text-small text-fg-muted">Chưa có cảnh báo nào được kích hoạt.</p>
              }
            </div>
          }
        </div>
      </div>
    </div>
  `,
})
export class AlertsComponent implements OnInit, OnDestroy {
  private readonly alertService = inject(AlertService);

  readonly activeTab = signal('active');
  readonly isSaving  = signal(false);
  readonly isLoading = signal(false);
  readonly latestAlert = signal('');
  readonly toastMessage = signal('');

  private readonly _alerts = signal<AlertRule[]>([]);

  readonly activeAlerts   = () => this._alerts().filter(a => a.isActive && !a.isTriggered);
  readonly triggeredAlerts = () => this._alerts().filter(a => a.isTriggered);
  readonly alertTabs = () => [
    { id: 'active',  label: 'Đang hoạt động', badge: this.activeAlerts().length || undefined },
    { id: 'history', label: 'Đã kích hoạt',   badge: this.triggeredAlerts().length || undefined },
  ] satisfies TabItem[];

  newAlert = {
    symbol: '' as string | number,
    type: 'price' as 'price' | 'volume' | 'rsi' | 'news',
    condition: 'gt' as 'gt' | 'gte' | 'lt' | 'lte',
    value: '' as string | number,
    notifyOnce: true,
  };

  readonly alertTypes = [
    { id: 'price',  label: 'Giá',       icon: 'trending-up' },
    { id: 'rsi',    label: 'RSI',        icon: 'bar-chart-2' },
    { id: 'volume', label: 'Khối lượng', icon: 'bar-chart-2' },
    { id: 'news',   label: 'Tin tức',    icon: 'bell' },
  ];

  ngOnInit(): void {
    this.loadAlerts();

    // Subscribe to SignalR push notifications for this user's alerts
    this.alertService.onAlertTriggered((n: AlertTriggeredNotification) => {
      this.showToast(`[${n.symbol}] ${n.alertType} ${this.conditionLabel(n.condition)} ${n.thresholdValue} — giá trị hiện tại: ${n.currentValue}`);
      this.latestAlert.set(`Cảnh báo kích hoạt: ${n.symbol}`);
      // Mark the alert as triggered in the local list
      this._alerts.update(list =>
        list.map(a => a.alertId === n.alertId ? { ...a, isTriggered: true, triggeredAt: n.triggeredAt } : a)
      );
    });
  }

  ngOnDestroy(): void {
    this.alertService.offAlertTriggered();
  }

  private loadAlerts(): void {
    this.isLoading.set(true);
    this.alertService.getMyAlerts().subscribe({
      next: alerts => { this._alerts.set(alerts); this.isLoading.set(false); },
      error: () => this.isLoading.set(false),
    });
  }

  saveAlert(): void {
    if (!this.newAlert.symbol || !this.newAlert.value) return;
    this.isSaving.set(true);

    this.alertService.createAlert({
      symbol:         String(this.newAlert.symbol).toUpperCase(),
      alertType:      this.newAlert.type,
      condition:      this.newAlert.condition,
      thresholdValue: Number(this.newAlert.value),
      notifyOnce:     this.newAlert.notifyOnce,
    }).subscribe({
      next: alert => {
        this._alerts.update(list => [alert, ...list]);
        this.isSaving.set(false);
        this.newAlert = { symbol: '', type: 'price', condition: 'gt', value: '', notifyOnce: true };
        this.latestAlert.set(`Cảnh báo mới đã được tạo cho ${alert.symbol}`);
      },
      error: () => this.isSaving.set(false),
    });
  }

  toggleAlert(alert: AlertRule): void {
    this.alertService.toggleAlert(alert.alertId, !alert.isActive).subscribe(updated => {
      this._alerts.update(list => list.map(a => a.alertId === updated.alertId ? updated : a));
    });
  }

  deleteAlert(alertId: number): void {
    this.alertService.deleteAlert(alertId).subscribe(() => {
      this._alerts.update(list => list.filter(a => a.alertId !== alertId));
    });
  }

  setAlertType(id: string): void {
    const valid = ['price', 'volume', 'rsi', 'news'] as const;
    if (valid.includes(id as typeof valid[number])) {
      this.newAlert.type = id as typeof valid[number];
    }
  }

  conditionLabel(condition: string): string {
    return { gt: '>', gte: '>=', lt: '<', lte: '<=' }[condition] ?? condition;
  }

  alertIcon(type: string): string {
    return { price: 'trending-up', rsi: 'bar-chart-2', volume: 'bar-chart-2', news: 'bell' }[type] ?? 'bell';
  }
  alertIconBg(type: string): string {
    return { price: 'bg-up/10', rsi: 'bg-reference/10', volume: 'bg-limit-up/10', news: 'bg-info/10' }[type] ?? 'bg-surface-2';
  }
  alertIconColor(type: string): string {
    return { price: 'text-up', rsi: 'text-reference', volume: 'text-limit-up', news: 'text-sky-400' }[type] ?? 'text-fg-muted';
  }

  private showToast(msg: string): void {
    this.toastMessage.set(msg);
    setTimeout(() => this.toastMessage.set(''), 5000);
  }
}
