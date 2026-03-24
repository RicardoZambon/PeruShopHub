import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-form-actions',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './form-actions.component.html',
  styleUrl: './form-actions.component.scss',
})
export class FormActionsComponent {
  @Input() saveLabel = 'Salvar';
  @Input() cancelLabel = 'Cancelar';
  @Input() saving = false;
  @Input() disabled = false;
  @Output() save = new EventEmitter<void>();
  @Output() cancel = new EventEmitter<void>();
}
