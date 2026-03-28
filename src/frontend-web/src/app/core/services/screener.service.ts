import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface ScreenerResult {
    symbol: string;
    companyName: string | null;
    exchange: string | null;
    sector: string | null;
    lastClose: number | null;
    prevClose: number | null;
    changePct: number;
    volume: number | null;
    pe: number | null;
    marketCap: number | null;
    rsi14: number | null;
    // Mapped for StockRow compatibility used by the screener table
    price: number;
    refPrice: number;
    ceilPrice: number;
    floorPrice: number;
    change: number;
    name: string;
    value: number;
    // Aliases used in screener-component template (rsi, marketCap as number)
    rsi: number;
}

export interface ScreenerFilter {
    priceMin?: number;
    priceMax?: number;
    peMin?: number;
    peMax?: number;
    rsiMin?: number;
    rsiMax?: number;
    volumeMin?: number;
    marketCap?: string;
    sector?: string | string[];
    sortBy?: string;
    sortDesc?: boolean;
    limit?: number;
}

@Injectable({ providedIn: 'root' })
export class ScreenerService {
    private readonly http = inject(HttpClient);
    private readonly base = `${environment.apiUrl}/screener`;

    filterStocks(filters: ScreenerFilter): Observable<ScreenerResult[]> {
        let params = new HttpParams();

        if (filters.priceMin != null)  params = params.set('priceMin',  filters.priceMin);
        if (filters.priceMax != null)  params = params.set('priceMax',  filters.priceMax);
        if (filters.peMin != null)     params = params.set('peMin',     filters.peMin);
        if (filters.peMax != null)     params = params.set('peMax',     filters.peMax);
        if (filters.rsiMin != null)    params = params.set('rsiMin',    filters.rsiMin);
        if (filters.rsiMax != null)    params = params.set('rsiMax',    filters.rsiMax);
        if (filters.volumeMin != null) params = params.set('volumeMin', filters.volumeMin);
        if (filters.marketCap)         params = params.set('marketCap', filters.marketCap);
        if (filters.sortBy)            params = params.set('sortBy',    filters.sortBy);
        if (filters.sortDesc != null)  params = params.set('sortDesc',  filters.sortDesc);
        if (filters.limit != null)     params = params.set('limit',     filters.limit);

        const sectorStr = Array.isArray(filters.sector)
            ? filters.sector.join(',')
            : (filters.sector ?? '');
        if (sectorStr) params = params.set('sector', sectorStr);

        return this.http.get<any[]>(this.base, { params }).pipe(
            map(items => items.map(item => {
                const price    = item.lastClose ?? 0;
                const refPrice = item.prevClose ?? price;
                return {
                    ...item,
                    price,
                    refPrice,
                    ceilPrice:  refPrice * 1.07,
                    floorPrice: refPrice * 0.93,
                    change:     price - refPrice,
                    name:       item.companyName ?? item.symbol,
                    value:      (price * (item.volume ?? 0)) / 1000,
                    rsi:        item.rsi14 ?? 50,           // alias for template
                } as ScreenerResult;
            }))
        );
    }
}
