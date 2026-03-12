import { Injectable, signal, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap, catchError, of } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface BuyingPowerResponse {
    buyingPower: number;
    availableCash: number;
    marginValue: number;
}

export interface RttResponse {
    rtt: number;
    loanAmount: number;
    isAtRisk: boolean;
    status: 'NO_LOAN' | 'SAFE' | 'CALL_MARGIN_ZONE' | 'FORCE_SELL_ZONE';
}

export interface RiskAlertItem {
    alertId: number;
    alertType: 'CALL_MARGIN' | 'FORCE_SELL';
    rtt: number;
    message: string;
    isAcknowledged: boolean;
    createdAt: string;
}

@Injectable({ providedIn: 'root' })
export class RiskService {
    private readonly http = inject(HttpClient);
    private readonly riskUrl = `${environment.apiUrl}/risk`;

    private readonly _buyingPower = signal<BuyingPowerResponse | null>(null);
    private readonly _rtt = signal<RttResponse | null>(null);
    private readonly _alerts = signal<RiskAlertItem[]>([]);

    readonly buyingPower = this._buyingPower.asReadonly();
    readonly rtt = this._rtt.asReadonly();
    readonly alerts = this._alerts.asReadonly();

    loadBuyingPower(): Observable<BuyingPowerResponse> {
        return this.http.get<BuyingPowerResponse>(`${this.riskUrl}/buying-power`).pipe(
            tap(res => this._buyingPower.set(res)),
            catchError(() => of({ buyingPower: 0, availableCash: 0, marginValue: 0 }))
        );
    }

    loadRtt(): Observable<RttResponse> {
        return this.http.get<RttResponse>(`${this.riskUrl}/rtt`).pipe(
            tap(res => this._rtt.set(res)),
            catchError(() => of({ rtt: 999, loanAmount: 0, isAtRisk: false, status: 'NO_LOAN' as const }))
        );
    }

    loadAlerts(limit = 20): Observable<RiskAlertItem[]> {
        return this.http.get<RiskAlertItem[]>(`${this.riskUrl}/alerts?limit=${limit}`).pipe(
            tap(res => this._alerts.set(res)),
            catchError(() => of([]))
        );
    }

    getRttStatusClass(status: string): string {
        switch (status) {
            case 'FORCE_SELL_ZONE': return 'risk-danger';
            case 'CALL_MARGIN_ZONE': return 'risk-warning';
            case 'SAFE': return 'risk-safe';
            default: return 'risk-neutral';
        }
    }

    getRttStatusLabel(status: string): string {
        switch (status) {
            case 'FORCE_SELL_ZONE': return 'Vùng Force Sell';
            case 'CALL_MARGIN_ZONE': return 'Vùng Call Margin';
            case 'SAFE': return 'An toàn';
            default: return '—';
        }
    }
}
