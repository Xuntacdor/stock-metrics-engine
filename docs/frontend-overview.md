# 🖥️ QuantIQ Platform – Frontend Overview (Angular 17)

> **Tech Stack chốt:** Angular 17 (Standalone Components) + TailwindCSS 3.4 + OKLCH Color System  
> **Cập nhật:** 05/03/2026  
> **Phối hợp với:** Backend .NET 8 API (JWT Auth, SignalR, PayOS)

---

## 🎨 Design System Foundation

### Color Palette (Semantic – Chuẩn Chứng Khoán Việt Nam)

Tất cả màu lưu trong CSS Custom Properties, import vào TailwindCSS config:

```css
/* src/styles.css */
:root {
  /* Giao dịch */
  --color-up:          oklch(0.54 0.24 120);  /* Tăng / Lãi → Xanh lá  #10B981 */
  --color-down:        oklch(0.54 0.22 15);   /* Giảm / Lỗ  → Đỏ      #DC2626 */
  --color-limit-up:    oklch(0.52 0.25 260);  /* Trần       → Tím      #A020F0 */
  --color-limit-down:  oklch(0.60 0.16 200);  /* Sàn        → Cyan     #0891B2 */
  --color-reference:   oklch(0.80 0.10 50);   /* Tham chiếu → Vàng     #EAB308 */

  /* Nền & Surface (Dark Mode mặc định) */
  --color-bg:          oklch(0.08 0 0);       /* Deep Dark  #0A0A0A */
  --color-surface:     oklch(0.15 0 0);       /* Card BG    #1A1A1A */
  --color-fg:          oklch(0.98 0 0);       /* Text sáng  #F5F5F5 */
  --color-border:      oklch(0.25 0 0);       /* Divider    #404040 */

  /* Light Mode override (dùng .light-theme class) */
  --color-bg-light:    oklch(0.98 0 0);
  --color-surface-light: oklch(0.94 0 0);
  --color-fg-light:    oklch(0.10 0 0);
}
```

```typescript
// src/app/core/tokens/colors.ts
export const COLORS = {
  up:         'var(--color-up)',
  down:       'var(--color-down)',
  limitUp:    'var(--color-limit-up)',
  limitDown:  'var(--color-limit-down)',
  reference:  'var(--color-reference)',
  bg:         'var(--color-bg)',
  surface:    'var(--color-surface)',
  fg:         'var(--color-fg)',
  border:     'var(--color-border)',
} as const;

export type ColorKey = keyof typeof COLORS;
```

### Typography Scale (Base 8px)

| Token    | Size  | Dùng cho               |
|----------|-------|------------------------|
| Display  | 32px  | Hero titles            |
| Headline | 24px  | Page titles            |
| Title    | 18px  | Section headers        |
| Body     | 14px  | Main content           |
| Small    | 12px  | Secondary / meta       |
| Xs       | 11px  | Labels, captions       |

> **Font:** `Inter` từ Google Fonts (npm: `fontsource`)

### Spacing Grid (8px base)

| Token | Value |
|-------|-------|
| xs    | 4px   |
| sm    | 8px   |
| md    | 16px  |
| lg    | 24px  |
| xl    | 32px  |
| 2xl   | 48px  |

---

## 📐 Atomic Design – Cấu Trúc Component

### Atoms (Đơn vị nhỏ nhất, không phụ thuộc)

| Component         | Selector              | Input Variants                                      |
|-------------------|-----------------------|-----------------------------------------------------|
| `ButtonComponent` | `app-btn`             | `variant`: primary / secondary / outline / ghost    |
| `BadgeComponent`  | `app-badge`           | `color`: up / down / limitUp / limitDown / neutral  |
| `InputComponent`  | `app-input`           | `state`: default / focused / error / disabled       |
| `SkeletonComponent`| `app-skeleton`       | `type`: text / circle / rect; `width`, `height`     |
| `LabelComponent`  | `app-label`           | `required`: boolean                                 |
| `IconComponent`   | `app-icon`            | `name`: string (Lucide icon names)                  |

