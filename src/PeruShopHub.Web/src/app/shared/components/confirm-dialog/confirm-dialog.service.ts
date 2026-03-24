import { Injectable, signal } from '@angular/core';

export interface ConfirmOptions {
  title?: string;
  message: string;
  confirmLabel?: string;
  cancelLabel?: string;
  variant?: 'danger' | 'warning' | 'primary';
}

@Injectable({ providedIn: 'root' })
export class ConfirmDialogService {
  readonly open = signal(false);
  readonly processing = signal(false);
  readonly options = signal<ConfirmOptions>({ message: '' });

  private resolveFn: ((value: boolean) => void) | null = null;

  confirm(options: ConfirmOptions | string): Promise<boolean> {
    const opts = typeof options === 'string'
      ? { message: options }
      : options;

    this.options.set({
      title: opts.title ?? 'Confirmar',
      message: opts.message,
      confirmLabel: opts.confirmLabel ?? 'Confirmar',
      cancelLabel: opts.cancelLabel ?? 'Cancelar',
      variant: opts.variant ?? 'danger',
    });
    this.processing.set(false);
    this.open.set(true);

    return new Promise<boolean>(resolve => {
      this.resolveFn = resolve;
    });
  }

  /** User clicked confirm — resolve promise but keep dialog open for processing */
  accept(): void {
    this.processing.set(true);
    this.resolveFn?.(true);
    this.resolveFn = null;
  }

  cancel(): void {
    if (this.processing()) return;
    this.open.set(false);
    this.processing.set(false);
    this.resolveFn?.(false);
    this.resolveFn = null;
  }

  /** Call after async work completes to close the dialog */
  done(): void {
    this.open.set(false);
    this.processing.set(false);
  }
}
