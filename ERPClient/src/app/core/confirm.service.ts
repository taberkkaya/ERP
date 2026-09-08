import { Injectable, signal } from '@angular/core';

export interface ConfirmRequest {
  title: string;
  text: string;
  confirmLabel: string;
  danger: boolean;
}

/**
 * Silme gibi geri alınamayan işlemler için onay penceresi. Tek bir istek aynı anda
 * açık olabiliyor; ikinci bir çağrı beklemek yerine öncekinin yerini alıyor.
 */
@Injectable({ providedIn: 'root' })
export class ConfirmService {
  readonly request = signal<ConfirmRequest | null>(null);

  private resolver: ((confirmed: boolean) => void) | null = null;

  ask(
    title: string,
    text: string,
    { confirmLabel = 'Sil', danger = true } = {}
  ): Promise<boolean> {
    // Açık bir pencere varsa reddedilmiş sayılıyor, aksi hâlde çağıran taraf
    // sonsuza kadar bekleyen bir söz tutuyor.
    this.resolver?.(false);

    this.request.set({ title, text, confirmLabel, danger });

    return new Promise<boolean>((resolve) => {
      this.resolver = resolve;
    });
  }

  answer(confirmed: boolean): void {
    this.request.set(null);
    this.resolver?.(confirmed);
    this.resolver = null;
  }
}