### Molecules (Tổ hợp của Atoms)

| Component           | Selector               | Mô tả                                              |
|---------------------|------------------------|----------------------------------------------------|
| `CardComponent`     | `app-card`             | Surface container với border/shadow                |
| `FormFieldComponent`| `app-form-field`       | Label + Input + Error (tích hợp Reactive Forms)    |
| `PriceDisplayComponent`| `app-price-display` | Icon + Giá + % thay đổi + Trend indicator          |
| `StatBoxComponent`  | `app-stat-box`         | Title + Value + Change + Icon                      |
| `TabNavComponent`   | `app-tab-nav`          | Tab navigation với active state                    |

### Organisms (Sections phức tạp)

| Component              | Selector                | Mô tả                                          |
|------------------------|-------------------------|------------------------------------------------|
| `PriceTableComponent`  | `app-price-table`       | Bảng giá với virtual scroll (CDK)              |
| `CandleChartComponent` | `app-candle-chart`      | Biểu đồ nến (lightweight-charts)              |
| `PortfolioSummaryComponent`| `app-portfolio-summary`| P&L card + Allocation pie                 |
| `NewsSectionComponent` | `app-news-section`      | Danh sách tin tức + Sentiment badge            |
| `AlertBannerComponent` | `app-alert-banner`      | Toast + Notification center                    |

### Templates (Layouts dùng chung)

| Component               | Selector                 | Dùng ở                                    |
|-------------------------|--------------------------|-------------------------------------------|
| `DashboardLayoutComponent`| `app-dashboard-layout` | Dashboard, Portfolio, Screener, Alerts    |
| `TradingLayoutComponent`  | `app-trading-layout`   | Stock Detail (Chart + Orderbook sidebar)  |
| `FormLayoutComponent`     | `app-form-layout`      | Auth, Settings, Deposit/Withdraw          |

---

## 🗂️ File Structure

```
src/
├── app/
│   ├── core/                          # Singleton services & interceptors
│   │   ├── interceptors/
│   │   │   ├── auth.interceptor.ts    # Gắn JWT vào mọi request
│   │   │   └── error.interceptor.ts   # Global error handling
│   │   ├── guards/
│   │   │   └── auth.guard.ts          # Route guard
│   │   ├── services/
│   │   │   ├── auth.service.ts
│   │   │   ├── market-data.service.ts # WebSocket / SignalR
│   │   │   ├── portfolio.service.ts
│   │   │   └── theme.service.ts       # Dark/Light toggle
│   │   └── tokens/
│   │       └── colors.ts              # CSS variable constants
│   │
│   ├── shared/                        # Atomic Design components
│   │   ├── atoms/
│   │   │   ├── button/
│   │   │   ├── badge/
│   │   │   ├── input/
│   │   │   ├── skeleton/
│   │   │   ├── label/
│   │   │   └── icon/
│   │   ├── molecules/
│   │   │   ├── card/
│   │   │   ├── form-field/
│   │   │   ├── price-display/
│   │   │   ├── stat-box/
│   │   │   └── tab-nav/
│   │   └── organisms/
│   │       ├── price-table/
│   │       ├── candle-chart/
│   │       ├── portfolio-summary/
│   │       ├── news-section/
│   │       └── alert-banner/
│   │
│   ├── layouts/                       # Templates
│   │   ├── dashboard-layout/
│   │   ├── trading-layout/
│   │   └── form-layout/
│   │
│   ├── pages/                         # Lazy-loaded feature modules
│   │   ├── auth/
│   │   │   ├── login/
│   │   │   ├── register/
│   │   │   └── kyc/
│   │   ├── dashboard/
│   │   ├── stock-detail/
│   │   ├── portfolio/
│   │   ├── screener/
│   │   ├── alerts/
│   │   ├── deposit/
│   │   └── settings/
│   │
│   ├── app.routes.ts                  # Root routes (lazy load)
│   ├── app.config.ts                  # Standalone bootstrap config
│   └── app.component.ts
│
├── environments/
│   ├── environment.ts                 # Dev: API base URL, WS URL
│   └── environment.prod.ts
│
└── styles.css                         # CSS variables + Tailwind base
```

