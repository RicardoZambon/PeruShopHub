import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { buildHttpParams } from '../shared/utils';
import { Observable } from 'rxjs';
import { PagedResult } from '../models/api.models';
import { environment } from '../../environments/environment';

export interface PurchaseOrderListItem {
  id: string;
  supplier: string | null;
  status: string;
  itemCount: number;
  total: number;
  createdAt: string;
  receivedAt: string | null;
}

export interface PurchaseOrderDetail {
  id: string;
  supplier: string | null;
  status: string;
  notes: string | null;
  subtotal: number;
  additionalCosts: number;
  total: number;
  version: number;
  createdAt: string;
  receivedAt: string | null;
  items: POItem[];
  costs: POCost[];
}

export interface POItem {
  id: string;
  productId: string;
  variantId: string;
  productName: string;
  sku: string;
  quantity: number;
  unitCost: number;
  totalCost: number;
  allocatedAdditionalCost: number;
  effectiveUnitCost: number;
}

export interface POCost {
  id: string;
  description: string;
  value: number;
  distributionMethod: string;
}

export interface CostPreview {
  allocations: {
    itemId: string;
    productName: string;
    sku: string;
    allocatedAmount: number;
    effectiveUnitCost: number;
  }[];
}

export interface CreatePurchaseOrderDto {
  supplier: string;
  notes?: string;
  items: {
    productId: string;
    variantId: string;
    quantity: number;
    unitCost: number;
  }[];
  costs?: {
    description: string;
    value: number;
    distributionMethod: string;
  }[];
}

@Injectable({ providedIn: 'root' })
export class PurchaseOrderService {
  private http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/purchase-orders`;

  list(params: {
    page?: number;
    pageSize?: number;
    status?: string;
    supplier?: string;
    sortBy?: string;
    sortDir?: string;
  } = {}): Observable<PagedResult<PurchaseOrderListItem>> {
    return this.http.get<PagedResult<PurchaseOrderListItem>>(this.baseUrl, { params: buildHttpParams(params) });
  }

  getById(id: string): Observable<PurchaseOrderDetail> {
    return this.http.get<PurchaseOrderDetail>(`${this.baseUrl}/${id}`);
  }

  create(dto: CreatePurchaseOrderDto): Observable<PurchaseOrderDetail> {
    return this.http.post<PurchaseOrderDetail>(this.baseUrl, dto);
  }

  update(id: string, dto: Partial<CreatePurchaseOrderDto> & { version: number }): Observable<PurchaseOrderDetail> {
    return this.http.put<PurchaseOrderDetail>(`${this.baseUrl}/${id}`, dto);
  }

  receive(id: string): Observable<PurchaseOrderDetail> {
    return this.http.post<PurchaseOrderDetail>(`${this.baseUrl}/${id}/receive`, {});
  }

  addCost(poId: string, cost: { description: string; value: number; distributionMethod: string }): Observable<POCost> {
    return this.http.post<POCost>(`${this.baseUrl}/${poId}/costs`, cost);
  }

  removeCost(poId: string, costId: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${poId}/costs/${costId}`);
  }

  previewCost(poId: string, value: number, method: string): Observable<CostPreview> {
    return this.http.get<CostPreview>(`${this.baseUrl}/${poId}/cost-preview`, { params: buildHttpParams({ value, method }) });
  }

  cancel(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }
}
