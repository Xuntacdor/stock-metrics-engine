import { Component, signal, ChangeDetectionStrategy, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { NewsService, LeaderboardEntry } from '../../core/services/news.service';
import { CardComponent } from '../../shared/molecules/card/card.component';
import { SkeletonComponent } from '../../shared/atoms/skeleton/skeleton.component';
import { BadgeComponent } from '../../shared/atoms/badge/badge.component';
import { IconComponent } from '../../shared/atoms/icon/icon.component';

@Component({
    selector: 'app-leaderboard',
    standalone: true,
    imports: [CommonModule, CardComponent, SkeletonComponent, BadgeComponent, IconComponent],
    changeDetection: ChangeDetectionStrategy.OnPush,
    styles: [`
    .podium { display: flex; align-items: flex-end; justify-content: center; gap: 1rem; margin-bottom: 2rem; }
    .podium-item { display: flex; flex-direction: column; align-items: center; gap: .5rem; }
    .podium-block { display: flex; align-items: center; justify-content: center; border-radius: .5rem .5rem 0 0; font-size: 1.5rem; }
    .rank-badge { display: inline-flex; align-items: center; justify-content: center; width: 2rem; height: 2rem; border-radius: 50%; font-size: .8rem; font-weight: 700; }
    .rb-1 { background: #fbbf24; color: #000; }
    .rb-2 { background: #9ca3af; color: #000; }
    .rb-3 { background: #b45309; color: #fff; }
    .rb-n { background: var(--color-surface-2); color: var(--color-fg-muted); }
  `],
    template: `
    <div class="p-4 md:p-6 space-y-6 animate-fade-in">
      <div>
        <h1 class="text-headline font-bold text-fg">🏆 Bảng xếp hạng nhà đầu tư</h1>
        <p class="text-small text-fg-muted mt-0.5">Xếp hạng theo lợi nhuận thực hiện (Realized P&amp;L). Cập nhật ngay khi có giao dịch.</p>
      </div>

      @if (loading()) {
        <div class="space-y-3">
          @for (i of [1,2,3,4,5]; track i) { <app-skeleton height="60px" /> }
        </div>
      } @else if (entries().length === 0) {
        <div class="flex flex-col items-center py-24 text-center text-fg-muted">
          <span class="text-5xl mb-4">🏅</span>
          <p class="font-medium text-fg text-lg">Chưa có dữ liệu xếp hạng</p>
          <p class="text-small mt-1">Bảng xếp hạng sẽ hiển thị khi có giao dịch bán thực tế.</p>
        </div>
      } @else {
        <!-- Top 3 podium -->
        @if (entries().length >= 3) {
          <div class="podium">
            <!-- 2nd -->
            <div class="podium-item">
              <div class="w-12 h-12 rounded-full bg-surface-2 flex items-center justify-center text-lg">
                {{ entries()[1].username.charAt(0).toUpperCase() }}
              </div>
              <p class="text-xs font-semibold text-fg">{{ entries()[1].username }}</p>
              <p class="text-xs text-up font-numeric">+{{ entries()[1].realizedPnL | number:'1.0-0' }} ₫</p>
              <div class="podium-block w-20 h-16 bg-surface-2 border border-border">🥈</div>
            </div>
            <!-- 1st -->
            <div class="podium-item">
              <div class="w-14 h-14 rounded-full bg-up/20 flex items-center justify-center text-xl border-2 border-up">
                {{ entries()[0].username.charAt(0).toUpperCase() }}
              </div>
              <p class="text-sm font-bold text-fg">{{ entries()[0].username }}</p>
              <p class="text-sm text-up font-bold font-numeric">+{{ entries()[0].realizedPnL | number:'1.0-0' }} ₫</p>
              <div class="podium-block w-24 h-24 bg-up/10 border border-up">🥇</div>
            </div>
            <!-- 3rd -->
            <div class="podium-item">
              <div class="w-12 h-12 rounded-full bg-surface-2 flex items-center justify-center text-lg">
                {{ entries()[2].username.charAt(0).toUpperCase() }}
              </div>
              <p class="text-xs font-semibold text-fg">{{ entries()[2].username }}</p>
              <p class="text-xs text-up font-numeric">+{{ entries()[2].realizedPnL | number:'1.0-0' }} ₫</p>
              <div class="podium-block w-20 h-12 bg-surface-2 border border-border">🥉</div>
            </div>
          </div>
        }

        <!-- Full table -->
        <app-card title="Danh sách đầy đủ" variant="default">
          <div class="overflow-x-auto mt-2">
            <table class="w-full text-small">
              <thead>
                <tr class="border-b border-border text-xs text-fg-muted text-left">
                  <th class="pb-2 font-medium w-12">Hạng</th>
                  <th class="pb-2 font-medium">Nhà đầu tư</th>
                  <th class="pb-2 font-medium text-right">Lãi thực hiện</th>
                  <th class="pb-2 font-medium text-right">% Lãi</th>
                  <th class="pb-2 font-medium text-right">Số GD</th>
                </tr>
              </thead>
              <tbody>
                @for (e of entries(); track e.userId) {
                  <tr class="border-b border-border/40 hover:bg-surface-2 transition-colors">
                    <td class="py-3">
                      <span [class]="rankBadgeClass(e.rank)">{{ e.rank }}</span>
                    </td>
                    <td class="py-3">
                      <div class="flex items-center gap-2.5">
                        <div class="w-8 h-8 rounded-full bg-surface-2 flex items-center justify-center text-sm font-bold text-fg-muted">
                          {{ e.username.charAt(0).toUpperCase() }}
                        </div>
                        <span class="font-medium text-fg">{{ e.username }}</span>
                      </div>
                    </td>
                    <td class="py-3 text-right font-numeric font-bold"
                        [class]="e.realizedPnL >= 0 ? 'text-up' : 'text-down'">
                      {{ e.realizedPnL >= 0 ? '+' : '' }}{{ e.realizedPnL | number:'1.0-0' }} ₫
                    </td>
                    <td class="py-3 text-right font-numeric"
                        [class]="e.realizedPnLPct >= 0 ? 'text-up' : 'text-down'">
                      {{ e.realizedPnLPct >= 0 ? '+' : '' }}{{ e.realizedPnLPct | number:'1.1-1' }}%
                    </td>
                    <td class="py-3 text-right text-fg-muted">{{ e.tradeCount }}</td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
        </app-card>
      }
    </div>
  `,
})
export class LeaderboardComponent implements OnInit {
    private readonly newsSvc = inject(NewsService);
    readonly loading = signal(true);
    readonly entries = signal<LeaderboardEntry[]>([]);

    ngOnInit(): void {
        this.newsSvc.getLeaderboard(20).subscribe({
            next: (data) => { this.entries.set(data); this.loading.set(false); },
            error: () => this.loading.set(false),
        });
    }

    rankBadgeClass(rank: number): string {
        const base = 'rank-badge ';
        return base + (['rb-1', 'rb-2', 'rb-3'][rank - 1] ?? 'rb-n');
    }
}
