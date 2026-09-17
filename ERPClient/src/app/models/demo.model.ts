/** Demo oturumunun istemci tarafındaki görünümü. */
export interface DemoStatusModel {
  sessionId: string;
  workspaceName: string;
  writesUsed: number;
  writeLimit: number;
  nudgeAfterWrites: number;
  expiresAt: string;
  secondsRemaining: number;
  contactUrl: string;
  isActive: boolean;
  endReason: string | null;
}

export interface DemoStartModel {
  accessToken: string;
  status: DemoStatusModel;
}

export interface DemoConfigModel {
  enabled: boolean;

  /** Açıkken ziyaretçiden önce e-posta adresi ve kod isteniyor. */
  emailVerificationRequired: boolean;
}

/**
 * Kod isteğinin sonucu. alreadyVerified true ise kod gönderilmedi ve gerekmiyor:
 * adres yakın zamanda doğrulanmış, kod adımı atlanır.
 */
export interface DemoCodeResultModel {
  message: string;
  alreadyVerified: boolean;
}

/**
 * Sunucunun demoya özel retleri. Normal hata gövdesinden `demoCode` alanıyla
 * ayrılıyor; istemci bunu görünce oturum penceresini açıyor.
 */
export type DemoErrorCode = 'session_ended' | 'write_limit' | 'action_blocked';

/** Oturum penceresinin hangi sebeple açıldığı. */
export type DemoPromptKind = 'nudge' | 'ended';
