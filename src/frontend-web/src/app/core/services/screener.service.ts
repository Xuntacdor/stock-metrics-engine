import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { environment } from '../../../environments/environment';
import { MarketDataService } from './market-data.service';

@Injectable({ providedIn: 'root' })
export class ScreenerService {
    private readonly marketData = inject(MarketDataService);

    filterStocks(filters: any): Observable<any[]> {
        return new Observable(subscriber => {
            setTimeout(() => {
                const stocks = this.marketData.stocks();

                const mappedData = stocks.map(s => {
                    const seed = s.symbol.charCodeAt(0) + s.symbol.length;

                    return {
                        ...s,
                        pe: 5 + (seed % 20) + Math.random() * 5,
                        rsi: 20 + (seed % 60) + + Math.random() * 10,
                        marketCap: (seed * 1000) + Math.random() * 10000,
                        sector: ['Ngân hàng', 'Bất động sản', 'Công nghệ', 'Tiêu dùng', 'Vật liệu', 'Năng lượng', 'Dược phẩm'][seed % 7]
                    };
                });

                const results = mappedData.filter(s =>
                    s.price >= filters.priceMin && (filters.priceMax === 0 || s.price <= filters.priceMax) &&
                    s.pe >= filters.peMin && (filters.peMax === 0 || s.pe <= filters.peMax) &&
                    s.rsi >= filters.rsiMin && s.rsi <= filters.rsiMax &&
                    (filters.marketCap === 'all' ||
                        (filters.marketCap === 'large' && s.marketCap >= 10_000) ||
                        (filters.marketCap === 'mid' && s.marketCap >= 1_000 && s.marketCap < 10_000) ||
                        (filters.marketCap === 'small' && s.marketCap < 1_000)) &&
                    (filters.sector.length === 0 || filters.sector.includes(s.sector))
                );

                subscriber.next(results);
                subscriber.complete();
            }, 600);
        });
    }
}
