import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { PagedResult } from '../models/api.models';
import { buildHttpParams } from '../shared/utils';
import { environment } from '../../environments/environment';

export interface AuditLogItem {
  id: string;
  userId: string;
  userName: string;
  action: string;
  entityType: string;
  entityId: string | null;
  oldValue: string | null;
  newValue: string | null;
  createdAt: string;
}

export interface AuditLogFilters {
  page?: number;
  pageSize?: number;
  entityType?: string;
  entityId?: string;
  dateFrom?: string;
  dateTo?: string;
  userId?: string;
}

@Injectable({ providedIn: 'root' })
export class AuditLogService {
  private http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/audit-log`;

  list(filters: AuditLogFilters = {}): Observable<PagedResult<AuditLogItem>> {
    return this.http.get<PagedResult<AuditLogItem>>(this.baseUrl, {
      params: buildHttpParams(filters),
    });
  }
}
