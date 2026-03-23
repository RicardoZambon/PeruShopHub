import { Injectable, inject, signal, computed, OnDestroy } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Subscription } from 'rxjs';
import { environment } from '../../environments/environment';
import { SignalRService, type SignalRNotification } from './signalr.service';

export interface Notification {
  id: string;
  type: 'sale' | 'question' | 'stock' | 'margin' | 'connection';
  title: string;
  description: string;
  timestamp: Date;
  isRead: boolean;
  navigationTarget?: string;
}

@Injectable({ providedIn: 'root' })
export class NotificationService {
  private readonly http = inject(HttpClient);
  private readonly signalRService = inject(SignalRService);
  private readonly baseUrl = `${environment.apiUrl}/notifications`;
  private signalRSub: Subscription | null = null;

  readonly notifications = signal<Notification[]>([]);
  readonly unreadCount = computed(() => this.notifications().filter(n => !n.isRead).length);

  constructor() {
    this.loadNotifications();
    this.signalRService.start();
    this.signalRSub = this.signalRService.notifications$.subscribe((n: SignalRNotification) => {
      const notification: Notification = {
        id: n.id,
        type: n.type,
        title: n.title,
        description: n.description,
        timestamp: new Date(n.timestamp),
        isRead: n.isRead,
        navigationTarget: n.navigationTarget,
      };
      this.notifications.update(list => [notification, ...list]);
    });
  }

  markAsRead(id: string): void {
    this.notifications.update(list =>
      list.map(n => n.id === id ? { ...n, isRead: true } : n)
    );
    this.http.patch(`${this.baseUrl}/${id}/read`, {}).subscribe({
      error: (err) => console.error('Failed to mark notification as read:', err),
    });
  }

  markAllAsRead(): void {
    this.notifications.update(list =>
      list.map(n => ({ ...n, isRead: true }))
    );
    this.http.patch(`${this.baseUrl}/read-all`, {}).subscribe({
      error: (err) => console.error('Failed to mark all notifications as read:', err),
    });
  }

  private loadNotifications(): void {
    this.http.get<Notification[]>(this.baseUrl).subscribe({
      next: (data) => {
        const notifications = data.map(n => ({
          ...n,
          timestamp: new Date(n.timestamp),
        }));
        this.notifications.set(notifications);
      },
      error: (err) => console.error('Failed to load notifications:', err),
    });
  }
}
