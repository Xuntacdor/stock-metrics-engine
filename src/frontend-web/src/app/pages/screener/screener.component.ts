import {
  Component, signal, computed,
  ChangeDetectionStrategy,
} from '@angular/core';
import { CommonModule, DecimalPipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { CardComponent } from '../../shared/molecules/card/card.component';
import { TabNavComponent, type TabItem } from '../../shared/molecules/tab-nav/tab-nav.component';
import { BadgeComponent } from '../../shared/atoms/badge/badge.component';
import { ButtonComponent } from '../../shared/atoms/button/button.component';
import { InputComponent } from '../../shared/atoms/input/input.component';
import { IconComponent } from '../../shared/atoms/icon/icon.component';
import { SkeletonComponent } from '../../shared/atoms/skeleton/skeleton.component';
import { type StockRow } from '../../shared/organisms/price-table/price-table.component';
import { getPriceColorClass, type PriceColor } from '../../core/tokens/colors';
import { ScreenerService } from '../../core/services/screener.service';
import { MarketDataService } from '../../core/services/market-data.service';
import { inject, OnInit, OnDestroy } from '@angular/core';

interface ScreenerFilter {
  priceMin: number; priceMax: number;
  peMin: number; peMax: number;
  rsiMin: number; rsiMax: number;
  marketCap: string;
  sector: string;
}

interface ScreenerResult extends StockRow {
  pe: number;
  rsi: number;
  marketCap: number;
  sector: string;
}

@Component({
  selector: 'app-screener',
  standalone: true,
  imports: [CommonModule, DecimalPipe, RouterLink, CardComponent, TabNavComponent, BadgeComponent, ButtonComponent, InputComponent, IconComponent, SkeletonComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="p-4 md:p-6 space-y-6 animate-fade-in">
      <!-- Header -->
      <div class="flex items-center justify-between flex-wrap gap-4">
        <div>
          <h1 class="text-headline font-bold text-fg">Bộ lọc cổ phiếu</h1>
          <p class="text-small text-fg-muted mt-0.5">Tìm cổ phiếu theo tiêu chí kỹ thuật & cơ bản</p>
        </div>
        <div class="flex gap-2">
          <app-btn variant="outline" size="sm" label="Lưu bộ lọc" />
          <app-btn variant="primary" size="sm" label="Tìm kiếm" [loading]="isSearching()" (clicked)="runSearch()" />
        </div>
      </div>

      <!-- Preset tabs -->
      <div class="flex items-center gap-2 flex-wrap">
        <span class="text-small text-fg-muted">Bộ lọc nhanh:</span>
        @for (preset of presets; track preset.id) {
          <button
            [class]="'px-3 py-1.5 rounded-full text-xs font-medium border transition-all ' +
              (activePreset() === preset.id
                ? 'bg-up/10 border-up/40 text-up'
                : 'border-border text-fg-muted hover:border-border-hover hover:text-fg')"
            (click)="applyPreset(preset)"
          >{{ preset.label }}</button>
        }
      </div>

      <div class="grid grid-cols-1 xl:grid-cols-4 gap-6">

        <!-- Filter Panel (1/4) -->
        <div class="xl:col-span-1">
          <app-card title="Bộ lọc" variant="elevated">

            <!-- Giá -->
            <div class="space-y-4">
              <div>
                <p class="text-xs font-semibold text-fg-muted uppercase tracking-wider mb-3">Giá (nghìn đồng)</p>
                <div class="grid grid-cols-2 gap-2">
                  <div>
                    <label class="text-xs text-fg-muted block mb-1">Từ</label>
                    <app-input type="number" placeholder="0" [(value)]="filters().priceMin" />
                  </div>
                  <div>
                    <label class="text-xs text-fg-muted block mb-1">Đến</label>
                    <app-input type="number" placeholder="500" [(value)]="filters().priceMax" />
                  </div>
                </div>
              </div>

              <hr class="border-border">

              <!-- P/E -->
              <div>
                <p class="text-xs font-semibold text-fg-muted uppercase tracking-wider mb-3">Chỉ số P/E</p>
                <div class="grid grid-cols-2 gap-2">
                  <div>
                    <label class="text-xs text-fg-muted block mb-1">Min</label>
                    <app-input type="number" placeholder="0" [(value)]="filters().peMin" />
                  </div>
                  <div>
                    <label class="text-xs text-fg-muted block mb-1">Max</label>
                    <app-input type="number" placeholder="30" [(value)]="filters().peMax" />
                  </div>
                </div>
              </div>

              <hr class="border-border">

              <!-- RSI -->
              <div>
                <p class="text-xs font-semibold text-fg-muted uppercase tracking-wider mb-3">RSI (14 ngày)</p>
                <div class="grid grid-cols-2 gap-2">
                  <div>
                    <label class="text-xs text-fg-muted block mb-1">Min</label>
                    <app-input type="number" placeholder="0" [(value)]="filters().rsiMin" />
                  </div>
                  <div>
                    <label class="text-xs text-fg-muted block mb-1">Max</label>
                    <app-input type="number" placeholder="100" [(value)]="filters().rsiMax" />
                  </div>
                </div>
              </div>

              <hr class="border-border">

              <!-- Vốn hóa -->
              <div>
                <p class="text-xs font-semibold text-fg-muted uppercase tracking-wider mb-3">Vốn hóa</p>
                <div class="space-y-2">
                  @for (cap of marketCaps; track cap.id) {
                    <label class="flex items-center gap-2 cursor-pointer text-small">
                      <input type="radio" name="cap" [value]="cap.id"
                        [checked]="filters().marketCap === cap.id"
                        (change)="updateFilter('marketCap', cap.id)"
                        class="accent-up" />
                      <span [class]="filters().marketCap === cap.id ? 'text-fg' : 'text-fg-muted'">{{ cap.label }}</span>
                    </label>
                  }
                </div>
              </div>

              <hr class="border-border">

              <!-- Ngành -->
              <div>
                <p class="text-xs font-semibold text-fg-muted uppercase tracking-wider mb-3">Ngành</p>
                <div class="space-y-1.5">
                  @for (s of sectors; track s) {
                    <label class="flex items-center gap-2 cursor-pointer text-small">
                      <input type="checkbox" (click)="toggleSector(s)" [checked]="selectedSectors().includes(s)" class="accent-up rounded" />
                      <span class="text-fg-muted">{{ s }}</span>
                    </label>
                  }
                </div>
              </div>

              <app-btn variant="outline" size="sm" label="Xóa bộ lọc" [fullWidth]="true" (clicked)="resetFilters()" />
            </div>
          </app-card>
        </div>

        <!-- Results (3/4) -->
        <div class="xl:col-span-3 space-y-4">

          <!-- Result header -->
          <div class="flex items-center justify-between">
            @if (!isSearching()) {
              <p class="text-small text-fg-muted">
                Tìm thấy <span class="text-fg font-semibold">{{ results().length }}</span> cổ phiếu
              </p>
            } @else {
              <app-skeleton type="text" width="md" height="md" />
            }
            <div class="flex items-center gap-2">
              <span class="text-xs text-fg-muted">Sắp xếp theo:</span>
              <select class="text-xs bg-surface-2 border border-border rounded-md px-2 py-1 text-fg focus:outline-none focus:border-up">
                <option>% Thay đổi</option>
                <option>RSI</option>
                <option>P/E</option>
                <option>Vốn hóa</option>
              </select>
            </div>
          </div>

          <!-- Result table -->
          @if (isSearching()) {
            <app-card variant="default" padding="none">
              <div class="divide-y divide-border/50">
                @for (i of [1,2,3,4,5,6,7,8]; track i) {
                  <div class="flex gap-4 items-center px-4 py-3">
                    <app-skeleton type="text" width="sm" height="md" />
                    <app-skeleton type="price" width="md" height="md" />
                    <app-skeleton type="price" width="sm" height="md" />
                    <app-skeleton type="price" width="sm" height="md" />
                    <app-skeleton type="price" width="sm" height="md" />
                    <app-skeleton type="rect" width="md" height="md" />
                  </div>
                }
              </div>
            </app-card>
          } @else if (results().length === 0) {
            <!-- Empty state -->
            <div class="flex flex-col items-center justify-center py-20 gap-4 text-fg-muted">
              <app-icon name="search" size="xl" />
              <div class="text-center">
                <p class="text-body font-medium text-fg">Không tìm thấy kết quả</p>
                <p class="text-small mt-1">Thử thay đổi tiêu chí lọc hoặc dùng bộ lọc nhanh</p>
              </div>
              <app-btn variant="outline" size="sm" label="Xem tất cả cổ phiếu" (clicked)="resetAndSearch()" />
            </div>
          } @else {
            <div class="overflow-hidden rounded-xl border border-border">
              <table class="w-full text-small" role="grid">
                <thead>
                  <tr class="bg-surface-2 border-b border-border text-xs text-fg-muted">
                    <th class="text-left px-4 py-3 font-medium">Mã CK</th>
                    <th class="text-right px-3 py-3 font-medium">Giá</th>
                    <th class="text-right px-3 py-3 font-medium">+/- %</th>
                    <th class="text-right px-3 py-3 font-medium">P/E</th>
                    <th class="text-right px-3 py-3 font-medium">RSI</th>
                    <th class="text-right px-3 py-3 font-medium">Vốn hóa (tỷ)</th>
                    <th class="px-3 py-3 font-medium">Ngành</th>
                    <th class="px-3 py-3"></th>
                  </tr>
                </thead>
                <tbody>
                  @for (r of results(); track r.symbol) {
                    <tr class="border-b border-border/50 hover:bg-surface-2 transition-colors group">
                      <td class="px-4 py-3">
                        <a [routerLink]="['/stocks', r.symbol]" class="font-bold text-fg hover:text-up transition-colors">{{ r.symbol }}</a>
                        <p class="text-xs text-fg-muted">{{ r.sector }}</p>
                      </td>
                      <td class="px-3 py-3 text-right font-numeric" [class]="getPriceClass(r)">{{ r.price | number:'1.1-2' }}</td>
                      <td class="px-3 py-3 text-right">
                        <app-badge [variant]="r.changePct >= 0 ? 'up' : 'down'" size="sm">
                          {{ r.changePct >= 0 ? '+' : '' }}{{ r.changePct | number:'1.2-2' }}%
                        </app-badge>
                      </td>
                      <td class="px-3 py-3 text-right font-numeric text-fg">{{ r.pe | number:'1.1-1' }}x</td>
                      <td class="px-3 py-3 text-right">
                        <span [class]="'font-numeric font-medium ' + rsiClass(r.rsi)">{{ r.rsi | number:'1.0-0' }}</span>
                      </td>
                      <td class="px-3 py-3 text-right font-numeric text-fg">{{ r.marketCap | number:'1.0-0' }}</td>
                      <td class="px-3 py-3">
                        <app-badge variant="neutral" size="sm">{{ r.sector }}</app-badge>
                      </td>
                      <td class="px-3 py-3">
                        <div class="flex gap-1 opacity-0 group-hover:opacity-100 transition-opacity">
                          <app-btn variant="primary" size="sm" label="Mua" />
                          <app-btn variant="outline" size="sm">
                            <app-icon name="bell" size="sm" />
                          </app-btn>
                        </div>
                      </td>
                    </tr>
                  }
                </tbody>
              </table>
            </div>
          }
        </div>
      </div>
    </div>
  `,
})
export class ScreenerComponent implements OnInit {
  private readonly screenerService = inject(ScreenerService);
  private readonly marketData = inject(MarketDataService);

  readonly isSearching = signal(false);
  readonly activePreset = signal('');
  readonly selectedSectors = signal<string[]>([]);
  readonly results = signal<ScreenerResult[]>([]);

  readonly filters = signal<ScreenerFilter>({
    priceMin: 0, priceMax: 500,
    peMin: 0, peMax: 30,
    rsiMin: 0, rsiMax: 100,
    marketCap: 'all', sector: '',
  });

  readonly presets = [
    { id: 'oversold', label: '📉 RSI Oversold (<30)', filter: { rsiMin: 0, rsiMax: 30 } },
    { id: 'overbought', label: '📈 RSI Overbought (>70)', filter: { rsiMin: 70, rsiMax: 100 } },
    { id: 'value', label: '💰 Giá trị (P/E < 12)', filter: { peMin: 0, peMax: 12 } },
    { id: 'largecap', label: '🏢 Vốn hóa lớn', filter: { marketCap: 'large' } },
    { id: 'gainers', label: '🚀 Tăng mạnh hôm nay', filter: {} },
  ];

  readonly sectors = ['Ngân hàng', 'Bất động sản', 'Công nghệ', 'Tiêu dùng', 'Vật liệu', 'Năng lượng', 'Dược phẩm'];
  readonly marketCaps = [
    { id: 'all', label: 'Tất cả' },
    { id: 'large', label: 'Lớn (> 10,000 tỷ)' },
    { id: 'mid', label: 'Vừa (1,000–10,000 tỷ)' },
    { id: 'small', label: 'Nhỏ (< 1,000 tỷ)' },
  ];

  ngOnInit() {
    if (this.marketData.stocks().length === 0) {
      this.marketData.getSymbols().subscribe(() => this.runSearch());
    } else {
      this.runSearch();
    }
  }

  runSearch(): void {
    if (this.isSearching()) return;
    this.isSearching.set(true);

    const filterQuery = {
      ...this.filters(),
      sector: this.selectedSectors()
    };

    this.screenerService.filterStocks(filterQuery).subscribe(results => {
      this.results.set(results);
      this.isSearching.set(false);
    });
  }

  applyPreset(preset: typeof this.presets[0]): void {
    this.activePreset.set(preset.id);
    this.filters.update(f => ({ ...f, ...preset.filter }));
    this.runSearch();
  }

  resetFilters(): void {
    this.activePreset.set('');
    this.selectedSectors.set([]);
    this.filters.set({ priceMin: 0, priceMax: 500, peMin: 0, peMax: 30, rsiMin: 0, rsiMax: 100, marketCap: 'all', sector: '' });
    this.runSearch();
  }

  resetAndSearch(): void { this.resetFilters(); }

  updateFilter(key: keyof ScreenerFilter, value: string | number): void {
    this.filters.update(f => ({ ...f, [key]: value }));
  }

  toggleSector(s: string): void {
    this.selectedSectors.update(list =>
      list.includes(s) ? list.filter(x => x !== s) : [...list, s]
    );
  }

  getPriceClass(r: ScreenerResult): string {
    const c = getPriceColorClass(r.price, r.refPrice, r.ceilPrice, r.floorPrice);
    const map: Record<PriceColor, string> = { up: 'text-up', down: 'text-down', 'limit-up': 'text-limit-up', 'limit-down': 'text-limit-down', reference: 'text-reference', neutral: 'text-fg-muted' };
    return map[c];
  }

  rsiClass(rsi: number): string {
    if (rsi >= 70) return 'text-down';
    if (rsi <= 30) return 'text-up';
    return 'text-fg';
  }
}
