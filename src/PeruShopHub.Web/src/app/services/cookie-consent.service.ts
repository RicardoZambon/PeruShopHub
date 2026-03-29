import { Injectable, signal, computed } from '@angular/core';

export interface CookieConsent {
  essential: true; // always active
  analytics: boolean;
  version: number;
  timestamp: string;
}

const STORAGE_KEY = 'cookie-consent';
const COOKIE_KEY = 'cookie_consent_given';
const CONSENT_VERSION = 1;

@Injectable({ providedIn: 'root' })
export class CookieConsentService {
  readonly consent = signal<CookieConsent | null>(this.loadConsent());

  readonly hasConsented = computed(() => {
    const c = this.consent();
    return c !== null && c.version === CONSENT_VERSION;
  });

  readonly showBanner = computed(() => !this.hasConsented());

  readonly analyticsEnabled = computed(() => {
    const c = this.consent();
    return c?.analytics ?? false;
  });

  acceptAll(): void {
    this.saveConsent({ essential: true, analytics: true });
  }

  acceptEssentialOnly(): void {
    this.saveConsent({ essential: true, analytics: false });
  }

  saveCustom(analytics: boolean): void {
    this.saveConsent({ essential: true, analytics });
  }

  resetConsent(): void {
    if (typeof window === 'undefined') return;
    localStorage.removeItem(STORAGE_KEY);
    this.removeCookie();
    this.consent.set(null);
  }

  private saveConsent(partial: Omit<CookieConsent, 'version' | 'timestamp'>): void {
    const consent: CookieConsent = {
      ...partial,
      version: CONSENT_VERSION,
      timestamp: new Date().toISOString(),
    };
    if (typeof window !== 'undefined') {
      localStorage.setItem(STORAGE_KEY, JSON.stringify(consent));
      this.setCookie();
    }
    this.consent.set(consent);
  }

  private loadConsent(): CookieConsent | null {
    if (typeof window === 'undefined') return null;
    try {
      const stored = localStorage.getItem(STORAGE_KEY);
      if (!stored) return null;
      const parsed = JSON.parse(stored) as CookieConsent;
      if (parsed.version !== CONSENT_VERSION) return null;
      return parsed;
    } catch {
      return null;
    }
  }

  private setCookie(): void {
    const expires = new Date();
    expires.setFullYear(expires.getFullYear() + 1);
    document.cookie = `${COOKIE_KEY}=true; path=/; expires=${expires.toUTCString()}; SameSite=Lax`;
  }

  private removeCookie(): void {
    document.cookie = `${COOKIE_KEY}=; path=/; expires=Thu, 01 Jan 1970 00:00:00 GMT`;
  }
}
