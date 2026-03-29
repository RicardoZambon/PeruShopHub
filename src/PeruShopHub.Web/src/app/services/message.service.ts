import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface MessageDto {
  id: string;
  externalPackId: string;
  orderId: string | null;
  senderType: string;
  text: string;
  sentAt: string;
  isRead: boolean;
}

export interface MessageThreadDto {
  externalPackId: string;
  orderId: string | null;
  messages: MessageDto[];
  unreadCount: number;
}

export interface UnreadCountDto {
  unreadCount: number;
}

@Injectable({ providedIn: 'root' })
export class MessageService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/messages`;

  getThread(orderId: string): Observable<MessageThreadDto> {
    return this.http.get<MessageThreadDto>(`${this.baseUrl}/orders/${orderId}`);
  }

  sendMessage(orderId: string, text: string): Observable<MessageDto> {
    return this.http.post<MessageDto>(`${this.baseUrl}/orders/${orderId}`, { text });
  }

  markAsRead(orderId: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/orders/${orderId}/read`, {});
  }

  getUnreadCount(): Observable<UnreadCountDto> {
    return this.http.get<UnreadCountDto>(`${this.baseUrl}/unread-count`);
  }
}
