import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { tap } from 'rxjs/operators';
import { environment } from '../../../environments/environment';

export interface CandleData {
    time: string;
    open: number;
    high: number;
    low: number;
    close: number;
    volume: number;
}

@Injectable({ providedIn: 'root' })
export class ChartService {
    private readonly http = inject(HttpClient);
    private readonly apiUrl = `${environment.apiUrl}/symbols`;

    getCandles(symbol: string): Observable<CandleData[]> {


        return of(this.generateMockCandles()).pipe(
        );
    }

    private generateMockCandles(days = 100): CandleData[] {
        const candles: CandleData[] = [];
        let currentPrice = 50 + Math.random() * 50;
        const today = new Date();
        today.setHours(0, 0, 0, 0);

        for (let i = days; i >= 0; i--) {
            const date = new Date(today);
            date.setDate(date.getDate() - i);


            if (date.getDay() === 0 || date.getDay() === 6) continue;

            const volatility = 0.05;
            const change = currentPrice * volatility * (Math.random() - 0.5);

            const open = currentPrice;
            const close = currentPrice + change;
            const high = Math.max(open, close) + Math.random() * 2;
            const low = Math.min(open, close) - Math.random() * 2;
            const volume = Math.floor(Math.random() * 5000000) + 500000;

            candles.push({
                time: date.toISOString().split('T')[0],
                open,
                high,
                low,
                close,
                volume
            });

            currentPrice = close;
        }

        return candles;
    }
}
