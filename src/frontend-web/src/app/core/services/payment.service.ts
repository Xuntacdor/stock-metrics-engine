import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface CreateDepositRequest {
    amount: number;
    returnUrl: string;
    cancelUrl: string;
}

export interface CreateDepositResponse {
    depositId: number;
    orderCode: number;
    amount: number;
    checkoutUrl: string;
    status: string;
    createdAt: string;
}

export interface DepositDetailResponse {
    depositId: number;
    userId: string;
    orderCode: number;
    amount: number;
    status: string;
    checkoutUrl: string;
    createdAt: string;
    paidAt?: string;
}

@Injectable({ providedIn: 'root' })
export class PaymentService {
    private readonly http = inject(HttpClient);
    private readonly apiUrl = `${environment.apiUrl}/payments`;

    createDeposit(request: CreateDepositRequest): Observable<CreateDepositResponse> {
        return this.http.post<CreateDepositResponse>(`${this.apiUrl}/deposit`, request);
    }

    getDepositHistory(): Observable<DepositDetailResponse[]> {
        return this.http.get<DepositDetailResponse[]>(`${this.apiUrl}/deposits`);
    }

    cancelDeposit(orderCode: number): Observable<any> {
        return this.http.get(`${this.apiUrl}/cancel?orderCode=${orderCode}`);
    }
}
