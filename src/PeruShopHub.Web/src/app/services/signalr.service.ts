import { Injectable, signal } from '@angular/core';
import { Subject, Observable } from 'rxjs';
import * as signalR from '@microsoft/signalr';
import { environment } from '../../environments/environment';

export interface SignalRNotification {
  id: string;
  type: 'sale' | 'question' | 'stock' | 'margin' | 'connection';
  title: string;
  description: string;
  timestamp: string;
  isRead: boolean;
  navigationTarget?: string;
}

@Injectable({ providedIn: 'root' })
export class SignalRService {
  private hubConnection: signalR.HubConnection | null = null;

  private readonly notificationsSubject = new Subject<SignalRNotification>();
  private readonly dataChangedSubject = new Subject<string>();

  readonly notifications$: Observable<SignalRNotification> = this.notificationsSubject.asObservable();
  readonly dataChanged$: Observable<string> = this.dataChangedSubject.asObservable();

  readonly connected = signal(false);

  start(): void {
    if (this.hubConnection) return;

    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl(`${environment.apiUrl}/hubs/notifications`)
      .withAutomaticReconnect()
      .build();

    this.hubConnection.on('ReceiveNotification', (notification: SignalRNotification) => {
      this.notificationsSubject.next(notification);
    });

    this.hubConnection.on('DataChanged', (entity: string) => {
      this.dataChangedSubject.next(entity);
    });

    this.hubConnection.onclose(() => this.connected.set(false));
    this.hubConnection.onreconnected(() => this.connected.set(true));

    this.hubConnection
      .start()
      .then(() => this.connected.set(true))
      .catch((err) => console.error('SignalR connection error:', err));
  }

  stop(): void {
    if (this.hubConnection) {
      this.hubConnection.stop();
      this.hubConnection = null;
      this.connected.set(false);
    }
  }
}
