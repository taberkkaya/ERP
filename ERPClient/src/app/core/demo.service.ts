import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, tap } from 'rxjs';
import { ResultModel } from '../models/auth.model';
import {
  DemoConfigModel,
  DemoErrorCode,
  DemoPromptKind,
  DemoStartModel,
  DemoStatusModel,
} from '../models/demo.model';
import { DEMO_FLAG_KEY, api } from './api';
import { AuthService } from './auth.service';

const FALLBACK_CONTACT = 'https://ataberkkaya.com';

/**
 * Demo oturumunun istemci tarafı: kotayı ve kalan süreyi taşır, sunucu demoya
 * özel bir retle cevap verdiğinde oturum penceresini açar.
 */
@Injectable({ providedIn: 'root' })
export class DemoService {
  private readonly http = inject(HttpClient);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  readonly status = signal<DemoStatusModel | null>(null);
  readonly prompt = signal<DemoPromptKind | null>(null);
  readonly secondsRemaining = signal(0);
  readonly starting = signal(false);

  readonly writesLeft = computed(() => {
    const status = this.status();
    return status ? Math.max(0, status.writeLimit - status.writesUsed) : 0;
  });

  /** Kota çubuğunun doluluk oranı. */
  readonly writeRatio = computed(() => {
    const status = this.status();
    if (!status || status.writeLimit <= 0) return 0;

    return Math.min(100, Math.round((status.writesUsed / status.writeLimit) * 100));
  });

  readonly contactUrl = computed(() => {
    // ?? değil: sunucu boş metin gönderebiliyor, o da boş bir sekme açardı.
    const url = this.status()?.contactUrl?.trim();
    return url ? url : FALLBACK_CONTACT;
  });

  /** Oturum başına bir kez: iletişim daveti tekrar tekrar gösterilmesin. */
  private nudged = false;
  private ticker?: ReturnType<typeof setInterval>;

  get isDemo(): boolean {
    try {
      return localStorage.getItem(DEMO_FLAG_KEY) === 'true';
    } catch {
      return false;
    }
  }

  config(): Observable<ResultModel<DemoConfigModel>> {
    return this.http.get<ResultModel<DemoConfigModel>>(`${api()}/demo/config`);
  }

  start(): Observable<ResultModel<DemoStartModel>> {
    this.starting.set(true);

    return this.http.post<ResultModel<DemoStartModel>>(`${api()}/demo/start`, {}).pipe(
      tap({
        next: (result) => {
          this.starting.set(false);
          if (result.data) this.adopt(result.data);
        },
        error: () => this.starting.set(false),
      })
    );
  }

  /** Sandbox'ı siler ve ziyaretçiye uygulamadan çıkmadan yenisini verir. */
  reset(): Observable<ResultModel<DemoStartModel>> {
    this.starting.set(true);

    return this.http.post<ResultModel<DemoStartModel>>(`${api()}/demo/reset`, {}).pipe(
      tap({
        next: (result) => {
          this.starting.set(false);
          if (result.data) this.adopt(result.data);
        },
        error: () => this.starting.set(false),
      })
    );
  }

  refreshStatus(): void {
    if (!this.isDemo) return;

    this.http.get<ResultModel<DemoStatusModel>>(`${api()}/demo/status`).subscribe({
      next: (result) => {
        if (result.data) this.applyStatus(result.data);
      },
      // Sonlanmış oturumu zaten araya giren katman bildiriyor.
      error: () => {},
    });
  }

  /** Sunucu demoya özel bir retle cevap verdiğinde araya giren katman çağırır. */
  handleError(code: DemoErrorCode): void {
    if (code === 'action_blocked') return;

    this.stopTicker();
    this.prompt.set('ended');
  }

  dismissPrompt(): void {
    this.prompt.set(null);
  }

  openContactPage(): void {
    window.open(this.resolvedContactUrl(), '_blank', 'noopener');
  }

  /** Demodan çıkıp giriş ekranına döner. */
  exit(): void {
    if (this.isDemo) {
      this.http.post(`${api()}/demo/end`, {}).subscribe({ next: () => {}, error: () => {} });
    }

    this.clear();
    this.router.navigateByUrl('/giris');
  }

  clear(): void {
    this.stopTicker();
    this.auth.clear();
    this.status.set(null);
    this.prompt.set(null);
    this.nudged = false;
  }

  /**
   * Yapılandırmaya düz bir e-posta adresi de yazılabiliyor; şema eklenmezse
   * tarayıcı onu göreli bir yol sanıp bozuk bir sekme açıyor.
   */
  private resolvedContactUrl(): string {
    const value = this.contactUrl();

    if (/^[a-z][a-z0-9+.-]*:/i.test(value)) return value;
    if (value.includes('@')) return `mailto:${value}`;

    return `https://${value}`;
  }

  private adopt(start: DemoStartModel): void {
    this.auth.store(start.accessToken, true);
    this.nudged = false;
    this.prompt.set(null);
    this.applyStatus(start.status);
  }

  private applyStatus(status: DemoStatusModel): void {
    this.status.set(status);
    this.secondsRemaining.set(status.secondsRemaining);
    this.startTicker();

    const shouldNudge =
      !this.nudged &&
      status.writesUsed >= status.nudgeAfterWrites &&
      status.writesUsed < status.writeLimit;

    if (shouldNudge) {
      this.nudged = true;
      this.prompt.set('nudge');
    }
  }

  private startTicker(): void {
    if (this.ticker) return;

    this.ticker = setInterval(() => {
      const remaining = this.secondsRemaining() - 1;
      this.secondsRemaining.set(Math.max(0, remaining));

      if (remaining <= 0) {
        this.stopTicker();
        this.prompt.set('ended');
      }
    }, 1000);
  }

  private stopTicker(): void {
    if (!this.ticker) return;

    clearInterval(this.ticker);
    this.ticker = undefined;
  }
}
