import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ToastService, type ToastType } from '../../../services/toast.service';
import { LucideAngularModule, CheckCircle, AlertTriangle, XCircle, Info, X, type LucideIconData } from 'lucide-angular';

@Component({
  selector: 'app-toast-container',
  standalone: true,
  imports: [CommonModule, LucideAngularModule],
  template: `
    <div class="toast-container">
      @for (toast of toastService.toasts(); track toast.id) {
        <div class="toast toast--{{ toast.type }}" role="alert">
          <div class="toast__icon">
            <lucide-icon [img]="getIcon(toast.type)" [size]="18"></lucide-icon>
          </div>
          <div class="toast__content">
            <span class="toast__message">{{ toast.message }}</span>
            @if (toast.description) {
              <span class="toast__description">{{ toast.description }}</span>
            }
          </div>
          <button class="toast__close" (click)="toastService.dismiss(toast.id)" aria-label="Fechar">
            <lucide-icon [img]="xIcon" [size]="14"></lucide-icon>
          </button>
        </div>
      }
    </div>
  `,
  styleUrl: './toast-container.component.scss',
})
export class ToastContainerComponent {
  readonly toastService = inject(ToastService);

  readonly xIcon = X;

  private readonly icons: Record<ToastType, LucideIconData> = {
    success: CheckCircle,
    warning: AlertTriangle,
    danger: XCircle,
    info: Info,
  };

  getIcon(type: ToastType): LucideIconData {
    return this.icons[type];
  }
}
