import {
  Component, signal, computed,
  ChangeDetectionStrategy,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { CardComponent } from '../../shared/molecules/card/card.component';
import { FormFieldComponent } from '../../shared/molecules/form-field/form-field.component';
import { ButtonComponent } from '../../shared/atoms/button/button.component';
import { InputComponent } from '../../shared/atoms/input/input.component';
import { LabelComponent } from '../../shared/atoms/label/label.component';
import { IconComponent } from '../../shared/atoms/icon/icon.component';
import { BadgeComponent } from '../../shared/atoms/badge/badge.component';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [
    CommonModule, RouterLink, FormsModule,
    CardComponent, FormFieldComponent, ButtonComponent,
    InputComponent, LabelComponent, IconComponent, BadgeComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="w-full max-w-md space-y-6">

      <!-- Step indicator -->
      <div class="flex items-center justify-center gap-0">
        @for (step of steps; track step.id; let i = $index) {
          <div class="flex items-center">
            <!-- Step circle -->
            <div [class]="stepCircleClass(step.id)">
              @if (currentStep() > step.id) {
                <app-icon name="check" size="xs" />
              } @else {
                <span class="text-xs font-bold">{{ step.id }}</span>
              }
            </div>
            <!-- Connector line -->
            @if (i < steps.length - 1) {
              <div [class]="'h-0.5 w-16 transition-colors ' + (currentStep() > step.id ? 'bg-up' : 'bg-border')"></div>
            }
          </div>
        }
      </div>
      <p class="text-center text-small text-fg-muted">
        Bước {{ currentStep() }}: <span class="text-fg font-medium">{{ steps[currentStep()-1].label }}</span>
      </p>

      <!-- STEP 1: Account Info -->
      @if (currentStep() === 1) {
        <div class="card-elevated rounded-2xl p-8 space-y-5 animate-fade-in">
          <h1 class="text-headline font-bold text-fg text-center">Tạo tài khoản</h1>

          <app-form-field label="Họ và tên" fieldId="reg-name" [required]="true"
            [hasError]="nameSubmitted() && !name()" errorMessage="Vui lòng nhập họ tên">
            <app-input inputId="reg-name" placeholder="Nguyễn Văn A"
              [(value)]="name" [state]="nameSubmitted() && !name() ? 'error' : 'default'" />
          </app-form-field>

          <app-form-field label="Số điện thoại" fieldId="reg-phone" [required]="true"
            [hasError]="phoneError()" [errorMessage]="phoneError() ? 'Số điện thoại không hợp lệ' : ''">
            <app-input type="tel" inputId="reg-phone" placeholder="0912 345 678"
              autocomplete="tel" [(value)]="phone"
              [state]="phoneError() ? 'error' : 'default'" />
          </app-form-field>

          <app-form-field label="Email" fieldId="reg-email" [required]="true"
            [hasError]="emailError()" [errorMessage]="emailError() ? 'Email không hợp lệ' : ''">
            <app-input type="email" inputId="reg-email" placeholder="trader&#64;email.com"
              [(value)]="email" [state]="emailError() ? 'error' : 'default'" />
          </app-form-field>

          <app-form-field label="Mật khẩu" fieldId="reg-pass" [required]="true"
            hintMessage="Ít nhất 8 ký tự, bao gồm chữ hoa và số"
            [hasError]="passError()" [errorMessage]="passError() ? 'Mật khẩu quá yếu' : ''">
            <app-input type="password" inputId="reg-pass" placeholder="••••••••"
              [(value)]="password" [state]="passError() ? 'error' : 'default'" />
          </app-form-field>

          <!-- Password strength bar -->
          @if (password()) {
            <div class="space-y-1">
              <div class="flex gap-1">
                @for (i of [1,2,3,4]; track i) {
                  <div [class]="'h-1 flex-1 rounded-full transition-colors ' + strengthBarClass(i)"></div>
                }
              </div>
              <p [class]="'text-xs ' + strengthLabel().color">{{ strengthLabel().text }}</p>
            </div>
          }

          <label class="flex items-start gap-2 cursor-pointer">
            <input type="checkbox" [(ngModel)]="agreed" class="mt-0.5 accent-up rounded"/>
            <span class="text-small text-fg-muted">
              Tôi đồng ý với
              <a href="#" class="text-up hover:underline">Điều khoản dịch vụ</a>
              và
              <a href="#" class="text-up hover:underline">Chính sách bảo mật</a>
            </span>
          </label>

          <app-btn variant="primary" size="lg" label="Tiếp tục →" [fullWidth]="true"
            [loading]="isLoading()" (clicked)="nextStep()" />

          <p class="text-center text-small text-fg-muted">
            Đã có tài khoản?
            <a routerLink="/auth/login" class="text-up font-medium hover:underline ml-1">Đăng nhập</a>
          </p>
        </div>
      }

      <!-- STEP 2: OTP Verification -->
      @if (currentStep() === 2) {
        <div class="card-elevated rounded-2xl p-8 space-y-6 animate-fade-in">
          <div class="text-center space-y-2">
            <div class="w-16 h-16 rounded-full bg-up/10 flex items-center justify-center mx-auto">
              <app-icon name="bell" size="xl" class="text-up" />
            </div>
            <h2 class="text-title font-bold text-fg">Xác thực OTP</h2>
            <p class="text-small text-fg-muted">
              Mã OTP đã được gửi tới <span class="text-fg font-medium">{{ phone() }}</span>
            </p>
          </div>

          <!-- OTP inputs -->
          <div class="flex justify-center gap-3">
            @for (i of [0,1,2,3,4,5]; track i) {
              <input
                type="text"
                maxlength="1"
                inputmode="numeric"
                [id]="'otp-' + i"
                [value]="otpDigits()[i]"
                (input)="onOtpInput($event, i)"
                (keydown.backspace)="onOtpBackspace(i)"
                class="w-12 h-14 text-center text-headline font-bold bg-surface-2 border border-border rounded-xl text-fg focus:border-up focus:ring-2 focus:ring-up/30 focus:outline-none transition-all"
                [attr.aria-label]="'Chữ số OTP thứ ' + (i + 1)"
              />
            }
          </div>

          <app-btn variant="primary" size="lg" label="Xác thực" [fullWidth]="true"
            [loading]="isLoading()" (clicked)="verifyOtp()" />

          <p class="text-center text-small text-fg-muted">
            Chưa nhận được mã?
            <button class="text-up hover:underline ml-1" (click)="resendOtp()">Gửi lại ({{ countdown() }}s)</button>
          </p>
        </div>
      }

      <!-- STEP 3: KYC Upload -->
      @if (currentStep() === 3) {
        <div class="card-elevated rounded-2xl p-8 space-y-6 animate-fade-in">
          <div class="text-center space-y-2">
            <h2 class="text-title font-bold text-fg">Xác minh danh tính (eKYC)</h2>
            <p class="text-small text-fg-muted">Upload CCCD/Hộ chiếu để kích hoạt giao dịch</p>
          </div>

          <!-- Upload zones -->
          <div class="grid grid-cols-2 gap-4">
            @for (side of cccdSides; track side.id) {
              <div
                [class]="'border-2 border-dashed rounded-xl p-6 text-center cursor-pointer transition-all ' +
                  (side.uploaded ? 'border-up bg-price-up-10' : 'border-border hover:border-up/50 hover:bg-surface-2')"
                (click)="side.uploaded = !side.uploaded"
                [attr.aria-label]="'Upload ' + side.label"
              >
                @if (!side.uploaded) {
                  <div class="space-y-2">
                    <app-icon name="user" size="xl" class="text-fg-muted mx-auto" />
                    <p class="text-small font-medium text-fg">{{ side.label }}</p>
                    <p class="text-xs text-fg-muted">JPG, PNG &lt; 5MB</p>
                  </div>
                } @else {
                  <div class="space-y-2">
                    <app-icon name="check-circle" size="xl" class="text-up mx-auto" />
                    <p class="text-small font-medium text-up">Đã upload</p>
                    <p class="text-xs text-fg-muted">{{ side.label }}</p>
                  </div>
                }
              </div>
            }
          </div>

          <!-- Selfie -->
          <div
            class="border-2 border-dashed border-border rounded-xl p-6 text-center cursor-pointer hover:border-up/50 hover:bg-surface-2 transition-all"
            (click)="selfieUploaded.set(!selfieUploaded())"
          >
            @if (!selfieUploaded()) {
              <div class="space-y-2">
                <app-icon name="user" size="xl" class="text-fg-muted mx-auto" />
                <p class="text-small font-medium text-fg">Ảnh chân dung (selfie)</p>
                <p class="text-xs text-fg-muted">Chụp mặt thẳng, ánh sáng đủ</p>
              </div>
            } @else {
              <div class="space-y-2">
                <app-icon name="check-circle" size="xl" class="text-up mx-auto" />
                <p class="text-small font-medium text-up">Đã upload selfie</p>
              </div>
            }
          </div>

          <div class="flex items-start gap-2 p-3 rounded-lg bg-reference/10 border border-reference/30">
            <app-icon name="info" size="sm" class="text-reference shrink-0 mt-0.5" />
            <p class="text-xs text-fg-muted">
              Thông tin sẽ được xử lý bởi AI nhận dạng (FPT.AI) và đội ngũ kiểm duyệt trong vòng 1-2 giờ làm việc.
            </p>
          </div>

          <app-btn variant="primary" size="lg" label="Gửi xét duyệt" [fullWidth]="true"
            [loading]="isLoading()" (clicked)="submitKyc()" />
        </div>
      }
    </div>
  `,
})
export class RegisterComponent {
  readonly name = signal<string | number>('');
  readonly phone = signal<string | number>('');
  readonly email = signal<string | number>('');
  readonly password = signal<string | number>('');
  agreed = false;

  readonly currentStep = signal(1);
  readonly isLoading = signal(false);
  readonly nameSubmitted = signal(false);
  readonly selfieUploaded = signal(false);
  readonly countdown = signal(60);
  readonly otpDigits = signal(['', '', '', '', '', '']);

  readonly steps = [
    { id: 1, label: 'Thông tin tài khoản' },
    { id: 2, label: 'Xác thực OTP' },
    { id: 3, label: 'Xác minh danh tính' },
  ];

  cccdSides = [
    { id: 'front', label: 'Mặt trước CCCD', uploaded: false },
    { id: 'back', label: 'Mặt sau CCCD', uploaded: false },
  ];

  readonly emailError = computed(() => {
    const v = this.email() as string;
    return v.length > 0 && !v.includes('@');
  });

  readonly phoneError = computed(() => {
    const v = (this.phone() as string).replace(/\s/g, '');
    return v.length > 0 && !/^(0[3-9]\d{8})$/.test(v);
  });

  readonly passError = computed(() => {
    const v = this.password() as string;
    return v.length > 0 && v.length < 8;
  });

  readonly passwordStrength = computed(() => {
    const p = this.password() as string;
    let score = 0;
    if (p.length >= 8) score++;
    if (/[A-Z]/.test(p)) score++;
    if (/[0-9]/.test(p)) score++;
    if (/[^A-Za-z0-9]/.test(p)) score++;
    return score;
  });

  strengthBarClass(level: number): string {
    const s = this.passwordStrength();
    if (s === 0) return 'bg-border';
    const colors = ['bg-down', 'bg-reference', 'bg-up', 'bg-up'];
    return level <= s ? colors[s - 1] : 'bg-border';
  }

  strengthLabel(): { text: string; color: string } {
    const labels = [
      { text: '', color: '' },
      { text: 'Yếu', color: 'text-down' },
      { text: 'Trung bình', color: 'text-reference' },
      { text: 'Mạnh', color: 'text-up' },
      { text: 'Rất mạnh', color: 'text-up' },
    ];
    return labels[this.passwordStrength()];
  }

  stepCircleClass(step: number): string {
    const base = 'w-8 h-8 rounded-full flex items-center justify-center transition-all font-medium text-small ';
    if (this.currentStep() > step) return base + 'bg-up text-white';
    if (this.currentStep() === step) return base + 'bg-up/20 text-up border-2 border-up';
    return base + 'bg-surface-2 text-fg-muted border border-border';
  }

  nextStep(): void {
    this.nameSubmitted.set(true);
    if (!this.name() || this.emailError() || this.phoneError() || this.passError() || !this.agreed) return;
    this.isLoading.set(true);
    setTimeout(() => { this.isLoading.set(false); this.currentStep.set(2); this.startCountdown(); }, 1000);
  }

  startCountdown(): void {
    this.countdown.set(60);
    const t = setInterval(() => {
      this.countdown.update(v => { if (v <= 1) { clearInterval(t); return 0; } return v - 1; });
    }, 1000);
  }

  resendOtp(): void { if (this.countdown() === 0) this.startCountdown(); }

  onOtpInput(event: Event, index: number): void {
    const val = (event.target as HTMLInputElement).value.slice(-1);
    this.otpDigits.update(d => { const n = [...d]; n[index] = val; return n; });
    if (val && index < 5) (document.getElementById(`otp-${index + 1}`) as HTMLInputElement)?.focus();
  }

  onOtpBackspace(index: number): void {
    if (!this.otpDigits()[index] && index > 0)
      (document.getElementById(`otp-${index - 1}`) as HTMLInputElement)?.focus();
  }

  verifyOtp(): void {
    this.isLoading.set(true);
    setTimeout(() => { this.isLoading.set(false); this.currentStep.set(3); }, 1000);
  }

  submitKyc(): void {
    this.isLoading.set(true);
    setTimeout(() => { this.isLoading.set(false); }, 2000);
  }
}
