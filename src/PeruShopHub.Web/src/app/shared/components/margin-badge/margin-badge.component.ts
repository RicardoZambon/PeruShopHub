import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-margin-badge',
  standalone: true,
  imports: [CommonModule],
  template: `<span class="margin-badge" [ngClass]="cssClass">{{ display }}</span>`,
  styles: [`
    .margin-badge {
      display: inline-block;
      padding: 2px 8px;
      border-radius: var(--radius-sm);
      font-family: var(--font-mono);
      font-size: var(--body-small-size);
      font-weight: 600;
    }

    .margin-badge--success {
      background: var(--success-light);
      color: var(--success);
    }

    .margin-badge--warning {
      background: var(--warning-light);
      color: var(--warning);
    }

    .margin-badge--danger {
      background: var(--danger-light);
      color: var(--danger);
    }
  `],
})
export class MarginBadgeComponent {
  @Input() margin: number | null = null;

  get display(): string {
    return (this.margin ?? 0).toFixed(1) + '%';
  }

  get cssClass(): string {
    const value = this.margin ?? 0;
    if (value >= 20) return 'margin-badge--success';
    if (value >= 10) return 'margin-badge--warning';
    return 'margin-badge--danger';
  }
}
