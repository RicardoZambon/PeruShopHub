import { Component, EventEmitter, HostListener, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LucideAngularModule, X } from 'lucide-angular';

@Component({
  selector: 'app-dialog',
  standalone: true,
  imports: [CommonModule, LucideAngularModule],
  templateUrl: './dialog.component.html',
  styleUrl: './dialog.component.scss',
})
export class DialogComponent {
  @Input({ required: true }) title!: string;
  @Input() open = false;
  @Input() size: 'sm' | 'md' | 'lg' = 'sm';
  @Output() closed = new EventEmitter<void>();

  readonly closeIcon = X;

  @HostListener('document:keydown.escape')
  onEscape(): void {
    if (this.open) {
      this.close();
    }
  }

  onBackdropClick(event: MouseEvent): void {
    if ((event.target as HTMLElement).classList.contains('dialog-overlay')) {
      this.close();
    }
  }

  close(): void {
    this.closed.emit();
  }
}
