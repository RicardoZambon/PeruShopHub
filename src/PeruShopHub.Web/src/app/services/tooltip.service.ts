import { Injectable, signal } from '@angular/core';

const STORAGE_KEY = 'psh_dismissed_tooltips';

@Injectable({ providedIn: 'root' })
export class TooltipService {
  private dismissed = signal<Set<string>>(this.loadDismissed());

  isDismissed(tooltipId: string): boolean {
    return this.dismissed().has(tooltipId);
  }

  dismiss(tooltipId: string): void {
    const current = new Set(this.dismissed());
    current.add(tooltipId);
    this.dismissed.set(current);
    this.saveDismissed(current);
  }

  resetAll(): void {
    this.dismissed.set(new Set());
    localStorage.removeItem(STORAGE_KEY);
  }

  private loadDismissed(): Set<string> {
    try {
      const stored = localStorage.getItem(STORAGE_KEY);
      return stored ? new Set(JSON.parse(stored)) : new Set();
    } catch {
      return new Set();
    }
  }

  private saveDismissed(dismissed: Set<string>): void {
    localStorage.setItem(STORAGE_KEY, JSON.stringify([...dismissed]));
  }
}
