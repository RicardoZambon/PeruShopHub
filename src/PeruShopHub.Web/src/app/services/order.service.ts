import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { buildHttpParams } from '../shared/utils';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface OrderListItem {
  id: string;
  date: string;
  buyer: string;
  status: string;
  itemCount: number;
  total: number;
  profit: number;
  margin: number;
  marketplace: string;
}

export interface OrderListResponse {
  items: OrderListItem[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface OrderListParams {
  search?: string;
  status?: string;
  dateFrom?: string;
  dateTo?: string;
  page?: number;
  pageSize?: number;
}

export interface OrderDetail {
  id: string;
  externalOrderId: string;
  buyer: { name: string; nickname?: string; email?: string; phone?: string };
  itemCount: number;
  totalAmount: number;
  revenue: number;
  totalCosts: number;
  profit: number;
  margin: number;
  status: string;
  isFulfilled: boolean;
  fulfilledAt?: string;
  orderDate: string;
  shipping: { trackingNumber?: string; carrier?: string; logisticType?: string; timeline?: { status: string; timestamp?: string; description?: string }[] };
  payment: { method?: string; installments?: number; amount?: number; status?: string };
  items: { id: string; productId?: string; name: string; sku: string; variation?: string; quantity: number; unitPrice: number; subtotal: number }[];
  costs: { id: string; category: string; description?: string; value: number; source: string }[];
}

export interface CreateCostRequest {
  category: string;
  description?: string;
  value: number;
}

export interface UpdateCostRequest {
  category: string;
  description?: string;
  value: number;
}

export interface OrderCostResponse {
  id: string;
  category: string;
  description?: string;
  value: number;
  source: string;
}

@Injectable({ providedIn: 'root' })
export class OrderService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/orders`;

  list(params: OrderListParams = {}): Observable<OrderListResponse> {
    return this.http.get<OrderListResponse>(this.baseUrl, { params: buildHttpParams(params) });
  }

  getById(id: string): Observable<OrderDetail> {
    return this.http.get<OrderDetail>(`${this.baseUrl}/${id}`);
  }

  addCost(orderId: string, request: CreateCostRequest): Observable<OrderCostResponse> {
    return this.http.post<OrderCostResponse>(`${this.baseUrl}/${orderId}/costs`, request);
  }

  updateCost(orderId: string, costId: string, request: UpdateCostRequest): Observable<OrderCostResponse> {
    return this.http.put<OrderCostResponse>(`${this.baseUrl}/${orderId}/costs/${costId}`, request);
  }

  deleteCost(orderId: string, costId: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${orderId}/costs/${costId}`);
  }

  recalculateCosts(orderId: string): Observable<any> {
    return this.http.post(`${this.baseUrl}/${orderId}/recalculate-costs`, {});
  }

  fulfill(orderId: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${orderId}/fulfill`, {});
  }
}
