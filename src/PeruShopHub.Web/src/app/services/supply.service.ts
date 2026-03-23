import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
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
  status: 'Ativo' | 'Inativo';
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

export interface SupplyListParams {
  search?: string;
  categoria?: string;
  page?: number;
  pageSize?: number;
}

@Injectable({ providedIn: 'root' })
export class SupplyService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/supplies`;

  list(params?: SupplyListParams): Observable<SupplyDto[]> {
    let httpParams = new HttpParams();
    if (params?.search) httpParams = httpParams.set('search', params.search);
    if (params?.categoria && params.categoria !== 'Todas') {
      httpParams = httpParams.set('categoria', params.categoria);
    }
    if (params?.page) httpParams = httpParams.set('page', params.page.toString());
    if (params?.pageSize) httpParams = httpParams.set('pageSize', params.pageSize.toString());
    return this.http.get<SupplyDto[]>(this.baseUrl, { params: httpParams });
  }

  create(dto: CreateSupplyDto): Observable<SupplyDto> {
    return this.http.post<SupplyDto>(this.baseUrl, dto);
  }

  update(id: string, dto: Partial<CreateSupplyDto>): Observable<SupplyDto> {
    return this.http.put<SupplyDto>(`${this.baseUrl}/${id}`, dto);
  }
}
