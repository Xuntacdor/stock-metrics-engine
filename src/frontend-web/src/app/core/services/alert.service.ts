import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { SignalRService } from './signalr.service';

export interface AlertRule {
    alertId: number;
    symbol: string;
    alertType: 'price' | 'volume' | 'rsi' | 'news';
    condition: 'gt' | 'gte' | 'lt' | 'lte';
    thresholdValue: number;
    isActive: boolean;
    isTriggered: boolean;
    notifyOnce: boolean;
    createdAt: string;
    triggeredAt: string | null;
}

export interface CreateAlertRequest {
    symbol: string;
    alertType: 'price' | 'volume' | 'rsi' | 'news';
    condition: 'gt' | 'gte' | 'lt' | 'lte';
    thresholdValue: number;
    notifyOnce?: boolean;
}

export interface AlertTriggeredNotification {
    alertId: number;
    symbol: string;
    alertType: string;
    condition: string;
    thresholdValue: number;
    currentValue: number;
    triggeredAt: string;
}

@Injectable({ providedIn: 'root' })
export class AlertService {
    private readonly http = inject(HttpClient);
    private readonly signalR = inject(SignalRService);
    private readonly base = `${environment.apiUrl}/alerts`;

    getMyAlerts(): Observable<AlertRule[]> {
        return this.http.get<AlertRule[]>(this.base);
    }

    createAlert(request: CreateAlertRequest): Observable<AlertRule> {
        return this.http.post<AlertRule>(this.base, request);
    }

    toggleAlert(alertId: number, isActive: boolean): Observable<AlertRule> {
        return this.http.put<AlertRule>(`${this.base}/${alertId}`, { isActive });
    }

    deleteAlert(alertId: number): Observable<void> {
        return this.http.delete<void>(`${this.base}/${alertId}`);
    }

    /** Subscribe to real-time alert notifications pushed via SignalR. */
    onAlertTriggered(callback: (n: AlertTriggeredNotification) => void): void {
        this.signalR.on<AlertTriggeredNotification>('AlertTriggered', callback);
    }

    offAlertTriggered(): void {
        this.signalR.off('AlertTriggered');
    }
}
