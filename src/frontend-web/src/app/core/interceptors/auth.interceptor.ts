import { HttpInterceptorFn, HttpRequest, HttpHandlerFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, switchMap, throwError } from 'rxjs';
import { AuthService } from '../services/auth.service';
import { Router } from '@angular/router';

export const authInterceptor: HttpInterceptorFn = (
    req: HttpRequest<unknown>,
    next: HttpHandlerFn,
) => {
    const authService = inject(AuthService);
    const router = inject(Router);

    const token = authService.currentToken();

    const authReq = token && !isAuthEndpoint(req.url)
        ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
        : req;

    return next(authReq).pipe(
        catchError((err: HttpErrorResponse) => {
            if (err.status === 401 && !isAuthEndpoint(req.url)) {
                return authService.refreshToken().pipe(
                    switchMap(newToken => {
                        const retryReq = req.clone({ setHeaders: { Authorization: `Bearer ${newToken}` } });
                        return next(retryReq);
                    }),
                    catchError(refreshErr => {
                        router.navigate(['/auth/login']);
                        return throwError(() => refreshErr);
                    }),
                );
            }
            return throwError(() => err);
        }),
    );
};

function isAuthEndpoint(url: string): boolean {
    return url.includes('/api/auth/');
}
