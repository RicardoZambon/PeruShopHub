import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-error-state',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="error-state">
      <div class="error-state__icon">⚠</div>
      <h3 class="error-state__title">{{ title }}</h3>
      <p class="error-state__description">{{ description }}</p>
      <button class="error-state__retry" (click)="retry.emit()">
        Tentar novamente
      </button>
    </div>
  `,
  styleUrl: './error-state.component.scss',
})
export class ErrorStateComponent {
  @Input() title = 'Algo deu errado';
  @Input() description = 'Ocorreu um erro inesperado. Tente novamente.';
  @Output() retry = new EventEmitter<void>();
}
