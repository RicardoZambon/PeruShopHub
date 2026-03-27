import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface TenantDetail {
  id: string;
  name: string;
  slug: string;
  isActive: boolean;
  memberCount: number;
  createdAt: string;
}

export interface TenantMember {
  id: string;
  name: string;
  email: string;
  role: string;
  isActive: boolean;
  lastLogin: string | null;
  createdAt: string;
}

@Injectable({ providedIn: 'root' })
export class TenantService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/tenant`;

  getTenant(): Observable<TenantDetail> {
    return this.http.get<TenantDetail>(this.baseUrl);
  }

  updateTenant(name: string): Observable<TenantDetail> {
    return this.http.put<TenantDetail>(this.baseUrl, { name });
  }

  getMembers(): Observable<TenantMember[]> {
    return this.http.get<TenantMember[]>(`${this.baseUrl}/members`);
  }

  inviteMember(data: { name: string; email: string; password: string; role: string }): Observable<TenantMember> {
    return this.http.post<TenantMember>(`${this.baseUrl}/members/invite`, data);
  }

  updateMember(userId: string, data: { name: string; email: string; role: string }): Observable<TenantMember> {
    return this.http.put<TenantMember>(`${this.baseUrl}/members/${userId}`, data);
  }

  removeMember(userId: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/members/${userId}`);
  }

  resetPassword(userId: string, newPassword: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/members/${userId}/reset-password`, { newPassword });
  }
}
