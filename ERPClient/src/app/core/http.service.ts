import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, catchError, map, throwError } from 'rxjs';
import { ResultModel } from '../models/auth.model';
import { api } from './api';
import { ErrorService } from './error.service';

/**
 * API çağrılarının tek kapısı. Sunucu her yanıtı `Result<T>` ile sarmalıyor;
 * burada sarmalayıcı açılıyor ve hata bildirimi tek yerden veriliyor.
 *
 * Bearer başlığını `authInterceptor` ekliyor, bu yüzden burada başlıkla ilgili
 * bir iş yok.
 */
@Injectable({ providedIn: 'root' })
export class HttpService {
  private readonly http = inject(HttpClient);
  private readonly error = inject(ErrorService);

  /**
   * Geri çağrı imzası, ekranların çoğunun tek ihtiyacı olduğu için korunuyor:
   * `post('Customers/GetAll', {}, res => ...)`.
   */
  post<T>(
    url: string,
    body: unknown,
    onSuccess: (data: T) => void,
    onError?: (error: HttpErrorResponse) => void
  ): void {
    this.request<T>(url, body).subscribe({
      next: onSuccess,
      error: (error: HttpErrorResponse) => onError?.(error),
    });
  }

  /** Aynı çağrının akış hâli: birden fazla isteğin birlikte beklenmesi gerektiğinde. */
  request<T>(url: string, body: unknown = {}): Observable<T> {
    return this.http.post<ResultModel<T>>(`${api()}/${url}`, body).pipe(
      map((result) => result.data as T),
      catchError((error: HttpErrorResponse) => {
        this.error.handle(error);
        return throwError(() => error);
      })
    );
  }
}
