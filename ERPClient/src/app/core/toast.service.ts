import { Injectable, signal } from '@angular/core';

export type ToastKind = 'ok' | 'danger' | 'info' | 'warn';

export interface Toast {
  id: number;
  kind: ToastKind;
  text: string;
}

const ICON_BY_KIND: Record<ToastKind, string> = {
  ok: 'check-circle',
  danger: 'x-circle',
  info: 'info',
  warn: 'alert',
};

/**
 * Sağ alt köşedeki bildirimler. SweetAlert yerine kendi bileşenimiz duruyor:
 * uygulamanın kendi tasarım belirteçlerini kullanıyor ve yayına ayrı bir paket
 * binmiyor.
 */
@Injectable({ providedIn: 'root' })
export class ToastService {
  readonly toasts = signal<Toast[]>([]);

  private nextId = 1;

  ok(text: string): void {
    this.push('ok', text);
  }

  error(text: string): void {
    // Hata daha uzun duruyor: kullanıcı ne olduğunu okumadan kaybolmamalı.
    this.push('danger', text, 6000);
  }

  info(text: string): void {
    this.push('info', text);
  }

  warn(text: string): void {
    this.push('warn', text, 5000);
  }

  dismiss(id: number): void {
    this.toasts.update((list) => list.filter((toast) => toast.id !== id));
  }

  iconFor(kind: ToastKind): string {
    return ICON_BY_KIND[kind];
  }

  private push(kind: ToastKind, text: string, timeout = 3600): void {
    const id = this.nextId++;

    this.toasts.update((list) => [...list, { id, kind, text }]);

    setTimeout(() => this.dismiss(id), timeout);
  }
}
