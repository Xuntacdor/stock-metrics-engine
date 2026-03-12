import { Injectable, signal, computed, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { SignalRService } from './signalr.service';
import { StockRow } from '../../shared/organisms/price-table/price-table.component';

export interface MarketState {
    vnIndex: number;
    hnxIndex: number;
    upcomIndex: number;
    upCount: number;
    downCount: number;
    refCount: number;
    limitUpCount: number;
    limitDownCount: number;
}

@Injectable({ providedIn: 'root' })
export class MarketDataService {
    private readonly http = inject(HttpClient);
    private readonly signalR = inject(SignalRService);
    private readonly apiUrl = `${environment.apiUrl}/symbols`;

    private readonly _market = signal<MarketState>({
        vnIndex: 1274.52, hnxIndex: 231.85, upcomIndex: 90.1,
        upCount: 0, downCount: 0, refCount: 0,
        limitUpCount: 0, limitDownCount: 0,
    });

    private readonly _stocks = signal<StockRow[]>([]);

    readonly market = this._market.asReadonly();
    readonly stocks = this._stocks.asReadonly();

    constructor() {
        this.signalR.connect().then(() => {
            this.subscribeToRealtime();
        });
    }

    getSymbols(): Observable<StockRow[]> {
        return this.http.get<any[]>(`${this.apiUrl}`).pipe(
            tap(data => {
                const formattedData: StockRow[] = data.map(item => {
                    const mockRefPrice = Math.floor(Math.random() * 100) + 10;
                    const mockPrice = mockRefPrice + (Math.random() * 4 - 2);
                    return {
                        symbol: item.symbol,
                        name: (item as any).companyName || item.symbol,
                        price: mockPrice,
                        refPrice: mockRefPrice,
                        ceilPrice: mockRefPrice * 1.07,
                        floorPrice: mockRefPrice * 0.93,
                        change: mockPrice - mockRefPrice,
                        changePct: ((mockPrice - mockRefPrice) / mockRefPrice) * 100,
                        volume: Math.floor(Math.random() * 5000000),
                        value: (mockPrice * Math.floor(Math.random() * 5000000)) / 1000
                    };
                });
                this._stocks.set(formattedData);
                this.recalculateMarketStats(formattedData);
            })
        );
    }

    private subscribeToRealtime(): void {
        this.signalR.on<any>('PriceUpdated', (update) => {
            this._stocks.update(currentList => {
                return currentList.map(s => {
                    if (s.symbol === update.symbol) {
                        const newPrice = update.currentPrice ?? s.price;
                        return {
                            ...s,
                            price: newPrice,
                            change: newPrice - s.refPrice,
                            changePct: s.refPrice > 0 ? ((newPrice - s.refPrice) / s.refPrice) * 100 : 0,
                            volume: update.totalVolume ?? s.volume
                        };
                    }
                    return s;
                });
            });
            this.recalculateMarketStats(this._stocks());
        });
    }

    private recalculateMarketStats(list: StockRow[]): void {
        let upCount = 0;
        let downCount = 0;
        let refCount = 0;
        let limitUpCount = 0;
        let limitDownCount = 0;

        for (const s of list) {
            if (s.price === s.ceilPrice) limitUpCount++;
            else if (s.price === s.floorPrice) limitDownCount++;
            else if (s.price > s.refPrice) upCount++;
            else if (s.price < s.refPrice) downCount++;
            else refCount++;
        }

        this._market.update(m => ({ ...m, upCount, downCount, refCount, limitUpCount, limitDownCount }));
    }
}