---

## 🛣️ Routing (Lazy-loaded Standalone)

```typescript
// app.routes.ts
import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';

export const routes: Routes = [
  {
    path: 'auth',
    loadChildren: () => import('./pages/auth/auth.routes').then(m => m.AUTH_ROUTES),
  },
  {
    path: '',
    canActivate: [authGuard],
    loadComponent: () => import('./layouts/dashboard-layout/dashboard-layout.component')
      .then(m => m.DashboardLayoutComponent),
    children: [
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
      {
        path: 'dashboard',
        loadComponent: () => import('./pages/dashboard/dashboard.component')
          .then(m => m.DashboardComponent),
      },
      {
        path: 'stocks/:symbol',
        loadComponent: () => import('./pages/stock-detail/stock-detail.component')
          .then(m => m.StockDetailComponent),
      },
      {
        path: 'portfolio',
        loadComponent: () => import('./pages/portfolio/portfolio.component')
          .then(m => m.PortfolioComponent),
      },
      {
        path: 'screener',
        loadComponent: () => import('./pages/screener/screener.component')
          .then(m => m.ScreenerComponent),
      },
      {
        path: 'alerts',
        loadComponent: () => import('./pages/alerts/alerts.component')
          .then(m => m.AlertsComponent),
      },
      {
        path: 'deposit',
        loadComponent: () => import('./pages/deposit/deposit.component')
          .then(m => m.DepositComponent),
      },
      {
        path: 'settings',
        loadComponent: () => import('./pages/settings/settings.component')
          .then(m => m.SettingsComponent),
      },
    ],
  },
  { path: '**', redirectTo: 'dashboard' },
];
```

---

## 🔄 State Management (Angular Signals)

Angular 17 Signals là lựa chọn native, không cần thư viện ngoài:

```typescript
// core/services/market-data.service.ts
import { Injectable, signal, computed } from '@angular/core';

export interface MarketState {
  vnIndex: number;
  hnxIndex: number;
  upCount: number;
  downCount: number;
  refCount: number;
  limitUpCount: number;
  limitDownCount: number;
}

@Injectable({ providedIn: 'root' })
export class MarketDataService {
  // State
  private _market = signal<MarketState>({
    vnIndex: 0, hnxIndex: 0,
    upCount: 0, downCount: 0, refCount: 0,
    limitUpCount: 0, limitDownCount: 0,
  });

  // Public readonly signals
  readonly market = this._market.asReadonly();

  // Computed signals
  readonly upPercent = computed(() => {
    const { upCount, downCount, refCount } = this._market();
    const total = upCount + downCount + refCount || 1;
    return (upCount / total) * 100;
  });

  // Update từ WebSocket/SignalR
  updateMarket(data: Partial<MarketState>): void {
    this._market.update(current => ({ ...current, ...data }));
  }
}

// Trong component dùng như sau:
// market = inject(MarketDataService).market;
// upPercent = inject(MarketDataService).upPercent;
// Template: {{ market().vnIndex | number:'1.2-2' }}
```

```typescript
// core/services/portfolio.service.ts
@Injectable({ providedIn: 'root' })
export class PortfolioService {
  private _holdings = signal<Holding[]>([]);
  private _cashBalance = signal<number>(0);

  readonly holdings = this._holdings.asReadonly();
  readonly cashBalance = this._cashBalance.asReadonly();

  readonly totalUnrealizedPnL = computed(() =>
    this._holdings().reduce((sum, h) => sum + h.unrealizedPnL, 0)
  );

  readonly totalPortfolioValue = computed(() =>
    this._cashBalance() +
    this._holdings().reduce((sum, h) => sum + h.marketValue, 0)
  );
}
```

---

## 📡 Real-time Data (SignalR Integration)

Backend .NET 8 đã có SignalR → Angular kết nối qua `@microsoft/signalr`:

