import { Injectable, signal } from '@angular/core';

export type ToastType = 'success' | 'warning' | 'danger' | 'info';

export interface Toast {
  id: number;
  message: string;
  type: ToastType;
  description?: string;
  duration: number;
}

const MAX_VISIBLE = 3;

@Injectable({ providedIn: 'root' })
export class ToastService {
  readonly toasts = signal<Toast[]>([]);

  private nextId = 0;
  private timers = new Map<number, ReturnType<typeof setTimeout>>();

  show(message: string, type: ToastType = 'info', duration = 5000, description?: string): void {
    const id = this.nextId++;
    const toast: Toast = { id, message, type, duration, description };

    this.toasts.update((current) => {
      const updated = [...current, toast];
      // Keep only the last MAX_VISIBLE toasts
      if (updated.length > MAX_VISIBLE) {
        const removed = updated.splice(0, updated.length - MAX_VISIBLE);
        removed.forEach((t) => this.clearTimer(t.id));
      }
      return updated;
    });

    if (duration > 0) {
      const timer = setTimeout(() => this.dismiss(id), duration);
      this.timers.set(id, timer);
    }
  }

  dismiss(id: number): void {
    this.clearTimer(id);
    this.toasts.update((current) => current.filter((t) => t.id !== id));
  }

  private clearTimer(id: number): void {
    const timer = this.timers.get(id);
    if (timer) {
      clearTimeout(timer);
      this.timers.delete(id);
    }
  }
}
