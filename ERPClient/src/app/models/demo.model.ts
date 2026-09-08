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
}

/**
 * Sunucunun demoya özel retleri. Normal hata gövdesinden `demoCode` alanıyla
 * ayrılıyor; istemci bunu görünce oturum penceresini açıyor.
 */
export type DemoErrorCode = 'session_ended' | 'write_limit' | 'action_blocked';

/** Oturum penceresinin hangi sebeple açıldığı. */
export type DemoPromptKind = 'nudge' | 'ended';
