import { Component, inject, output, HostListener } from '@angular/core';
import { Router } from '@angular/router';
import {
  LucideAngularModule,
  X,
  ShoppingCart,
  MessageCircle,
  AlertTriangle,
  TrendingDown,
  WifiOff,
  type LucideIconData,
} from 'lucide-angular';
import { NotificationService, type Notification } from '../../../services/notification.service';
import { RelativeDatePipe } from '../../pipes/relative-date.pipe';

@Component({
  selector: 'app-notification-panel',
  standalone: true,
  imports: [LucideAngularModule, RelativeDatePipe],
  templateUrl: './notification-panel.component.html',
  styleUrl: './notification-panel.component.scss',
})
export class NotificationPanelComponent {
  readonly notifications = inject(NotificationService);
  private readonly router = inject(Router);
  readonly closed = output<void>();

  readonly closeIcon = X;

  private readonly iconMap: Record<Notification['type'], LucideIconData> = {
    sale: ShoppingCart,
    question: MessageCircle,
    stock: AlertTriangle,
    margin: TrendingDown,
    connection: WifiOff,
  };

  getIcon(type: Notification['type']): LucideIconData {
    return this.iconMap[type];
  }

  onClose(): void {
    this.closed.emit();
  }

  onBackdropClick(): void {
    this.closed.emit();
  }

  markAllRead(): void {
    this.notifications.markAllAsRead();
  }

  onNotificationClick(notification: Notification): void {
    this.notifications.markAsRead(notification.id);
    if (notification.navigationTarget) {
      console.log('Navigate to:', notification.navigationTarget);
      this.router.navigateByUrl(notification.navigationTarget);
    }
    this.closed.emit();
  }

  @HostListener('document:keydown.escape')
  onEsc(): void {
    this.closed.emit();
  }
}