```typescript
// core/services/signalr.service.ts
import { Injectable, signal, OnDestroy } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class SignalRService implements OnDestroy {
  private hub!: signalR.HubConnection;
  readonly connectionState = signal<'connected' | 'disconnected' | 'connecting'>('disconnected');

  async connect(): Promise<void> {
    this.hub = new signalR.HubConnectionBuilder()
      .withUrl(`${environment.apiUrl}/hubs/market`, {
        accessTokenFactory: () => localStorage.getItem('token') ?? '',
      })
      .withAutomaticReconnect()
      .build();

    this.hub.onreconnecting(() => this.connectionState.set('connecting'));
    this.hub.onreconnected(() => this.connectionState.set('connected'));
    this.hub.onclose(() => this.connectionState.set('disconnected'));

    await this.hub.start();
    this.connectionState.set('connected');
  }

  on<T>(event: string, callback: (data: T) => void): void {
    this.hub.on(event, callback);
  }

  ngOnDestroy(): void {
    this.hub?.stop();
  }
}
```

---

## 📱 8 Screens – Đặc Tả Chi Tiết

### Screen 1: Auth Flow (Login / Register / E-KYC)

**Route:** `/auth/login`, `/auth/register`, `/auth/kyc`

**Components:**
- `LoginComponent` – Email/Phone + Password, link sang Register
- `RegisterComponent` – Đăng ký với validation (Reactive Forms + Zod-style validators)
- `KycComponent` – Upload CCCD mặt trước/sau → call KYC API (FPT AI)
- `OtpVerificationComponent` – 6-digit OTP, auto-focus next input

**Signals:**
```typescript
readonly isLoading = signal(false);
readonly errorMessage = signal<string | null>(null);
```

**Layout:** `FormLayoutComponent` – centered card, full-width mobile

---

### Screen 2: Dashboard (Bảng Giá + Portfolio P&L)

**Route:** `/dashboard`

**Components chính:**
- `MarketSummaryComponent` – VN-Index, HNX, Up/Down/Ref counts
- `WatchlistTableComponent` – Virtual scroll CDK, columns: Mã / Trần / Sàn / TC / Khớp / +/- / %
- `PortfolioSnapshotComponent` – Tổng tài sản, Unrealized P&L, Cash
- `TrendingStocksComponent` – Sparklines (lightweight-charts micro chart)

**Performance:**
```typescript
// Virtual scroll cho bảng giá lớn
// app/shared/organisms/price-table/price-table.component.html
<cdk-virtual-scroll-viewport itemSize="40" class="h-[600px]">
  <div *cdkVirtualFor="let stock of stocks()" class="price-row">
    <app-price-display [stock]="stock" />
  </div>
</cdk-virtual-scroll-viewport>
```

---

### Screen 3: Stock Detail (Candlestick Chart + Indicators + News)

**Route:** `/stocks/:symbol`

**Layout:** `TradingLayoutComponent` (Chart area 70% + Sidebar 30%)

**Components:**
- `CandleChartComponent` – Dùng `lightweight-charts` (TradingView OSS)
  - Timeframe selector: 1D / 1W / 1M / 3M / 1Y
  - Zoom/pan built-in
  - Volume histogram dưới chart
- `IndicatorPanelComponent` – RSI (overbought >70 shade đỏ, oversold <30 shade xanh), MACD signal
- `OrderBookComponent` – Bid/Ask 3 mức giá
- `NewsSectionComponent` – Sentiment badge (Bullish 🟢 / Bearish 🔴 / Neutral ⚪)

**Micro-animation khi RSI < 30:**
```typescript
// Pulse animation trigger
readonly showBuySignal = computed(() => this.rsi() < 30);
// Template: <div *ngIf="showBuySignal()" @pulse class="alert-badge">RSI < 30 – Oversold!</div>
```

---

### Screen 4: Portfolio Tracker (P&L Table + Charts)

**Route:** `/portfolio`

