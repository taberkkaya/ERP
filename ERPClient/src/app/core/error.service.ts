import { HttpErrorResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { ToastService } from './toast.service';

@Injectable({ providedIn: 'root' })
export class ErrorService {
  private readonly toast = inject(ToastService);

  handle(error: HttpErrorResponse): void {
    // Demoya özel retler kendi penceresini açıyor; ayrıca bildirim göstermek
    // aynı şeyi iki kez söylemek olurdu.
    if (error.error?.demoCode) return;

    this.toast.error(this.messageOf(error));
  }

  /**
   * Sunucu hata gövdesini iki ayrı yerden üretiyor: ExceptionHandler'ın elle
   * serileştirdiği yanıt PascalCase, denetleyicilerden dönen Result camelCase.
   * İkisi de okunuyor.
   */
  private messageOf(error: HttpErrorResponse): string {
    if (error.status === 0) {
      return 'Sunucuya ulaşılamadı. Bağlantınızı kontrol edin.';
    }

    const body = error.error ?? {};
    const messages: unknown = body.errorMessages ?? body.ErrorMessages;

    if (Array.isArray(messages) && messages.length) {
      return messages.filter(Boolean).join(' · ');
    }

    if (typeof messages === 'string' && messages.trim()) {
      return messages;
    }

    if (error.status === 401) return 'Oturumunuz geçersiz. Yeniden giriş yapın.';
    if (error.status === 404) return 'Kayıt bulunamadı.';

    return 'Beklenmeyen bir hata oluştu.';
  }
}
