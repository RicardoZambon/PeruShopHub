import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { PagedResult } from '../models/api.models';

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

  list(params: {
    page?: number;
    pageSize?: number;
    status?: string;
    supplier?: string;
    sortBy?: string;
    sortDir?: string;
  } = {}): Observable<PagedResult<PurchaseOrderListItem>> {
    let httpParams = new HttpParams();
    if (params.page) httpParams = httpParams.set('page', params.page);
    if (params.pageSize) httpParams = httpParams.set('pageSize', params.pageSize);
    if (params.status) httpParams = httpParams.set('status', params.status);
    if (params.supplier) httpParams = httpParams.set('supplier', params.supplier);
    if (params.sortBy) httpParams = httpParams.set('sortBy', params.sortBy);
    if (params.sortDir) httpParams = httpParams.set('sortDir', params.sortDir);
    return this.http.get<PagedResult<PurchaseOrderListItem>>('/api/purchase-orders', { params: httpParams });
  }

  getById(id: string): Observable<PurchaseOrderDetail> {
    return this.http.get<PurchaseOrderDetail>(`/api/purchase-orders/${id}`);
  }

  create(dto: CreatePurchaseOrderDto): Observable<PurchaseOrderDetail> {
    return this.http.post<PurchaseOrderDetail>('/api/purchase-orders', dto);
  }

  update(id: string, dto: Partial<CreatePurchaseOrderDto>): Observable<PurchaseOrderDetail> {
    return this.http.put<PurchaseOrderDetail>(`/api/purchase-orders/${id}`, dto);
  }

  receive(id: string): Observable<PurchaseOrderDetail> {
    return this.http.post<PurchaseOrderDetail>(`/api/purchase-orders/${id}/receive`, {});
  }

  addCost(poId: string, cost: { description: string; value: number; distributionMethod: string }): Observable<POCost> {
    return this.http.post<POCost>(`/api/purchase-orders/${poId}/costs`, cost);
  }

  removeCost(poId: string, costId: string): Observable<void> {
    return this.http.delete<void>(`/api/purchase-orders/${poId}/costs/${costId}`);
  }

  previewCost(poId: string, value: number, method: string): Observable<CostPreview> {
    const params = new HttpParams().set('value', value).set('method', method);
    return this.http.get<CostPreview>(`/api/purchase-orders/${poId}/cost-preview`, { params });
  }
}
