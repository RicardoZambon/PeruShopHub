import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { PagedResult } from '../models/api.models';

export interface SupplyDto {
  id: string;
  nome: string;
  sku: string;
  categoria: string;
  custoUnitario: number;
  estoque: number;
  estoqueMinimo: number;
  fornecedor: string;
  status: string;
  observacao?: string;
}

export interface CreateSupplyDto {
  nome: string;
  sku: string;
  categoria: string;
  custoUnitario: number;
  estoque: number;
  estoqueMinimo: number;
  fornecedor: string;
  observacao?: string;
}

@Injectable({ providedIn: 'root' })
export class SupplyService {
  private http = inject(HttpClient);

  list(params: { page?: number; pageSize?: number; search?: string; sortBy?: string; sortDir?: string } = {}): Observable<SupplyDto[]> {
    let httpParams = new HttpParams();
    if (params.page) httpParams = httpParams.set('page', params.page);
    if (params.pageSize) httpParams = httpParams.set('pageSize', params.pageSize);
    if (params.search) httpParams = httpParams.set('search', params.search);
    if (params.sortBy) httpParams = httpParams.set('sortBy', params.sortBy);
    if (params.sortDir) httpParams = httpParams.set('sortDir', params.sortDir);
    return this.http.get<PagedResult<SupplyDto>>('/api/supplies', { params: httpParams }).pipe(
      map(result => result.items)
    );
  }

  create(dto: CreateSupplyDto): Observable<SupplyDto> {
    return this.http.post<SupplyDto>('/api/supplies', dto);
  }

  update(id: string, dto: Partial<CreateSupplyDto>): Observable<SupplyDto> {
    return this.http.put<SupplyDto>(`/api/supplies/${id}`, dto);
  }
}
