import { Injectable, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { JwtPayload, jwtDecode } from 'jwt-decode';
import { UserModel } from '../models/auth.model';
import { DEMO_FLAG_KEY, TOKEN_KEY } from './api';

interface ErpJwtPayload extends JwtPayload {
  Id?: string;
  Name?: string;
  Email?: string;
  UserName?: string;
  DemoSessionId?: string;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly router = inject(Router);

  readonly user = signal<UserModel>(new UserModel());

  /** Rayın altındaki kullanıcı kutusunda gösterilen baş harfler. */
  readonly initials = computed(() => {
    const name = this.user().name?.trim();
    if (!name) return 'TZ';

    return name
      .split(/\s+/)
      .slice(0, 2)
      .map((part) => part.charAt(0).toLocaleUpperCase('tr'))
      .join('');
  });

  get token(): string {
    try {
      return localStorage.getItem(TOKEN_KEY) ?? '';
    } catch {
      return '';
    }
  }

  store(token: string, isDemo = false): void {
    try {
      localStorage.setItem(TOKEN_KEY, token);

      if (isDemo) localStorage.setItem(DEMO_FLAG_KEY, 'true');
      else localStorage.removeItem(DEMO_FLAG_KEY);
    } catch {
      /* gizli sekme: oturum yalnızca bu sayfa ömrü boyunca yaşar. */
    }

    this.readUser(token);
  }

  clear(): void {
    try {
      localStorage.removeItem(TOKEN_KEY);
      localStorage.removeItem(DEMO_FLAG_KEY);
    } catch {
      /* yoksayılır */
    }

    this.user.set(new UserModel());
  }

  /** Yol koruması. Jeton yoksa ya da süresi geçtiyse giriş ekranına gönderir. */
  isAuthenticated(): boolean {
    const token = this.token;

    if (!token) {
      this.router.navigateByUrl('/giris');
      return false;
    }

    const payload = this.decode(token);
    const expiry = payload?.exp;

    // Bozuk jeton çözülemiyor; onu geçerli saymak, her isteğin 401 dönmesine ve
    // kullanıcının boş ekranda kalmasına yol açıyordu.
    if (!payload || (expiry !== undefined && Date.now() / 1000 > expiry)) {
      this.clear();
      this.router.navigateByUrl('/giris');
      return false;
    }

    this.apply(payload);

    return true;
  }

  private readUser(token: string): void {
    const payload = this.decode(token);
    if (payload) this.apply(payload);
  }

  private apply(payload: ErpJwtPayload): void {
    this.user.set({
      id: payload.Id ?? '',
      name: payload.Name ?? '',
      email: payload.Email ?? '',
      userName: payload.UserName ?? '',
    });
  }

  private decode(token: string): ErpJwtPayload | null {
    try {
      return jwtDecode<ErpJwtPayload>(token);
    } catch {
      return null;
    }
  }
}
