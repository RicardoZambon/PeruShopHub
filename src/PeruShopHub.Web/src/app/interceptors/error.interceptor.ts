import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { ToastService } from '../services/toast.service';

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const toast = inject(ToastService);
  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status === 0) {
        toast.show('Erro de conexão com o servidor', 'danger');
      } else if (error.status === 409) {
        toast.show('Este registro foi modificado por outro usuário. Recarregue a página.', 'warning');
      } else if (error.status >= 500) {
        toast.show('Erro interno do servidor', 'danger');
      } else if (error.status >= 400 && error.status !== 401 && error.status !== 404) {
        const msg = error.error?.message || error.error?.title || 'Erro na requisição';
        toast.show(msg, 'warning');
      }
      return throwError(() => error);
    })
  );
};
