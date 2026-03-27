import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { buildHttpParams } from '../shared/utils';
import { Observable, map } from 'rxjs';
import { PagedResult } from '../models/api.models';
import { environment } from '../../environments/environment';

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
  private readonly baseUrl = `${environment.apiUrl}/supplies`;

  list(params: { page?: number; pageSize?: number; search?: string; sortBy?: string; sortDir?: string } = {}): Observable<SupplyDto[]> {
    return this.http.get<PagedResult<SupplyDto>>(this.baseUrl, { params: buildHttpParams(params) }).pipe(
      map(result => result.items)
    );
  }

  create(dto: CreateSupplyDto): Observable<SupplyDto> {
    return this.http.post<SupplyDto>(this.baseUrl, dto);
  }

  update(id: string, dto: Partial<CreateSupplyDto>): Observable<SupplyDto> {
    return this.http.put<SupplyDto>(`${this.baseUrl}/${id}`, dto);
  }
}
