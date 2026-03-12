import { Injectable, signal, computed, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, tap, catchError, throwError, BehaviorSubject, switchMap, filter, take } from 'rxjs';
import { environment } from '../../../environments/environment';


export interface RegisterRequest {
    username: string;
    email: string;
    password: string;
}

export interface LoginRequest {
    username: string;
    password: string;
}

export interface AuthResponse {
    token: string;
    refreshToken: string;
    userId: string;
    username: string;
    email?: string;
    expiresAt: string;
}


export interface AuthUser {
    userId: string;
    username: string;
    email?: string;
    token: string;
    expiresAt: Date;
}

const TOKEN_KEY = 'auth_token';
const USER_KEY = 'auth_user';

@Injectable({ providedIn: 'root' })
export class AuthService {
    private readonly http = inject(HttpClient);
    private readonly router = inject(Router);
    private readonly base = `${environment.apiUrl}/auth`;

    private readonly _user = signal<AuthUser | null>(this.loadUser());
    readonly user = this._user.asReadonly();
    readonly isLoggedIn = computed(() => !!this._user());
    readonly currentToken = computed(() => this._user()?.token ?? null);

    private _refreshing$ = new BehaviorSubject<boolean>(false);
    private _refresh$ = new BehaviorSubject<string | null>(null);


    register(payload: RegisterRequest): Observable<AuthResponse> {
        return this.http.post<AuthResponse>(`${this.base}/register`, payload).pipe(
            tap(res => this.saveSession(res)),
            catchError(err => throwError(() => this.extractError(err))),
        );
    }

    login(payload: LoginRequest): Observable<AuthResponse> {
        return this.http.post<AuthResponse>(`${this.base}/login`, payload).pipe(
            tap(res => this.saveSession(res)),
            catchError(err => throwError(() => this.extractError(err))),
        );
    }

    logout(): Observable<unknown> {
        return this.http.post(`${this.base}/logout`, {}).pipe(
            tap(() => this.clearSession()),
            catchError(() => { this.clearSession(); return throwError(() => new Error('Logout failed')); }),
        );
    }

    refreshToken(): Observable<string> {
        if (this._refreshing$.value) {
            return this._refresh$.pipe(
                filter(t => t !== null),
                take(1),
                switchMap(t => new Observable<string>(obs => { obs.next(t!); obs.complete(); })),
            );
        }

        this._refreshing$.next(true);

        return this.http.post<AuthResponse>(`${this.base}/refresh-token`, {}, { withCredentials: true }).pipe(
            tap(res => {
                this.saveSession(res);
                this._refresh$.next(res.token);
                this._refreshing$.next(false);
            }),
            switchMap(res => new Observable<string>(obs => { obs.next(res.token); obs.complete(); })),
            catchError(err => {
                this._refreshing$.next(false);
                this.clearSession();
                this.router.navigate(['/auth/login']);
                return throwError(() => err);
            }),
        );
    }


    private saveSession(res: AuthResponse): void {
        const user: AuthUser = {
            userId: res.userId,
            username: res.username,
            email: res.email,
            token: res.token,
            expiresAt: new Date(res.expiresAt),
        };
        localStorage.setItem(TOKEN_KEY, res.token);
        localStorage.setItem(USER_KEY, JSON.stringify(user));
        this._user.set(user);
    }

    private clearSession(): void {
        localStorage.removeItem(TOKEN_KEY);
        localStorage.removeItem(USER_KEY);
        this._user.set(null);
    }

    private loadUser(): AuthUser | null {
        try {
            const raw = localStorage.getItem(USER_KEY);
            if (!raw) return null;
            const u = JSON.parse(raw) as AuthUser;
            u.expiresAt = new Date(u.expiresAt);
            if (u.expiresAt < new Date()) { localStorage.clear(); return null; }
            return u;
        } catch { return null; }
    }

    private extractError(err: any): Error {
        const msg = err?.error?.message ?? err?.message ?? 'Có lỗi xảy ra, vui lòng thử lại.';
        return new Error(msg);
    }
}