**Components:**
- `HoldingsTableComponent` – Symbol / SL / Giá vốn / Giá TT / P&L / % / Trend sparkline
- `AllocationChartComponent` – Pie chart phân bổ theo ngành (lightweight-charts pie)
- `PnlBarChartComponent` – Bar chart P&L từng vị thế
- `PortfolioScoreBadgeComponent` – Gamification badge ("Danh mục cân bằng", "Expert Trader")

**Mobile:** Stacked cards thay bảng ngang

---

### Screen 5: Screener / Bộ Lọc Cổ Phiếu

**Route:** `/screener`

**Components:**
- `FilterPanelComponent` – Chỉ số tài chính (P/E, ROE, Market Cap) + Kỹ thuật (RSI range, MA cross)
- `CriteriaBuilderComponent` – Drag-drop thêm criteria (CDK DragDrop)
- `ResultsTableComponent` – Virtual scroll, quick-add to watchlist button inline
- `SavedScreenersComponent` – Preset: Tăng trưởng / Giá trị / Momentum

**State:**
```typescript
readonly filters = signal<ScreenerFilter>({ peMin: 0, peMax: 100, rsiMin: 0, rsiMax: 100, ... });
readonly results = signal<Stock[]>([]);
readonly isFiltering = signal(false);
```

**Empty state:** Khi không có kết quả → gợi ý preset phổ biến

---

### Screen 6: Alerts / Cảnh Báo

**Route:** `/alerts`

**Components:**
- `AlertFormComponent` – Tạo cảnh báo: Giá / RSI / Volume / Tin tức; Tần suất: Realtime / 1min / Daily
- `AlertListComponent` – Danh sách đang active với toggle on/off
- `NotificationCenterComponent` – Bell icon + dropdown, mark as read
- `AlertHistoryComponent` – Log lịch sử trigger (kèm timestamp + giá tại thời điểm trigger)

**ARIA Live Region:**
```html
<div role="status" aria-live="polite" class="sr-only">
  {{ latestAlert() }}
</div>
```

---

### Screen 7: Deposit / Withdraw (Luồng 3 bước)

**Route:** `/deposit`

**Wizard 3 bước:**
1. **Chọn phương thức** – Bank transfer / E-wallet / Liên kết ACB / VCB / Vietcombank
2. **Nhập số tiền** – Min/max validation, số dư hiện tại
3. **Xác nhận** – Preview + Confirm → gọi PayOS API

**Components:**
- `MethodSelectorComponent` – Grid card banks với logo
- `AmountInputComponent` – Currency input với format VND (1,000,000)
- `ConfirmationDialogComponent` – Modal confirm với spinner khi đang xử lý
- `TransactionHistoryComponent` – Lịch sử nạp/rút với trạng thái (Pending/Success/Failed)

---

### Screen 8: Settings / Hồ Sơ

**Route:** `/settings`

**Tabs:**
- **Hồ sơ** – Avatar, thông tin cá nhân, đổi mật khẩu
- **Bảo mật** – 2FA setup, session management, IP whitelist
- **Thông báo** – Toggle từng loại cảnh báo
- **Giao diện** – Dark/Light mode toggle, ngôn ngữ (VI/EN), định dạng số

**Theme Toggle:**
```typescript
// core/services/theme.service.ts
@Injectable({ providedIn: 'root' })
export class ThemeService {
  private _theme = signal<'dark' | 'light'>('dark');
  readonly theme = this._theme.asReadonly();

  toggle(): void {
    const next = this._theme() === 'dark' ? 'light' : 'dark';
    this._theme.set(next);
    document.documentElement.classList.toggle('light-theme', next === 'light');
    localStorage.setItem('theme', next);
  }

  init(): void {
    const saved = localStorage.getItem('theme') as 'dark' | 'light' | null;
    const preferred = window.matchMedia('(prefers-color-scheme: light)').matches ? 'light' : 'dark';
    const theme = saved ?? preferred;
    this._theme.set(theme);
    document.documentElement.classList.toggle('light-theme', theme === 'light');
  }
}
```

---

## ♿ Accessibility (WCAG AA)

