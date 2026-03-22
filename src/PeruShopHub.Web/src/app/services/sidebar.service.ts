import { Injectable, signal, computed } from '@angular/core';

const STORAGE_KEY = 'sidebar-collapsed';

@Injectable({ providedIn: 'root' })
export class SidebarService {
  readonly collapsed = signal<boolean>(this.loadState());
  readonly mobileOpen = signal(false);

  toggle(): void {
    this.collapsed.update((v) => !v);
    localStorage.setItem(STORAGE_KEY, String(this.collapsed()));
  }

  collapse(): void {
    this.collapsed.set(true);
    localStorage.setItem(STORAGE_KEY, 'true');
  }

  expand(): void {
    this.collapsed.set(false);
    localStorage.setItem(STORAGE_KEY, 'false');
  }

  toggleMobile(): void {
    this.mobileOpen.update((v) => !v);
  }

  closeMobile(): void {
    this.mobileOpen.set(false);
  }

  private loadState(): boolean {
    if (typeof window === 'undefined') return false;
    // Default: collapsed on tablet, expanded on desktop
    const stored = localStorage.getItem(STORAGE_KEY);
    if (stored !== null) return stored === 'true';
    return window.innerWidth < 1024;
  }
}
