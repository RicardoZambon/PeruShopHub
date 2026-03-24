import { Component, HostListener, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ConfirmDialogService } from './confirm-dialog.service';

@Component({
  selector: 'app-confirm-dialog',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './confirm-dialog.component.html',
  styleUrl: './confirm-dialog.component.scss',
})
export class ConfirmDialogComponent {
  readonly confirmService = inject(ConfirmDialogService);

  @HostListener('document:keydown.escape')
  onEscape(): void {
    if (this.confirmService.open() && !this.confirmService.processing()) {
      this.confirmService.cancel();
    }
  }

  onBackdropClick(event: MouseEvent): void {
    if (this.confirmService.processing()) return;
    if ((event.target as HTMLElement).classList.contains('confirm-overlay')) {
      this.confirmService.cancel();
    }
  }
}
