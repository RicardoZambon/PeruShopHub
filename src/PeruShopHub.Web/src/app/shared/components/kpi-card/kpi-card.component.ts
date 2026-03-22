import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-kpi-card',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="kpi-card">
      <span class="kpi-card__label">{{ label }}</span>
      <span class="kpi-card__value">{{ value }}</span>
      <div class="kpi-card__change" *ngIf="change !== undefined" [ngClass]="changeColorClass">
        <span class="kpi-card__arrow">{{ change >= 0 ? '↑' : '↓' }}</span>
        <span class="kpi-card__percentage">{{ formattedChange }}</span>
        <span class="kpi-card__change-label" *ngIf="changeLabel">{{ changeLabel }}</span>
      </div>
    </div>
  `,
  styleUrl: './kpi-card.component.scss',
})
export class KpiCardComponent {
  @Input({ required: true }) label!: string;
  @Input({ required: true }) value!: string;
  @Input() change?: number;
  @Input() changeLabel?: string;
  @Input() invertColors = false;

  get formattedChange(): string {
    if (this.change === undefined) return '';
    const abs = Math.abs(this.change);
    return `${abs.toFixed(1)}%`;
  }

  get changeColorClass(): string {
    if (this.change === undefined) return '';
    const isPositive = this.change >= 0;
    const isGood = this.invertColors ? !isPositive : isPositive;
    return isGood ? 'kpi-card__change--positive' : 'kpi-card__change--negative';
  }
}
