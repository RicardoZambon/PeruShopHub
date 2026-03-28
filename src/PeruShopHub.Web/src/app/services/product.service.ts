import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { buildHttpParams } from '../shared/utils';
import { Observable, firstValueFrom } from 'rxjs';
import { environment } from '../../environments/environment';
import { PagedResult } from '../models/api.models';

export interface Product {
  id: string;
  name: string;
  sku: string;
  description?: string;
  categoryId?: string;
  supplier?: string;
  price: number;
  acquisitionCost: number;
  purchaseCost: number;
  packagingCost: number;
  storageCostDaily?: number | null;
  weight?: number;
  height?: number;
  width?: number;
  length?: number;
  imageUrl: string | null;
  photoUrls?: string[];
  stock: number;
  status: string;
  isActive: boolean;
  margin: number | null;
  variantCount: number;
  needsReview: boolean;
  abcClass?: string | null;
  minStock?: number | null;
  maxStock?: number | null;
  variants?: any[];
  version: number;
  createdAt?: string;
  updatedAt?: string;
}

export interface CostHistoryItem {
  id: string;
  date: string;
  previousCost: number;
  newCost: number;
  quantity: number;
  unitCostPaid: number;
  purchaseOrderId: string | null;
  reason: string;
}

export interface ProductListParams {
  page?: number;
  pageSize?: number;
  search?: string;
  status?: string;
  categoryId?: string;
  sortBy?: string;
  sortDirection?: 'asc' | 'desc';
}

export interface ProductAnalytics {
  totalSales: number;
  totalRevenue: number;
  totalProfit: number;
  margin: number | null;
  salesChange: number | null;
  revenueChange: number | null;
  profitChange: number | null;
  marginChange: number | null;
}

@Injectable({ providedIn: 'root' })
export class ProductService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/products`;

  async list(params: ProductListParams = {}): Promise<PagedResult<Product>> {
    const { sortDirection, ...rest } = params;
    return firstValueFrom(
      this.http.get<PagedResult<Product>>(this.baseUrl, {
        params: buildHttpParams({ ...rest, sortDir: sortDirection }),
      }),
    );
  }

  async getById(id: string): Promise<Product> {
    return firstValueFrom(
      this.http.get<Product>(`${this.baseUrl}/${id}`),
    );
  }

  async create(dto: Partial<Product>): Promise<Product> {
    return firstValueFrom(
      this.http.post<Product>(this.baseUrl, dto),
    );
  }

  async update(id: string, dto: Partial<Product> & { version: number }): Promise<Product> {
    return firstValueFrom(
      this.http.put<Product>(`${this.baseUrl}/${id}`, dto),
    );
  }

  async delete(id: string): Promise<void> {
    await firstValueFrom(
      this.http.delete<void>(`${this.baseUrl}/${id}`),
    );
  }

  async getNextSku(categoryId: string): Promise<string | null> {
    const result = await firstValueFrom(
      this.http.get<{ suggestedSku: string | null }>(
        `${this.baseUrl}/next-sku`,
        { params: buildHttpParams({ categoryId }) },
      ),
    );
    return result.suggestedSku;
  }

  getVariants(id: string): Observable<any[]> {
    return this.http.get<any[]>(`${this.baseUrl}/${id}/variants`);
  }

  getCostHistory(id: string, page = 1, pageSize = 20): Observable<PagedResult<CostHistoryItem>> {
    return this.http.get<PagedResult<CostHistoryItem>>(`${this.baseUrl}/${id}/cost-history`, { params: buildHttpParams({ page, pageSize }) });
  }

  async getAnalytics(id: string, days = 30): Promise<ProductAnalytics> {
    return firstValueFrom(
      this.http.get<ProductAnalytics>(`${this.baseUrl}/${id}/analytics`, { params: buildHttpParams({ days }) }),
    );
  }

  getRecentOrders(id: string, days = 30, page = 1, pageSize = 10): Observable<PagedResult<any>> {
    return this.http.get<PagedResult<any>>(`${this.baseUrl}/${id}/recent-orders`, { params: buildHttpParams({ days, page, pageSize }) });
  }
}
