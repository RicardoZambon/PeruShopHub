import { Injectable, inject, signal } from '@angular/core';
import { Subject } from 'rxjs';
import * as signalR from '@microsoft/signalr';
import { environment } from '../../environments/environment';
import { AuthService } from './auth.service';

export interface SignalRNotification {
  id: string;
  type: 'sale' | 'question' | 'stock' | 'margin' | 'connection';
  title: string;
  description: string;
  timestamp: string;
  isRead: boolean;
  navigationTarget?: string;
}

export interface DataChangeEvent {
  entity: string;
  action: string;
  id?: string;
}

@Injectable({ providedIn: 'root' })
export class SignalRService {
  private readonly auth = inject(AuthService);
  private connection: signalR.HubConnection | null = null;
  private readonly _notifications$ = new Subject<any>();
  private readonly _dataChanged$ = new Subject<DataChangeEvent>();

  readonly notifications$ = this._notifications$.asObservable();
  readonly dataChanged$ = this._dataChanged$.asObservable();
  readonly connected = signal(false);

  start(): void {
    if (this.connection) return;
    if (!this.auth.isAuthenticated) return;

    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(environment.hubUrl, {
        accessTokenFactory: () => this.auth.accessToken ?? '',
      })
      .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
      .build();

    this.connection.on('ReceiveNotification', (n: any) => this._notifications$.next(n));
    this.connection.on('DataChanged', (e: DataChangeEvent) => this._dataChanged$.next(e));
    this.connection.onreconnected(() => this.connected.set(true));
    this.connection.onclose(() => this.connected.set(false));

    this.connection.start()
      .then(() => this.connected.set(true))
      .catch(err => console.warn('SignalR connection failed:', err));
  }

  stop(): void {
    this.connection?.stop();
    this.connection = null;
    this.connected.set(false);
  }
}
