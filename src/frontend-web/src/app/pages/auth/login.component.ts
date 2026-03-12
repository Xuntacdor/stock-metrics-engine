import {
  Component, signal, computed, inject,
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
import { AuthService } from '../../core/services/auth.service';
import { Router } from '@angular/router';
@Component({
  selector: 'app-login',
  standalone: true,
  imports: [
    CommonModule, FormsModule, RouterLink,
    CardComponent, FormFieldComponent, ButtonComponent,
    InputComponent, LabelComponent, IconComponent, BadgeComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="w-full max-w-md space-y-6">

      <!-- Card -->
      <div class="card-elevated rounded-2xl p-8 space-y-6 animate-fade-in">

        <!-- Header -->
        <div class="text-center space-y-1">
          <h1 class="text-headline font-bold text-fg">Đăng nhập</h1>
          <p class="text-small text-fg-muted">Chào mừng trở lại, nhà đầu tư!</p>
        </div>

        <!-- Social login quick options -->
        <div class="grid grid-cols-2 gap-3">
          <button class="flex items-center justify-center gap-2 h-10 rounded-lg border border-border bg-surface hover:bg-surface-2 transition-colors text-small text-fg-muted hover:text-fg">
            <svg class="w-4 h-4" viewBox="0 0 24 24" aria-hidden="true">
              <path fill="#4285F4" d="M22.56 12.25c0-.78-.07-1.53-.2-2.25H12v4.26h5.92c-.26 1.37-1.04 2.53-2.21 3.31v2.77h3.57c2.08-1.92 3.28-4.74 3.28-8.09z"/>
              <path fill="#34A853" d="M12 23c2.97 0 5.46-.98 7.28-2.66l-3.57-2.77c-.98.66-2.23 1.06-3.71 1.06-2.86 0-5.29-1.93-6.16-4.53H2.18v2.84C3.99 20.53 7.7 23 12 23z"/>
              <path fill="#FBBC05" d="M5.84 14.09c-.22-.66-.35-1.36-.35-2.09s.13-1.43.35-2.09V7.07H2.18C1.43 8.55 1 10.22 1 12s.43 3.45 1.18 4.93l2.85-2.22.81-.62z"/>
              <path fill="#EA4335" d="M12 5.38c1.62 0 3.06.56 4.21 1.64l3.15-3.15C17.45 2.09 14.97 1 12 1 7.7 1 3.99 3.47 2.18 7.07l3.66 2.84c.87-2.6 3.3-4.53 6.16-4.53z"/>
            </svg>
            <span>Google</span>
          </button>
          <button class="flex items-center justify-center gap-2 h-10 rounded-lg border border-border bg-surface hover:bg-surface-2 transition-colors text-small text-fg-muted hover:text-fg">
            <svg class="w-4 h-4" fill="currentColor" viewBox="0 0 24 24" aria-hidden="true">
              <path d="M24 12.073c0-6.627-5.373-12-12-12s-12 5.373-12 12c0 5.99 4.388 10.954 10.125 11.854v-8.385H7.078v-3.47h3.047V9.43c0-3.007 1.792-4.669 4.533-4.669 1.312 0 2.686.235 2.686.235v2.953H15.83c-1.491 0-1.956.925-1.956 1.874v2.25h3.328l-.532 3.47h-2.796v8.385C19.612 23.027 24 18.062 24 12.073z"/>
            </svg>
            <span>Facebook</span>
          </button>
        </div>

        <div class="flex items-center gap-3">
          <hr class="flex-1 border-border">
          <span class="text-xs text-fg-muted">hoặc đăng nhập bằng email</span>
          <hr class="flex-1 border-border">
        </div>

        <!-- Form -->
        <form class="space-y-4" (ngSubmit)="onSubmit()" #loginForm="ngForm" novalidate>

          <app-form-field label="Email hoặc Số điện thoại" fieldId="login-email"
            [required]="true" [hasError]="!!emailError()" [errorMessage]="emailError()">
            <app-input
              type="email"
              inputId="login-email"
              name="email"
              placeholder="trader&#64;quantiq.vn"
              autocomplete="email"
              [(value)]="email"
              [state]="emailError() ? 'error' : 'default'"
            />
          </app-form-field>

          <app-form-field label="Mật khẩu" fieldId="login-pass"
            [required]="true" [hasError]="!!passError()" [errorMessage]="passError()">
            <app-input
              type="password"
              inputId="login-pass"
              name="password"
              placeholder="••••••••"
              autocomplete="current-password"
              [(value)]="password"
              [state]="passError() ? 'error' : 'default'"
            />
          </app-form-field>

          <!-- Forgot password -->
          <div class="flex items-center justify-between text-small">
            <label class="flex items-center gap-2 cursor-pointer">
              <input type="checkbox" class="rounded border-border bg-surface-2 accent-up" />
              <span class="text-fg-muted">Ghi nhớ đăng nhập</span>
            </label>
            <a href="#" class="text-up hover:underline">Quên mật khẩu?</a>
          </div>

          <!-- Submit -->
          <app-btn
            type="submit"
            variant="primary"
            size="lg"
            label="Đăng nhập"
            [fullWidth]="true"
            [loading]="isLoading()"
            (clicked)="onSubmit()"
          />
        </form>

        <!-- Error banner -->
        @if (generalError()) {
          <div class="flex items-center gap-2.5 px-4 py-3 rounded-lg bg-down/10 border border-down/30 text-small text-down animate-fade-in" role="alert">
            <app-icon name="alert-triangle" size="sm" />
            {{ generalError() }}
          </div>
        }

        <!-- Register link -->
        <p class="text-center text-small text-fg-muted">
          Chưa có tài khoản?
          <a routerLink="/auth/register" class="text-up font-medium hover:underline ml-1">
            Đăng ký miễn phí →
          </a>
        </p>
      </div>

      <!-- Trust badges -->
      <div class="flex items-center justify-center gap-6 text-xs text-fg-muted">
        <span class="flex items-center gap-1">
          <app-icon name="check-circle" size="xs" class="text-up" />
          Bảo mật SSL 256-bit
        </span>
        <span class="flex items-center gap-1">
          <app-icon name="check-circle" size="xs" class="text-up" />
          UBCKNN cấp phép
        </span>
        <span class="flex items-center gap-1">
          <app-icon name="check-circle" size="xs" class="text-up" />
          2FA bảo vệ
        </span>
      </div>
    </div>
  `,
})
export class LoginComponent {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  readonly email = signal<string | number>('');
  readonly password = signal<string | number>('');
  readonly isLoading = signal(false);
  readonly generalError = signal('');

  readonly emailError = computed(() => {
    const v = this.email() as string;
    if (!v) return '';
    return !v.includes('@') ? 'Email không hợp lệ' : '';
  });

  readonly passError = computed(() => {
    const v = this.password() as string;
    if (!v) return '';
    return v.length < 8 ? 'Mật khẩu phải có ít nhất 8 ký tự' : '';
  });

  onSubmit(): void {
    const email = this.email() as string;
    const pass = this.password() as string;
    if (!email || !pass) { this.generalError.set('Vui lòng nhập đầy đủ thông tin.'); return; }
    if (this.emailError() || this.passError()) return;

    this.isLoading.set(true);
    this.generalError.set('');

    this.auth.login({ username: email, password: pass }).subscribe({
      next: () => {
        this.isLoading.set(false);
        this.router.navigate(['/dashboard']);
      },
      error: (err) => {
        this.isLoading.set(false);
        this.generalError.set(err.message || 'Sai tài khoản hoặc mật khẩu.');
      }
    });
  }
}
