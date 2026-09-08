import { HttpErrorResponse, HttpEvent, HttpInterceptorFn, HttpResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, tap, throwError } from 'rxjs';
import { DemoErrorCode } from '../models/demo.model';
import { AuthService } from './auth.service';
import { DemoService } from './demo.service';

const WRITE_ACTIONS = ['/create', '/update', '/deletebyid', '/requirementsplanningbyorderid'];

const isWrite = (url: string): boolean => {
  const path = url.split('?')[0].toLocaleLowerCase('en');
  return WRITE_ACTIONS.some((action) => path.endsWith(action));
};

/**
 * Her isteğe bearer jetonunu ekler ve demo kotası göstergesini sunucunun gerçekten
 * kaydettiği değerle eşler; böylece şerittekiler sunucudaki sayaçtan sapamıyor.
 */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const demo = inject(DemoService);
  const token = auth.token;

  const request = token
    ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
    : req;

  return next(request).pipe(
    tap((event: HttpEvent<unknown>) => {
      if (event instanceof HttpResponse && demo.isDemo && isWrite(request.url)) {
        demo.refreshStatus();
      }
    }),
    catchError((error: HttpErrorResponse) => {
      const demoCode = error.error?.demoCode as DemoErrorCode | undefined;

      if (demoCode) demo.handleError(demoCode);

      return throwError(() => error);
    })
  );
};
