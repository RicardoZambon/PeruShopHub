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

export interface ReconciliationItem {
  variantId: string;
  countedQuantity: number;
}

export interface ReconciliationRequest {
  items: ReconciliationItem[];
}

export interface ReconciliationResultItem {
  variantId: string;
  sku: string;
  productName: string;
  systemQuantity: number;
  countedQuantity: number;
  difference: number;
  hasDiscrepancy: boolean;
}

export interface ReconciliationResult {
  batchId: string;
  itemsChecked: number;
  discrepancies: number;
  totalDifference: number;
  reconciliatedAt: string;
  items: ReconciliationResultItem[];
}

// ML Stock Reconciliation Report interfaces
export interface ReconciliationReport {
  id: string;
  marketplaceId: string;
  itemsChecked: number;
  matches: number;
  discrepancies: number;
  autoCorrected: number;
  manualReviewRequired: number;
  status: string;
  errorMessage?: string | null;
  startedAt: string;
  completedAt?: string | null;
}

export interface ReconciliationReportItem {
  id: string;
  productVariantId: string;
  sku: string;
  productName: string;
  externalId: string;
  localQuantity: number;
  marketplaceQuantity: number;
  difference: number;
  resolution: string;
  notes?: string | null;
}

export interface ReconciliationReportDetail extends ReconciliationReport {
  items: ReconciliationReportItem[];
}

export interface ReconciliationReportQueryParams {
  dateFrom?: string;
  dateTo?: string;
  page?: number;
  pageSize?: number;
}

// Fulfillment (ML Full) Stock interfaces
export interface FulfillmentStockItem {
  externalId: string;
  sku: string;
  productName: string;
  variantName?: string | null;
  availableQuantity: number;
  notAvailableQuantity?: number | null;
  warehouseId?: string | null;
  status?: string | null;
}

export interface ProductFulfillmentStock {
  productId: string;
  productName: string;
  sku: string;
  items: FulfillmentStockItem[];
  totalAvailable: number;
  totalNotAvailable: number;
}

export interface FulfillmentStockOverview {
  products: ProductFulfillmentStock[];
  totalProducts: number;
  totalAvailable: number;
  totalNotAvailable: number;
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

  reconcile(dto: ReconciliationRequest): Observable<ReconciliationResult> {
    return this.http.post<ReconciliationResult>(`${this.baseUrl}/reconciliation`, dto);
  }

  getReconciliationReports(params: ReconciliationReportQueryParams = {}): Observable<PagedResult<ReconciliationReport>> {
    return this.http.get<PagedResult<ReconciliationReport>>(`${this.baseUrl}/reconciliation-reports`, {
      params: buildHttpParams(params),
    });
  }

  getReconciliationReportDetail(reportId: string): Observable<ReconciliationReportDetail> {
    return this.http.get<ReconciliationReportDetail>(`${this.baseUrl}/reconciliation-reports/${reportId}`);
  }

  getFulfillmentStock(): Observable<FulfillmentStockOverview> {
    return this.http.get<FulfillmentStockOverview>(`${this.baseUrl}/fulfillment-stock`);
  }

  exportMovements(params: Omit<MovementQueryParams, 'page' | 'pageSize'>): Observable<Blob> {
    const { type, ...rest } = params;
    return this.http.get(`${this.baseUrl}/movements/export`, {
      params: buildHttpParams({ ...rest, type: type && type !== 'all' ? type : undefined }),
      responseType: 'blob',
    });
  }
}