| Yêu cầu | Cách implement Angular |
|---------|------------------------|
| Color contrast 4.5:1 | OKLCH values đảm bảo tỉ lệ trên dark bg |
| Touch target ≥ 44px | `min-h-[44px] min-w-[44px]` trên mọi button |
| Form labels | `[for]` binding + `aria-describedby` error |
| Live regions | `aria-live="polite"` cho toast/price updates |
| Keyboard nav | `tabindex`, `(keydown.escape)` đóng modal |
| Icon buttons | `aria-label` trên mọi `app-icon` standalone |
| Semantic HTML | `<nav>`, `<main>`, `<form>`, `<table role="grid">` |

---

## ⚡ Performance Optimization

| Kỹ thuật | Angular 17 Implementation |
|----------|--------------------------|
| **Code splitting** | `loadComponent()` / `loadChildren()` lazy routes |
| **Virtual scroll** | `@angular/cdk/scrolling` – `CdkVirtualScrollViewport` |
| **OnPush CD** | `changeDetection: ChangeDetectionStrategy.OnPush` mọi component |
| **Skeleton loading** | `app-skeleton` hiển thị khi signal ở trạng thái loading |
| **Image lazy load** | `loading="lazy"` + `NgOptimizedImage` |
| **Signal-based reactivity** | Không dùng `async pipe` − dùng Signals để tránh re-render |
| **TrackBy** | `trackBy` trên mọi `@for` loop |

