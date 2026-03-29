import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { buildHttpParams } from '../shared/utils';
import { environment } from '../../environments/environment';

export interface ClaimListItem {
  id: string;
  externalId: string;
  orderId: string | null;
  externalOrderId: string;
  type: string;
  status: string;
  reason: string;
  buyerName: string | null;
  productName: string | null;
  quantity: number;
  amount: number | null;
  createdAt: string;
  resolvedAt: string | null;
}

export interface ClaimDetail {
  id: string;
  externalId: string;
  orderId: string | null;
  externalOrderId: string;
  type: string;
  status: string;
  reason: string;
  buyerComment: string | null;
  sellerComment: string | null;
  buyerName: string | null;
  resolution: string | null;
  productId: string | null;
  productName: string | null;
  quantity: number;
  amount: number | null;
  createdAt: string;
  resolvedAt: string | null;
  updatedAt: string;
}

export interface ClaimListResponse {
  items: ClaimListItem[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface ClaimListParams {
  status?: string;
  type?: string;
  page?: number;
  pageSize?: number;
}

export interface ClaimSummary {
  openCount: number;
  closedCount: number;
  returnRate: number;
}

@Injectable({ providedIn: 'root' })
export class ClaimService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/claims`;

  list(params: ClaimListParams = {}): Observable<ClaimListResponse> {
    return this.http.get<ClaimListResponse>(this.baseUrl, { params: buildHttpParams(params) });
  }

  get(id: string): Observable<ClaimDetail> {
    return this.http.get<ClaimDetail>(`${this.baseUrl}/${id}`);
  }

  respond(id: string, sellerComment: string): Observable<ClaimDetail> {
    return this.http.post<ClaimDetail>(`${this.baseUrl}/${id}/respond`, { sellerComment });
  }

  getSummary(): Observable<ClaimSummary> {
    return this.http.get<ClaimSummary>(`${this.baseUrl}/summary`);
  }
}
