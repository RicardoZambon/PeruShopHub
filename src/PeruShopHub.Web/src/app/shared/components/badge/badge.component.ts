import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

export type BadgeVariant = 'success' | 'warning' | 'danger' | 'primary' | 'neutral' | 'accent';

@Component({
  selector: 'app-badge',
  standalone: true,
  imports: [CommonModule],
  template: `
    <span class="badge" [ngClass]="'badge--' + variant">{{ label }}</span>
  `,
  styleUrl: './badge.component.scss',
})
export class BadgeComponent {
  @Input({ required: true }) label!: string;
  @Input() variant: BadgeVariant = 'neutral';
}
