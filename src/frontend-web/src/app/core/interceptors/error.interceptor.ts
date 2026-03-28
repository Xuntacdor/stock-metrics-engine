import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { ToastService } from '../services/toast.service';

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
    const toast = inject(ToastService);

    return next(req).pipe(
        catchError((err: HttpErrorResponse) => {
            // 401 is already handled by authInterceptor (token refresh / redirect)
            if (err.status === 401) return throwError(() => err);

            if (err.status === 0) {
                toast.error('Không thể kết nối máy chủ. Vui lòng kiểm tra kết nối mạng.');
            } else if (err.status === 403) {
                toast.error('Bạn không có quyền thực hiện hành động này.');
            } else if (err.status === 404) {
                // Intentionally silent — 404 is often expected (e.g. empty portfolio)
            } else if (err.status >= 500) {
                toast.error('Lỗi máy chủ nội bộ. Vui lòng thử lại sau.');
            } else if (err.status >= 400) {
                const message = err.error?.message
                    ?? err.error?.title
                    ?? 'Yêu cầu không hợp lệ. Vui lòng thử lại.';
                toast.error(message);
            }

            return throwError(() => err);
        }),
    );
};
