import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface PlaceOrderRequest {
    symbol: string;
    side: 'BUY' | 'SELL';
    orderType: 'MARKET' | 'LIMIT';
    quantity: number;
    price: number;
}

export interface OrderResponse {
    orderId: string;
    symbol: string;
    side: string;
    orderType: string;
    status: string;
    requestQty: number;
    matchedQty: number | null;
    price: number;
    avgMatchedPrice: number | null;
    createdAt: string | null;
}

@Injectable({ providedIn: 'root' })
export class OrderService {
    private readonly http = inject(HttpClient);
    private readonly base = `${environment.apiUrl}/orders`;

    placeOrder(request: PlaceOrderRequest): Observable<OrderResponse> {
        return this.http.post<OrderResponse>(this.base, request);
    }

    getMyOrders(): Observable<OrderResponse[]> {
        return this.http.get<OrderResponse[]>(this.base);
    }

    cancelOrder(orderId: string): Observable<void> {
        return this.http.delete<void>(`${this.base}/${orderId}`);
    }
}
