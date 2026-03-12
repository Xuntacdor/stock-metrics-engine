import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface TransactionResponse {
    transId: number;
    refId: string;
    transType: string;
    amount: number;
    balanceBefore: number;
    balanceAfter: number;
    description: string;
    transTime: string;
}

@Injectable({ providedIn: 'root' })
export class TransactionService {
    private readonly http = inject(HttpClient);
    private readonly apiUrl = `${environment.apiUrl}/transactions`;

    getMyTransactions(type?: string): Observable<TransactionResponse[]> {
        const url = type ? `${this.apiUrl}?type=${type}` : this.apiUrl;
        return this.http.get<TransactionResponse[]>(url);
    }
}