**ChangeDetection mẫu:**
```typescript
@Component({
  selector: 'app-price-table',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @for (stock of stocks(); track stock.symbol) {
      <app-price-display [stock]="stock" />
    }
  `
})
export class PriceTableComponent {
  stocks = input.required<Stock[]>();
}
```

---

## 🛠️ Technology Stack

| Thư viện | Version | Lý do |
|----------|---------|-------|
| **Angular** | 17.x | Standalone + Signals native |
| **TailwindCSS** | 3.4 | Stable, ecosystem đầy đủ |
| **lightweight-charts** | 4.x | TradingView OSS – candlestick chuẩn |
| **@microsoft/signalr** | 8.x | Real-time kết nối .NET SignalR hub |
| **@angular/cdk** | 17.x | Virtual scroll, DragDrop |
| **Angular Reactive Forms** | 17.x | Built-in, type-safe |
| **lucide-angular** | latest | Icon set nhất quán |
| **@angular/animations** | 17.x | Micro-animations built-in |

**Cài đặt:**
```bash
ng new frontend-web --standalone --routing --style=css
cd frontend-web
npm install -D tailwindcss postcss autoprefixer
npx tailwindcss init
npm install lightweight-charts @microsoft/signalr lucide-angular
ng add @angular/cdk
```

---

## 📱 Responsive Design

| Breakpoint | Chiều rộng | Layout |
|------------|-----------|--------|
| Desktop    | ≥ 1440px  | Sidebar cố định, 2-3 cột, bảng đầy đủ |
| Laptop     | ≥ 1024px  | Sidebar thu gọn, bảng ngang scroll |
| Tablet     | ≥ 768px   | Single column, navigation drawer |
| Mobile     | ≥ 375px   | Cards stacked, bottom sheet menu |

**Tailwind config:**
```javascript
// tailwind.config.js
module.exports = {
  content: ['./src/**/*.{html,ts}'],
  theme: {
    extend: {
      colors: {
        up:         'var(--color-up)',
        down:       'var(--color-down)',
        'limit-up': 'var(--color-limit-up)',
        'limit-down':'var(--color-limit-down)',
        reference:  'var(--color-reference)',
        surface:    'var(--color-surface)',
        fg:         'var(--color-fg)',
        border:     'var(--color-border)',
      },
      fontFamily: {
        sans: ['Inter', 'sans-serif'],
      },
      spacing: {
        'xs': '4px',
        'sm': '8px',
        'md': '16px',
        'lg': '24px',
        'xl': '32px',
        '2xl': '48px',
      }
    },
  },
};
```

---

## 🏗️ Build Phases (Frontend)

### Phase 1 – Foundation (Tuần 1)
- [ ] Khởi tạo Angular 17 project với Standalone config
- [ ] Cài đặt Tailwind + CSS variables trong `styles.css`
- [ ] Tạo `ThemeService` (Dark/Light toggle)
- [ ] Tạo toàn bộ Atom components (Button, Badge, Input, Skeleton, Label, Icon)
- [ ] Setup `AuthInterceptor` + `AuthGuard`
- [ ] Kết nối `SignalRService` với backend hub

### Phase 2 – Components (Tuần 2)
- [ ] Tạo Molecule components (Card, FormField, PriceDisplay, StatBox, TabNav)
- [ ] Tạo `CandleChartComponent` với `lightweight-charts`
- [ ] Tạo `PriceTableComponent` với CDK Virtual Scroll
- [ ] Tạo `PortfolioSummaryComponent`

### Phase 3 – Layouts & Auth (Tuần 3)
- [ ] `DashboardLayoutComponent` (Sidebar + Header)
- [ ] `TradingLayoutComponent` (Chart + Sidebar)
- [ ] `FormLayoutComponent`
- [ ] Auth flow: Login / Register / KYC / OTP

### Phase 4 – Pages (Tuần 4-5)
- [ ] Dashboard page (bảng giá + portfolio snapshot)
- [ ] Stock Detail page (chart + indicators + news)
- [ ] Portfolio page (P&L table + charts)
- [ ] Screener page (filters + results)
- [ ] Alerts page (form + history)
- [ ] Deposit/Withdraw page (wizard 3 bước)
- [ ] Settings page (profile + security + preferences)

### Phase 5 – Polish (Tuần 6)
- [ ] Skeleton loading trên toàn bộ async operations
- [ ] Micro-animations (price tick flash, chart zoom, page transitions)
- [ ] WCAG AA audit (contrast checker, keyboard nav test)
- [ ] Responsive validation 1440px → 375px
- [ ] Bundle size optimization (kiểm tra < 100KB JS per route)
- [ ] Gamification badges (Portfolio Score)

---

## 🔁 Đối Chiếu Backend API → Angular Service

| API Endpoint (Backend) | Angular Service Method |
|------------------------|------------------------|
| `POST /api/auth/login` | `AuthService.login()` |
| `POST /api/auth/register` | `AuthService.register()` |
| `POST /api/kyc/upload` | `KycService.uploadCCCD()` |
| `GET /api/portfolio` | `PortfolioService.loadHoldings()` |
| `GET /api/orders` | `OrderService.getOrders()` |
| `POST /api/orders` | `OrderService.placeOrder()` |
| `GET /api/wallet` | `WalletService.getBalance()` |
| `POST /api/payment/deposit` | `PaymentService.deposit()` |
| `GET /api/symbols` | `MarketDataService.getSymbols()` |
| `GET /api/symbols/:symbol/candles` | `ChartService.getCandles()` |
| SignalR Hub: `PriceUpdated` | `SignalRService.on<StockPrice>('PriceUpdated')` |
| SignalR Hub: `AlertTriggered` | `SignalRService.on<Alert>('AlertTriggered')` |

---

## ✅ Definition of Done – Frontend

| Tiêu chí | Đánh giá |
|----------|----------|
| 8 screens responsive 1440px → 375px | ✓ |
| 100% skeleton loading coverage | ✓ |
| WCAG AA (4.5:1 contrast, keyboard nav) | ✓ |
| Semantic Vietnamese colors (Trần/Sàn/TC) | ✓ |
| Atomic Design structure (Atoms → Pages) | ✓ |
| < 100KB JS per lazy-loaded route | ✓ |
| Micro-animations (price flash, transitions) | ✓ |
| Dark/Light theme toggle + system fallback | ✓ |
| SignalR real-time kết nối backend | ✓ |
| Angular Signals state management | ✓ |

---

*Tài liệu này là phần bổ sung cho `roadmap.MD` – tập trung vào Frontend Angular 17.*  
*Phối hợp với Backend team theo phần "Đối Chiếu Backend API" ở trên.*
