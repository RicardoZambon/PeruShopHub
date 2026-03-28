import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { buildHttpParams } from '../shared/utils';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { PagedResult } from '../models/api.models';

export interface InventoryItem {
  productId: string;
  sku: string;
  productName: string;
  totalStock: number;
  reserved: number;
  available: number;
  unitCost: number;
  stockValue: number;
  minStock?: number | null;
  maxStock?: number | null;
}

export interface StockAlert {
  productId: string;
  sku: string;
  productName: string;
  totalStock: number;
  minStock: number | null;
  deficit: number;
}

export interface StockAllocation {
  id: string;
  productVariantId: string;
  variantSku: string;
  marketplaceId: string;
  allocatedQuantity: number;
  reservedQuantity: number;
}

export interface VariantAllocations {
  variantId: string;
  variantSku: string;
  totalStock: number;
  totalAllocated: number;
  unallocated: number;
  allocations: StockAllocation[];
}

export interface ProductAllocations {
  productId: string;
  productName: string;
  variants: VariantAllocations[];
}

export interface UpdateStockAllocationDto {
  marketplaceId: string;
  allocatedQuantity: number;
}

export interface StockMovement {
  id: string;
  sku: string;
  productName: string;
  type: string;
  quantity: number;
  unitCost?: number | null;
  reason?: string | null;
  createdBy?: string | null;
  createdAt: string;
  purchaseOrderId?: string | null;
  orderId?: string | null;
}

export interface MovementQueryParams {
  productId?: string;
  variantId?: string;
  type?: string;
  dateFrom?: string;
  dateTo?: string;
  createdBy?: string;
  page?: number;
  pageSize?: number;
}

export interface InventoryQueryParams {
  page?: number;
  pageSize?: number;
  search?: string;
  sortBy?: string;
  sortDir?: string;
}

export interface StockAdjustmentDto {
  productId: string;
  variantId: string;
  quantity: number;
  reason: string;
}

@Injectable({ providedIn: 'root' })
export class InventoryService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/inventory`;

  getInventory(params: InventoryQueryParams = {}): Observable<PagedResult<InventoryItem>> {
    return this.http.get<PagedResult<InventoryItem>>(this.baseUrl, { params: buildHttpParams(params) });
  }

  getMovements(params: MovementQueryParams): Observable<PagedResult<StockMovement>> {
    const { type, ...rest } = params;
    return this.http.get<PagedResult<StockMovement>>(`${this.baseUrl}/movements`, {
      params: buildHttpParams({ ...rest, type: type && type !== 'all' ? type : undefined }),
    });
  }

  adjust(dto: StockAdjustmentDto): Observable<unknown> {
    return this.http.post(`${this.baseUrl}/adjust`, dto);
  }

  getAllocations(productId: string): Observable<ProductAllocations> {
    return this.http.get<ProductAllocations>(`${this.baseUrl}/${productId}/allocations`);
  }

  updateAllocation(variantId: string, dto: UpdateStockAllocationDto): Observable<StockAllocation> {
    return this.http.put<StockAllocation>(`${this.baseUrl}/${variantId}/allocations`, dto);
  }

  getAlerts(): Observable<StockAlert[]> {
    return this.http.get<StockAlert[]>(`${this.baseUrl}/alerts`);
  }

  exportMovements(params: Omit<MovementQueryParams, 'page' | 'pageSize'>): Observable<Blob> {
    const { type, ...rest } = params;
    return this.http.get(`${this.baseUrl}/movements/export`, {
      params: buildHttpParams({ ...rest, type: type && type !== 'all' ? type : undefined }),
      responseType: 'blob',
    });
  }
}
