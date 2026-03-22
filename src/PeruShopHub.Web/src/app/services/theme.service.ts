import { Injectable, signal, effect, computed } from '@angular/core';

export type ThemePreference = 'light' | 'dark' | 'system';

const STORAGE_KEY = 'theme-preference';

@Injectable({ providedIn: 'root' })
export class ThemeService {
  readonly currentTheme = signal<ThemePreference>(this.loadPreference());

  readonly resolvedTheme = computed<'light' | 'dark'>(() => {
    const pref = this.currentTheme();
    if (pref === 'system') {
      return this.systemPrefersDark() ? 'dark' : 'light';
    }
    return pref;
  });

  private systemPrefersDark = signal(
    typeof window !== 'undefined'
      ? window.matchMedia('(prefers-color-scheme: dark)').matches
      : false
  );

  constructor() {
    // Listen for OS theme changes
    if (typeof window !== 'undefined') {
      const mq = window.matchMedia('(prefers-color-scheme: dark)');
      mq.addEventListener('change', (e) => {
        this.systemPrefersDark.set(e.matches);
      });
    }

    // Apply theme whenever resolvedTheme changes
    effect(() => {
      const resolved = this.resolvedTheme();
      this.applyTheme(resolved);
    });
  }

  setTheme(theme: ThemePreference): void {
    this.currentTheme.set(theme);
    localStorage.setItem(STORAGE_KEY, theme);
  }

  toggleTheme(): void {
    const order: ThemePreference[] = ['light', 'dark', 'system'];
    const current = this.currentTheme();
    const nextIndex = (order.indexOf(current) + 1) % order.length;
    this.setTheme(order[nextIndex]);
  }

  private loadPreference(): ThemePreference {
    if (typeof window === 'undefined') return 'system';
    const stored = localStorage.getItem(STORAGE_KEY);
    if (stored === 'light' || stored === 'dark' || stored === 'system') {
      return stored;
    }
    return 'system';
  }

  private applyTheme(theme: 'light' | 'dark'): void {
    if (typeof document === 'undefined') return;
    if (theme === 'dark') {
      document.documentElement.setAttribute('data-theme', 'dark');
    } else {
      document.documentElement.removeAttribute('data-theme');
    }
  }
}
