import { environment } from '../../environments/environment';

let resolvedApiUrl = environment.apiUrl;

/**
 * API'nin kök adresi. Sabit değil fonksiyon: panel üzerinden kurulan bir yayında
 * (sunucuda derleme adımı yok) istemcinin başka bir API adresine bakabilmesi için
 * değerin çalışma zamanında değişebilmesi gerekiyor.
 */
export const api = (): string => resolvedApiUrl;

/** Açılışta `assets/config.json` dosyasından uygulanır; boş değer derlemedeki adresi korur. */
export function setApiUrl(url: string | undefined | null): void {
  const trimmed = url?.trim();
  if (!trimmed) return;

  resolvedApiUrl = trimmed.replace(/\/+$/, '');
}

/**
 * Yapılandırmayı uygulama açılmadan önce okur. Dosya yoksa ya da bozuksa sessizce
 * derlemedeki adresle devam edilir: yapılandırma dosyası olmayan bir kurulum da
 * çalışmalı.
 */
export async function loadRuntimeConfig(): Promise<void> {
  try {
    const response = await fetch('assets/config.json', { cache: 'no-store' });
    if (!response.ok) return;

    const config = (await response.json()) as { apiUrl?: string };
    setApiUrl(config.apiUrl);
  } catch {
    /* yapılandırma isteğe bağlı */
  }
}

export const TOKEN_KEY = 'tz-token';
export const DEMO_FLAG_KEY = 'tz-demo';
export const THEME_KEY = 'tz-theme';
