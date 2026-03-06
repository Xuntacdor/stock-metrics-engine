import { Component, ChangeDetectionStrategy } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { IconComponent } from '../../shared/atoms/icon/icon.component';


@Component({
  selector: 'app-form-layout',
  standalone: true,
  imports: [RouterOutlet, IconComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="min-h-screen bg-bg flex flex-col">
      <!-- Background decoration -->
      <div class="fixed inset-0 overflow-hidden pointer-events-none" aria-hidden="true">
        <div class="absolute -top-40 -right-40 w-96 h-96 rounded-full bg-up/5 blur-3xl"></div>
        <div class="absolute -bottom-40 -left-40 w-96 h-96 rounded-full bg-limit-up/5 blur-3xl"></div>
      </div>

      <!-- Logo header -->
      <header class="relative z-10 flex items-center justify-center py-8">
        <a href="/dashboard" class="flex items-center gap-2.5 group">
          <app-icon name="bar-chart-2" size="xl" class="text-up transition-transform group-hover:scale-110" />
          <span class="text-headline font-bold tracking-tight text-fg">QuantIQ</span>
        </a>
      </header>

      <!-- Content -->
      <main class="relative z-10 flex-1 flex items-start justify-center px-4 pb-12">
        <router-outlet />
      </main>

      <!-- Footer -->
      <footer class="relative z-10 text-center py-4 text-xs text-fg-muted">
        © 2026 QuantIQ · Nền tảng giao dịch chứng khoán Việt Nam
      </footer>
    </div>
  `,
})
export class FormLayoutComponent { }
