import {
  Component, signal, inject,
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
import { ThemeService } from '../../core/services/theme.service';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-settings',
  standalone: true,
  imports: [CommonModule, CardComponent, TabNavComponent, FormFieldComponent, BadgeComponent, ButtonComponent, InputComponent, LabelComponent, IconComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="p-4 md:p-6 space-y-6 animate-fade-in">
      <div>
        <h1 class="text-headline font-bold text-fg">Cài đặt tài khoản</h1>
        <p class="text-small text-fg-muted mt-0.5">Quản lý thông tin cá nhân, bảo mật và tùy chỉnh</p>
      </div>

      <div class="grid grid-cols-1 xl:grid-cols-4 gap-6">

        <!-- Settings nav (sidebar) -->
        <div class="xl:col-span-1">
          <app-card variant="elevated" padding="sm">
            <nav class="space-y-0.5">
              @for (item of settingsNav; track item.id) {
                <button
                  [class]="'w-full flex items-center gap-3 px-3 py-2.5 rounded-lg text-small font-medium transition-all ' +
                    (activeSection() === item.id ? 'bg-up/10 text-up' : 'text-fg-muted hover:bg-surface-2 hover:text-fg')"
                  (click)="activeSection.set(item.id)"
                >
                  <app-icon [name]="item.icon" size="sm" class="shrink-0" />
                  {{ item.label }}
                </button>
              }
            </nav>
          </app-card>
        </div>

        <!-- Settings content -->
        <div class="xl:col-span-3 space-y-6">

          <!-- Profile -->
          @if (activeSection() === 'profile') {
            <app-card title="Thông tin cá nhân" variant="elevated" [hasHeaderAction]="true">
              <ng-container slot="header-action">
                <app-badge variant="up" size="sm">Đã xác minh KYC</app-badge>
              </ng-container>
              <div class="space-y-5">
                <!-- Avatar -->
                <div class="flex items-center gap-4">
                  <div class="w-16 h-16 rounded-full bg-up/20 flex items-center justify-center">
                    <app-icon name="user" size="xl" class="text-up" />
                  </div>
                  <div>
                    <app-btn variant="outline" size="sm" label="Đổi ảnh đại diện" />
                    <p class="text-xs text-fg-muted mt-1">JPG, PNG · tối đa 2MB</p>
                  </div>
                </div>
                <!-- Fields -->
                <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
                  <app-form-field label="Họ và tên" fieldId="pf-name">
                    <app-input inputId="pf-name" [(value)]="profile.name" />
                  </app-form-field>
                  <app-form-field label="Số điện thoại" fieldId="pf-phone">
                    <app-input inputId="pf-phone" [(value)]="profile.phone" [state]="'success'" />
                  </app-form-field>
                  <app-form-field label="Email" fieldId="pf-email" [optional]="true">
                    <app-input type="email" inputId="pf-email" [(value)]="profile.email" [state]="'success'" />
                  </app-form-field>
                  <app-form-field label="CCCD/CMND" fieldId="pf-cccd">
                    <app-input inputId="pf-cccd" [(value)]="profile.cccd" [state]="'disabled'" />
                  </app-form-field>
                </div>
                <div class="flex items-center gap-3">
                  <app-btn variant="primary" size="md" label="Lưu thay đổi" [loading]="isSaving()" (clicked)="save()" />
                  @if (saveSuccess()) {
                    <span class="text-sm text-up animate-fade-in">✅ Đã lưu thành công!</span>
                  }
                </div>
              </div>
            </app-card>
          }

          <!-- Security -->
          @if (activeSection() === 'security') {
            <div class="space-y-4">
              <app-card title="Đổi mật khẩu" variant="elevated">
                <div class="space-y-4">
                  <app-form-field label="Mật khẩu hiện tại" fieldId="sec-old">
                    <app-input type="password" inputId="sec-old" placeholder="••••••••" [(value)]="sec.oldPass" />
                  </app-form-field>
                  <app-form-field label="Mật khẩu mới" fieldId="sec-new"
                    hintMessage="Ít nhất 8 ký tự, bao gồm chữ hoa và số">
                    <app-input type="password" inputId="sec-new" placeholder="••••••••" [(value)]="sec.newPass" />
                  </app-form-field>
                  <app-form-field label="Xác nhận mật khẩu" fieldId="sec-confirm">
                    <app-input type="password" inputId="sec-confirm" placeholder="••••••••" [(value)]="sec.confirmPass"
                      [state]="sec.confirmPass && sec.newPass !== sec.confirmPass ? 'error' : 'default'" />
                  </app-form-field>
                  <app-btn variant="primary" size="md" label="Đổi mật khẩu" [loading]="isSaving()" (clicked)="save()" />
                </div>
              </app-card>

              <app-card title="Xác thực 2 bước (2FA)" variant="default" [hasHeaderAction]="true">
                <ng-container slot="header-action">
                  <app-badge [variant]="twoFaEnabled() ? 'up' : 'neutral'" size="sm">
                    {{ twoFaEnabled() ? 'Đã bật' : 'Chưa bật' }}
                  </app-badge>
                </ng-container>
                <p class="text-small text-fg-muted my-3">
                  Bảo vệ tài khoản bằng mã OTP (Google Authenticator hoặc SMS).
                </p>
                <app-btn
                  [variant]="twoFaEnabled() ? 'outline' : 'primary'"
                  size="sm"
                  [label]="twoFaEnabled() ? 'Tắt 2FA' : 'Bật 2FA'"
                  (clicked)="toggleTwoFa()"
                />
              </app-card>

              <app-card title="Phiên đăng nhập" variant="default">
                <div class="space-y-3 mt-2">
                  @for (session of sessions; track session.id) {
                    <div class="flex items-center justify-between py-2 border-b border-border/50 last:border-0">
                      <div class="flex items-center gap-3">
                        <app-icon [name]="session.icon" size="md" class="text-fg-muted" />
                        <div>
                          <p class="text-small font-medium text-fg">{{ session.device }}</p>
                          <p class="text-xs text-fg-muted">{{ session.ip }} · {{ session.time }}</p>
                        </div>
                      </div>
                      @if (session.current) {
                        <app-badge variant="up" size="sm">Hiện tại</app-badge>
                      } @else {
                        <app-btn variant="ghost" size="sm" label="Đăng xuất" />
                      }
                    </div>
                  }
                </div>
              </app-card>
            </div>
          }

          <!-- Notifications -->
          @if (activeSection() === 'notif') {
            <app-card title="Cài đặt thông báo" variant="elevated">
              <div class="space-y-4 mt-2">
                @for (setting of notifSettings; track setting.id) {
                  <div class="flex items-center justify-between py-3 border-b border-border/50 last:border-0">
                    <div>
                      <p class="text-small font-medium text-fg">{{ setting.label }}</p>
                      <p class="text-xs text-fg-muted">{{ setting.desc }}</p>
                    </div>
                    <button
                      [class]="'w-11 h-6 rounded-full transition-colors relative shrink-0 ' +
                        (setting.enabled ? 'bg-up' : 'bg-border')"
                      (click)="setting.enabled = !setting.enabled"
                      [attr.aria-checked]="setting.enabled"
                      role="switch"
                      [attr.aria-label]="setting.label"
                    >
                      <span [class]="'absolute top-1 w-4 h-4 rounded-full bg-white shadow transition-all ' +
                        (setting.enabled ? 'left-6' : 'left-1')"></span>
                    </button>
                  </div>
                }
              </div>
            </app-card>
          }

          <!-- Theme & Language -->
          @if (activeSection() === 'theme') {
            <app-card title="Giao diện & Ngôn ngữ" variant="elevated">
              <div class="space-y-6 mt-2">
                <!-- Theme -->
                <div>
                  <p class="text-small font-medium text-fg mb-3">Chủ đề</p>
                  <div class="grid grid-cols-2 gap-3">
                    <button
                      [class]="'p-4 rounded-xl border-2 transition-all text-center space-y-2 ' +
                        (themeService.isDark() ? 'border-up bg-up/5' : 'border-border hover:border-border-hover')"
                      (click)="themeService.setTheme('dark')"
                    >
                      <div class="w-full h-12 rounded-lg bg-gray-900 border border-gray-700 flex items-center justify-center">
                        <app-icon name="moon" size="md" class="text-gray-300" />
                      </div>
                      <p class="text-small font-medium text-fg">Tối (Dark)</p>
                    </button>
                    <button
                      [class]="'p-4 rounded-xl border-2 transition-all text-center space-y-2 ' +
                        (!themeService.isDark() ? 'border-up bg-up/5' : 'border-border hover:border-border-hover')"
                      (click)="themeService.setTheme('light')"
                    >
                      <div class="w-full h-12 rounded-lg bg-gray-100 border border-gray-200 flex items-center justify-center">
                        <app-icon name="sun" size="md" class="text-gray-700" />
                      </div>
                      <p class="text-small font-medium text-fg">Sáng (Light)</p>
                    </button>
                  </div>
                </div>

                <!-- Language -->
                <div>
                  <p class="text-small font-medium text-fg mb-3">Ngôn ngữ</p>
                  <div class="space-y-2">
                    @for (lang of languages; track lang.id) {
                      <label class="flex items-center gap-3 p-3 rounded-lg border border-border cursor-pointer hover:bg-surface-2 transition-colors">
                        <input type="radio" name="lang" [value]="lang.id"
                          [checked]="selectedLang() === lang.id"
                          (change)="selectedLang.set(lang.id)"
                          class="accent-up" />
                        <span class="text-lg">{{ lang.flag }}</span>
                        <span class="text-small text-fg">{{ lang.label }}</span>
                      </label>
                    }
                  </div>
                </div>
              </div>
            </app-card>
          }
        </div>
      </div>
    </div>
  `,
})
export class SettingsComponent {
  readonly themeService = inject(ThemeService);
  private readonly authService = inject(AuthService);
  readonly activeSection = signal('profile');
  readonly isSaving = signal(false);
  readonly twoFaEnabled = signal(true);
  readonly selectedLang = signal('vi');
  readonly saveSuccess = signal(false);

  readonly settingsNav = [
    { id: 'profile', label: 'Hồ sơ cá nhân', icon: 'user' },
    { id: 'security', label: 'Bảo mật', icon: 'check-circle' },
    { id: 'notif', label: 'Thông báo', icon: 'bell' },
    { id: 'theme', label: 'Giao diện', icon: 'moon' },
  ];

  profile = {
    name: this.authService.user()?.username ?? '' as string | number,
    phone: '' as string | number,
    email: this.authService.user()?.email ?? '' as string | number,
    cccd: '' as string | number,
  };

  sec = { oldPass: '' as string | number, newPass: '' as string | number, confirmPass: '' as string | number };

  readonly sessions = [
    { id: 1, device: 'Chrome · Windows 11', ip: '192.168.1.5', time: 'Hiện tại', icon: 'settings', current: true },
    { id: 2, device: 'Safari · iPhone 15 Pro', ip: '118.70.22.1', time: 'Hôm qua', icon: 'user', current: false },
    { id: 3, device: 'Firefox · macOS Sequoia', ip: '172.16.0.20', time: '2 ngày trước', icon: 'settings', current: false },
  ];

  notifSettings = [
    { id: 'price_alert', label: 'Cảnh báo giá', desc: 'Thông báo khi giá chạm ngưỡng cảnh báo', enabled: true },
    { id: 'order_exec', label: 'Khớp lệnh', desc: 'Thông báo khi lệnh được khớp', enabled: true },
    { id: 'news', label: 'Tin tức thị trường', desc: 'Tin nổi bật liên quan đến danh mục của bạn', enabled: false },
    { id: 'weekly', label: 'Báo cáo tuần', desc: 'Tóm tắt hiệu suất danh mục mỗi tuần', enabled: true },
    { id: 'marketing', label: 'Chương trình khuyến mãi', desc: 'Ưu đãi và tính năng mới từ QuantIQ', enabled: false },
  ];

  readonly languages = [
    { id: 'vi', label: 'Tiếng Việt', flag: '🇻🇳' },
    { id: 'en', label: 'English', flag: '🇺🇸' },
  ];

  toggleTwoFa(): void {
    this.twoFaEnabled.update(v => !v);
  }

  save(): void {
    this.isSaving.set(true);
    setTimeout(() => {
      this.isSaving.set(false);
      this.saveSuccess.set(true);
      setTimeout(() => this.saveSuccess.set(false), 3000);
    }, 800);
  }
}
