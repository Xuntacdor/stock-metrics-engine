import {
  Component, signal,
  ChangeDetectionStrategy,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { CardComponent } from '../../shared/molecules/card/card.component';
import { TabNavComponent, type TabItem } from '../../shared/molecules/tab-nav/tab-nav.component';
import { FormFieldComponent } from '../../shared/molecules/form-field/form-field.component';
import { BadgeComponent } from '../../shared/atoms/badge/badge.component';
import { ButtonComponent } from '../../shared/atoms/button/button.component';
import { InputComponent } from '../../shared/atoms/input/input.component';
import { LabelComponent } from '../../shared/atoms/label/label.component';
import { IconComponent } from '../../shared/atoms/icon/icon.component';

interface AlertRule {
  id: number;
  symbol: string;
  type: 'price' | 'rsi' | 'volume' | 'news';
  condition: string;
  value: number;
  active: boolean;
  triggered: boolean;
  createdAt: string;
}

@Component({
  selector: 'app-alerts',
  standalone: true,
  imports: [CommonModule, CardComponent, TabNavComponent, FormFieldComponent, BadgeComponent, ButtonComponent, InputComponent, LabelComponent, IconComponent],
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
                  <select class="w-full h-10 px-3 text-body bg-surface-2 border border-border rounded-md text-fg focus:outline-none focus:border-up">
                    <option>&gt; Lớn hơn</option>
                    <option>&lt; Nhỏ hơn</option>
                    <option>= Bằng</option>
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
                  @for (f of frequencies; track f.id) {
                    <label class="flex items-center gap-2 cursor-pointer text-small">
                      <input type="radio" name="freq" [value]="f.id"
                        [checked]="newAlert.frequency === f.id"
                        (change)="newAlert.frequency = f.id" class="accent-up" />
                      <span>{{ f.label }}</span>
                    </label>
                  }
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
            [tabs]="alertTabs"
            [activeId]="activeTab()"
            variant="underline"
            (activeIdChange)="activeTab.set($event)"
          />

          <!-- Active alerts -->
          @if (activeTab() === 'active') {
            <div class="space-y-3">
              @for (alert of activeAlerts(); track alert.id) {
                <div [class]="'rounded-xl border p-4 transition-all ' +
                  (alert.triggered ? 'border-down/40 bg-down/5' : 'border-border hover:border-border-hover')"
                >
                  <div class="flex items-start justify-between gap-4">
                    <div class="flex items-start gap-3">
                      <!-- Type icon -->
                      <div [class]="'p-2 rounded-lg shrink-0 ' + alertIconBg(alert.type)">
                        <app-icon [name]="alertIcon(alert.type)" size="sm" [class]="alertIconColor(alert.type)" />
                      </div>
                      <!-- Info -->
                      <div>
                        <div class="flex items-center gap-2 mb-0.5">
                          <span class="font-bold text-fg">{{ alert.symbol }}</span>
                          <app-badge [variant]="alert.triggered ? 'down' : 'neutral'" size="sm" [dot]="alert.triggered">
                            {{ alert.triggered ? 'Đã kích hoạt' : 'Đang chờ' }}
                          </app-badge>
                        </div>
                        <p class="text-small text-fg-muted">{{ alert.condition }} {{ alert.value }}</p>
                        <p class="text-xs text-fg-muted mt-0.5">{{ alert.createdAt }}</p>
                      </div>
                    </div>

                    <!-- Controls -->
                    <div class="flex items-center gap-2 shrink-0">
                      <!-- Toggle -->
                      <button
                        [class]="'w-10 h-6 rounded-full transition-colors relative ' +
                          (alert.active ? 'bg-up' : 'bg-border')"
                        (click)="toggleAlert(alert)"
                        [attr.aria-label]="alert.active ? 'Tắt cảnh báo' : 'Bật cảnh báo'"
                        [attr.aria-checked]="alert.active"
                        role="switch"
                      >
                        <span [class]="'absolute top-1 w-4 h-4 rounded-full bg-white shadow transition-all ' +
                          (alert.active ? 'left-5' : 'left-1')"></span>
                      </button>
                      <button
                        class="p-1.5 rounded-md text-fg-muted hover:text-down hover:bg-down/10 transition-colors"
                        aria-label="Xóa cảnh báo"
                        (click)="deleteAlert(alert.id)"
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

          <!-- History -->
          @if (activeTab() === 'history') {
            <div class="space-y-2">
              @for (h of alertHistory; track h.id) {
                <div class="flex items-center gap-4 p-4 rounded-xl border border-border hover:bg-surface-2 transition-colors">
                  <div [class]="'w-2 h-2 rounded-full shrink-0 ' + (h.triggered ? 'bg-down' : 'bg-fg-muted')"></div>
                  <div class="flex-1 min-w-0">
                    <p class="text-small text-fg font-medium">{{ h.symbol }} – {{ h.condition }}</p>
                    <p class="text-xs text-fg-muted">{{ h.time }}</p>
                  </div>
                  <app-badge [variant]="h.triggered ? 'down' : 'neutral'" size="sm">
                    {{ h.triggered ? 'Kích hoạt' : 'Hết hạn' }}
                  </app-badge>
                </div>
              }
            </div>
          }
        </div>
      </div>
    </div>
  `,
})
export class AlertsComponent {
  readonly activeTab = signal('active');
  readonly isSaving = signal(false);
  readonly latestAlert = signal('');

  newAlert = { symbol: '' as string | number, type: 'price' as 'price' | 'rsi' | 'volume' | 'news', value: '' as string | number, frequency: 'realtime' };

  readonly alertTabs: TabItem[] = [
    { id: 'active', label: 'Đang hoạt động', badge: 3 },
    { id: 'history', label: 'Lịch sử' },
  ];

  readonly alertTypes = [
    { id: 'price', label: 'Giá', icon: 'trending-up' },
    { id: 'rsi', label: 'RSI', icon: 'bar-chart-2' },
    { id: 'volume', label: 'Khối lượng', icon: 'bar-chart-2' },
    { id: 'news', label: 'Tin tức', icon: 'bell' },
  ];

  readonly frequencies = [
    { id: 'realtime', label: 'Ngay lập tức (Real-time)' },
    { id: '1min', label: 'Mỗi 1 phút' },
    { id: 'daily', label: 'Tổng hợp cuối ngày' },
  ];

  private _alerts = signal<AlertRule[]>([
    { id: 1, symbol: 'VNM', type: 'price', condition: 'Giá > ', value: 67.0, active: true, triggered: false, createdAt: '06/03 09:15' },
    { id: 2, symbol: 'FPT', type: 'rsi', condition: 'RSI < ', value: 30, active: true, triggered: true, createdAt: '06/03 08:30' },
    { id: 3, symbol: 'HPG', type: 'price', condition: 'Giá < ', value: 26.0, active: false, triggered: false, createdAt: '05/03 14:22' },
  ]);

  readonly activeAlerts = () => this._alerts();

  readonly alertHistory = [
    { id: 1, symbol: 'VNM', condition: 'Giá > 65.5', time: 'Hôm nay 10:42', triggered: true },
    { id: 2, symbol: 'FPT', condition: 'RSI < 30', time: 'Hôm nay 09:18', triggered: true },
    { id: 3, symbol: 'TCB', condition: 'Giá > 25.0', time: 'Hôm qua 15:30', triggered: false },
  ];

  alertIcon(type: string): string {
    return { price: 'trending-up', rsi: 'bar-chart-2', volume: 'bar-chart-2', news: 'bell' }[type] ?? 'bell';
  }
  alertIconBg(type: string): string {
    return { price: 'bg-up/10', rsi: 'bg-reference/10', volume: 'bg-limit-up/10', news: 'bg-info/10' }[type] ?? 'bg-surface-2';
  }
  alertIconColor(type: string): string {
    return { price: 'text-up', rsi: 'text-reference', volume: 'text-limit-up', news: 'text-sky-400' }[type] ?? 'text-fg-muted';
  }

  toggleAlert(alert: AlertRule): void {
    this._alerts.update(list => list.map(a => a.id === alert.id ? { ...a, active: !a.active } : a));
  }

  deleteAlert(id: number): void {
    this._alerts.update(list => list.filter(a => a.id !== id));
  }

  saveAlert(): void {
    if (!this.newAlert.symbol) return;
    this.isSaving.set(true);
    setTimeout(() => {
      this._alerts.update(list => [...list, {
        id: Date.now(), symbol: this.newAlert.symbol as string,
        type: this.newAlert.type, condition: 'Giá > ',
        value: Number(this.newAlert.value) || 0,
        active: true, triggered: false,
        createdAt: new Date().toLocaleDateString('vi'),
      }]);
      this.isSaving.set(false);
      this.newAlert = { symbol: '', type: 'price', value: '', frequency: 'realtime' };
      this.latestAlert.set(`Cảnh báo mới đã được tạo`);
    }, 800);
  }

  setAlertType(id: string): void {
    const valid: AlertRule['type'][] = ['price', 'rsi', 'volume', 'news'];
    if (valid.includes(id as AlertRule['type'])) {
      this.newAlert.type = id as AlertRule['type'];
    }
  }
}
