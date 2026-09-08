import { Injectable, signal } from '@angular/core';
import { THEME_KEY } from './api';

export type Theme = 'dark' | 'light';

/**
 * Tema `index.html` içindeki küçük betikle, uygulama açılmadan önce kök öğeye
 * yazılıyor. Bu servis yalnızca o değeri okuyup değiştiriyor; ilk boyamayı o betik
 * yaptığı için burada bir başlangıç ataması yok.
 */
@Injectable({ providedIn: 'root' })
export class ThemeService {
  readonly theme = signal<Theme>(this.read());

  toggle(): void {
    this.set(this.theme() === 'dark' ? 'light' : 'dark');
  }

  set(theme: Theme): void {
    this.theme.set(theme);
    document.documentElement.setAttribute('data-theme', theme);

    try {
      localStorage.setItem(THEME_KEY, theme);
    } catch {
      /* gizli sekmede yazılamıyor; tema yalnızca bu sekmede geçerli kalır. */
    }
  }

  private read(): Theme {
    return document.documentElement.getAttribute('data-theme') === 'light'
      ? 'light'
      : 'dark';
  }
}
