import {
  Component, inject, signal,
  ChangeDetectionStrategy,
} from '@angular/core';
import { RouterOutlet, RouterLink, RouterLinkActive, Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { ThemeService } from '../../core/services/theme.service';
import { AuthService } from '../../core/services/auth.service';
import { IconComponent } from '../../shared/atoms/icon/icon.component';
import { BadgeComponent } from '../../shared/atoms/badge/badge.component';

interface NavItem {
  path: string;
  label: string;
  icon: string;
  badge?: number;
}

@Component({
  selector: 'app-dashboard-layout',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive, CommonModule, IconComponent, BadgeComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="flex h-screen overflow-hidden bg-bg text-fg">

      <!-- ===== SIDEBAR ===== -->
      <aside
        [class]="'flex flex-col border-r border-border bg-surface transition-all duration-300 shrink-0 z-40 ' +
          (sidebarCollapsed() ? 'w-16' : 'w-60') +
          (mobileSidebarOpen() ? ' fixed inset-y-0 left-0 w-60 shadow-2xl' : ' relative hidden md:flex')"
      >
        <!-- Logo -->
        <div class="flex items-center h-14 px-4 border-b border-border shrink-0">
          @if (!sidebarCollapsed()) {
            <div class="flex items-center gap-2.5 animate-fade-in">
              <app-icon name="bar-chart-2" size="lg" class="text-up shrink-0" />
              <span class="text-title font-bold tracking-tight">QuantIQ</span>
            </div>
          } @else {
            <app-icon name="bar-chart-2" size="lg" class="text-up mx-auto" />
          }
        </div>

        <!-- Navigation -->
        <nav class="flex-1 overflow-y-auto py-4 space-y-1 px-2" role="navigation" aria-label="Menu chính">
          @for (item of navItems; track item.path) {
            <a
              [routerLink]="item.path"
              routerLinkActive="bg-up/10 text-up border-r-2 border-up"
              [routerLinkActiveOptions]="{ exact: item.path === '/' }"
              class="flex items-center gap-3 px-3 py-2.5 rounded-lg text-fg-muted hover:bg-surface-2 hover:text-fg transition-all duration-150 group min-touch"
              [attr.aria-label]="item.label"
              [title]="sidebarCollapsed() ? item.label : ''"
            >
              <app-icon [name]="item.icon" size="md" class="shrink-0" />
              @if (!sidebarCollapsed()) {
                <span class="text-small font-medium flex-1 animate-fade-in">{{ item.label }}</span>
                @if (item.badge) {
                  <app-badge variant="down" size="sm">{{ item.badge }}</app-badge>
                }
              }
            </a>
          }
        </nav>

        <!-- Sidebar footer -->
        <div class="border-t border-border p-3 space-y-1">
          <!-- Theme toggle -->
          <button
            class="flex items-center gap-3 w-full px-3 py-2.5 rounded-lg text-fg-muted hover:bg-surface-2 hover:text-fg transition-all duration-150 min-touch"
            (click)="themeService.toggle()"
            [attr.aria-label]="themeService.isDark() ? 'Chuyển sang Light mode' : 'Chuyển sang Dark mode'"
          >
            <app-icon [name]="themeService.isDark() ? 'sun' : 'moon'" size="md" class="shrink-0" />
            @if (!sidebarCollapsed()) {
              <span class="text-small font-medium animate-fade-in">
                {{ themeService.isDark() ? 'Light mode' : 'Dark mode' }}
              </span>
            }
          </button>

          <!-- User avatar -->
          @if (!sidebarCollapsed()) {
            <div class="flex items-center gap-3 px-3 py-2.5 rounded-lg hover:bg-surface-2 transition-colors cursor-pointer">
              <div class="w-7 h-7 rounded-full bg-up/20 flex items-center justify-center shrink-0">
                <app-icon name="user" size="sm" class="text-up" />
              </div>
              <div class="flex-1 min-w-0 animate-fade-in">
                <p class="text-small font-medium text-fg truncate">{{ authService.user()?.username ?? 'Người dùng' }}</p>
                <p class="text-xs text-fg-muted truncate">{{ authService.user()?.email ?? '' }}</p>
              </div>
              <button (click)="logout()" class="p-1 rounded text-fg-muted hover:text-down transition-colors" title="Đăng xuất">
                <app-icon name="log-out" size="sm" />
              </button>
            </div>
          }
        </div>
      </aside>

      <!-- ===== MAIN COLUMN ===== -->
      <div class="flex flex-col flex-1 min-w-0 overflow-hidden">

        <!-- Top Header -->
        <header class="h-14 border-b border-border bg-surface/80 backdrop-blur-md flex items-center justify-between px-4 shrink-0 z-30">
          <!-- Left: hamburger + breadcrumb -->
          <div class="flex items-center gap-3">
            <!-- Mobile sidebar toggle -->
            <button
              class="md:hidden p-1.5 rounded-md text-fg-muted hover:text-fg hover:bg-surface-2 transition-colors"
              (click)="mobileSidebarOpen.set(!mobileSidebarOpen())"
              aria-label="Mở menu"
            >
              <app-icon name="menu" size="md" />
            </button>
            <!-- Desktop collapse toggle -->
            <button
              class="hidden md:flex p-1.5 rounded-md text-fg-muted hover:text-fg hover:bg-surface-2 transition-colors"
              (click)="sidebarCollapsed.set(!sidebarCollapsed())"
              [attr.aria-label]="sidebarCollapsed() ? 'Mở rộng sidebar' : 'Thu gọn sidebar'"
            >
              <app-icon name="menu" size="md" />
            </button>
          </div>

          <!-- Right: notifications + user -->
          <div class="flex items-center gap-2">
            <!-- Live market indicator -->
            <div class="hidden sm:flex items-center gap-1.5 px-2.5 py-1 rounded-full bg-up/10 border border-up/30 text-xs text-up font-medium">
              <span class="w-1.5 h-1.5 rounded-full bg-up animate-pulse-soft"></span>
              Thị trường đang mở
            </div>

            <!-- Notifications bell -->
            <button
              class="relative p-2 rounded-md text-fg-muted hover:text-fg hover:bg-surface-2 transition-colors min-touch"
              aria-label="Thông báo (3 chưa đọc)"
            >
              <app-icon name="bell" size="md" />
              <span class="absolute top-1 right-1 w-2 h-2 rounded-full bg-down border border-surface"></span>
            </button>

            <!-- Settings -->
            <a
              routerLink="/settings"
              class="p-2 rounded-md text-fg-muted hover:text-fg hover:bg-surface-2 transition-colors min-touch"
              aria-label="Cài đặt"
            >
              <app-icon name="settings" size="md" />
            </a>
          </div>
        </header>

        <!-- Page content -->
        <main class="flex-1 overflow-y-auto" id="main-content" role="main">
          <router-outlet />
        </main>
      </div>

      <!-- Mobile sidebar overlay -->
      @if (mobileSidebarOpen()) {
        <div
          class="fixed inset-0 bg-black/60 z-30 md:hidden backdrop-blur-sm"
          (click)="mobileSidebarOpen.set(false)"
          aria-hidden="true"
        ></div>
      }
    </div>
  `,
})
export class DashboardLayoutComponent {
  readonly themeService = inject(ThemeService);
  readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  readonly sidebarCollapsed = signal(false);
  readonly mobileSidebarOpen = signal(false);

  logout(): void {
    this.authService.logout().subscribe({
      complete: () => this.router.navigate(['/auth/login']),
      error: () => this.router.navigate(['/auth/login']),
    });
  }

  readonly navItems: NavItem[] = [
    { path: '/dashboard', label: 'Tổng quan', icon: 'bar-chart-2' },
    { path: '/portfolio', label: 'Danh mục', icon: 'pie-chart' },
    { path: '/screener', label: 'Bộ lọc', icon: 'search' },
    { path: '/news', label: 'Tin tức', icon: 'rss' },
    { path: '/alerts', label: 'Cảnh báo', icon: 'bell', badge: 3 },
    { path: '/deposit', label: 'Nạp/Rút tiền', icon: 'wallet' },
    { path: '/risk', label: 'Quản trị rủi ro', icon: 'shield' },
    { path: '/leaderboard', label: 'Xếp hạng', icon: 'award' },
    { path: '/settings', label: 'Cài đặt', icon: 'settings' },
  ];
}
