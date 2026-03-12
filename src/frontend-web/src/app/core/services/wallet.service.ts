import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface WalletResponse {
    balance: number;
    lockedAmount: number;
    availableBalance: number;
    lastUpdated: string;
}

@Injectable({ providedIn: 'root' })
export class WalletService {
    private readonly http = inject(HttpClient);
    private readonly apiUrl = `${environment.apiUrl}/wallets`;

    getWallet(): Observable<WalletResponse> {
        return this.http.get<WalletResponse>(this.apiUrl);
    }

    withdraw(amount: number): Observable<any> {
        return this.http.post(`${this.apiUrl}/withdraw`, { amount });
    }
}
