import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../environments/environment';
import { PagedResult } from '../models/api.models';
import { buildHttpParams } from '../shared/utils';

export interface ListingGridItem {
  id: string;
  marketplaceId: string;
  externalId: string;
  productId: string | null;
  productName: string | null;
  title: string;
  status: string;
  price: number;
  permalink: string | null;
  thumbnailUrl: string | null;
  availableQuantity: number;
  syncStatus: string;
  updatedAt: string;
}

export interface ListingListParams {
  search?: string;
  status?: string;
  syncStatus?: string;
  sortBy?: string;
  sortDirection?: string;
  page?: number;
  pageSize?: number;
}

@Injectable({ providedIn: 'root' })
export class ListingService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/integrations/mercadolivre`;

  async list(params: ListingListParams = {}): Promise<PagedResult<ListingGridItem>> {
    const httpParams = buildHttpParams(params);
    return firstValueFrom(
      this.http.get<PagedResult<ListingGridItem>>(`${this.baseUrl}/listings`, { params: httpParams })
    );
  }
}
