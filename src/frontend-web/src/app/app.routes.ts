import { Routes } from '@angular/router';
import { authGuard, guestGuard } from './core/guards/auth.guard';

export const routes: Routes = [
    { path: '', redirectTo: 'dashboard', pathMatch: 'full' },

    {
        path: 'auth',
        canActivate: [guestGuard],
        loadComponent: () =>
            import('./layouts/form-layout/form-layout.component').then(m => m.FormLayoutComponent),
        children: [
            { path: '', redirectTo: 'login', pathMatch: 'full' },
            {
                path: 'login',
                loadComponent: () => import('./pages/auth/login.component').then(m => m.LoginComponent),
                title: 'Đăng nhập – QuantIQ',
            },
            {
                path: 'register',
                loadComponent: () => import('./pages/auth/register.component').then(m => m.RegisterComponent),
                title: 'Đăng ký – QuantIQ',
            },
        ],
    },

    {
        path: '',
        canActivate: [authGuard],
        loadComponent: () =>
            import('./layouts/dashboard-layout/dashboard-layout.component').then(m => m.DashboardLayoutComponent),
        children: [
            {
                path: 'dashboard',
                loadComponent: () =>
                    import('./pages/dashboard/dashboard.component').then(m => m.DashboardComponent),
                title: 'Tổng quan – QuantIQ',
            },
            {
                path: 'portfolio',
                loadComponent: () =>
                    import('./pages/portfolio/portfolio.component').then(m => m.PortfolioComponent),
                title: 'Danh mục – QuantIQ',
            },
            {
                path: 'stocks/:symbol',
                loadComponent: () =>
                    import('./pages/stock-detail/stock-detail.component').then(m => m.StockDetailComponent),
                title: 'Chi tiết cổ phiếu – QuantIQ',
            },
            {
                path: 'screener',
                loadComponent: () =>
                    import('./pages/screener/screener.component').then(m => m.ScreenerComponent),
                title: 'Bộ lọc – QuantIQ',
            },
            {
                path: 'alerts',
                loadComponent: () =>
                    import('./pages/alerts/alerts.component').then(m => m.AlertsComponent),
                title: 'Cảnh báo – QuantIQ',
            },
            {
                path: 'deposit',
                loadComponent: () =>
                    import('./pages/deposit/deposit.component').then(m => m.DepositComponent),
                title: 'Nạp/Rút tiền – QuantIQ',
            },
            {
                path: 'risk',
                loadComponent: () =>
                    import('./pages/risk/risk.component').then(m => m.RiskComponent),
                title: 'Quản trị rủi ro – QuantIQ',
            },
            {
                path: 'settings',
                loadComponent: () =>
                    import('./pages/settings/settings.component').then(m => m.SettingsComponent),
                title: 'Cài đặt – QuantIQ',
            },
            {
                path: 'news',
                loadComponent: () =>
                    import('./pages/news/news.component').then(m => m.NewsComponent),
                title: 'Tin tức – QuantIQ',
            },
            {
                path: 'leaderboard',
                loadComponent: () =>
                    import('./pages/leaderboard/leaderboard.component').then(m => m.LeaderboardComponent),
                title: 'Bảng xếp hạng – QuantIQ',
            },
        ],
    },

    { path: '**', redirectTo: 'dashboard' },
];
