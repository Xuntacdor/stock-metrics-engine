import { Injectable, signal, computed, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Holding } from '../../shared/organisms/portfolio-summary/portfolio-summary.component';
import { MarketDataService } from './market-data.service';

export interface PortfolioTransaction {
    id: string;
    symbol: string;
    type: string;
    price: number;
    quantity: number;
    fee: number;
    tax: number;
    time: string;
}

export interface PortfolioSummaryResponse {
    userId: string;
    cashBalance: number;
    holdings: any[];
}

@Injectable({ providedIn: 'root' })
export class PortfolioService {
    private readonly http = inject(HttpClient);
    private readonly marketData = inject(MarketDataService);
    private readonly apiUrl = `${environment.apiUrl}/portfolios`;
    private readonly walletUrl = `${environment.apiUrl}/wallets`;

    private readonly _holdings = signal<Holding[]>([]);
    private readonly _cashBalance = signal<number>(0);
    private readonly _transactions = signal<PortfolioTransaction[]>([]);

    readonly holdings = this._holdings.asReadonly();
    readonly cashBalance = this._cashBalance.asReadonly();
    readonly transactions = this._transactions.asReadonly();

    readonly totalUnrealizedPnL = computed(() => {
        return this._holdings().reduce((sum, h) => {
            const currentPrice = this.getCurrentPrice(h.symbol) || h.currentPrice;
            const pnl = (currentPrice - h.avgCost) * h.quantity;
            return sum + pnl;
        }, 0);
    });

    readonly totalPortfolioValue = computed(() => {
        return this._cashBalance() + this._holdings().reduce((sum, h) => {
            const currentPrice = this.getCurrentPrice(h.symbol) || h.currentPrice;
            return sum + (currentPrice * h.quantity);
        }, 0);
    });

    loadPortfolio(): Observable<any> {
        return this.http.get<any>(this.apiUrl).pipe(
            tap(res => {
                const mappedHoldings: Holding[] = (res?.holdings || []).map((h: any) => ({
                    symbol: h.symbol,
                    name: h.symbol,
                    quantity: h.totalQuantity,
                    avgCost: h.avgCostPrice,
                    currentPrice: this.getCurrentPrice(h.symbol) || h.avgCostPrice,
                    marketValue: (this.getCurrentPrice(h.symbol) || h.avgCostPrice) * h.totalQuantity,
                    unrealizedPnL: ((this.getCurrentPrice(h.symbol) || h.avgCostPrice) - h.avgCostPrice) * h.totalQuantity,
                    unrealizedPct: h.avgCostPrice > 0 ? (((this.getCurrentPrice(h.symbol) || h.avgCostPrice) - h.avgCostPrice) / h.avgCostPrice) * 100 : 0,
                    sector: 'Khác'
                }));
                this._holdings.set(mappedHoldings);
            })
        );
    }

    loadWallet(): Observable<any> {
        return this.http.get<any>(this.walletUrl).pipe(
            tap((res: any) => {
                this._cashBalance.set(res.balance ?? 0);
            })
        );
    }

    private getCurrentPrice(symbol: string): number {
        const stocks = this.marketData.stocks();
        const st = stocks.find(s => s.symbol === symbol);
        return st ? st.price : 0;
    }
}
