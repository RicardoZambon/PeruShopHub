import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-empty-state',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="empty-state">
      <div class="empty-state__icon" *ngIf="icon">{{ icon }}</div>
      <h3 class="empty-state__title">{{ title }}</h3>
      <p class="empty-state__description">{{ description }}</p>
      <button
        class="empty-state__action"
        *ngIf="actionLabel"
        (click)="actionClick.emit()"
      >
        {{ actionLabel }}
      </button>
    </div>
  `,
  styleUrl: './empty-state.component.scss',
})
export class EmptyStateComponent {
  @Input({ required: true }) title!: string;
  @Input({ required: true }) description!: string;
  @Input() icon?: string;
  @Input() actionLabel?: string;
  @Output() actionClick = new EventEmitter<void>();
}
