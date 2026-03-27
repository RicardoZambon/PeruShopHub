import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { buildHttpParams } from '../shared/utils';
import { Observable } from 'rxjs';
import { KpiCard, ChartDataPoint, SkuProfitability, ReconciliationRow, AbcProduct } from '../models/api.models';
import { environment } from '../../environments/environment';

@Injectable({ providedIn: 'root' })
export class FinanceService {
  private http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/finance`;

  getSummary(period: string): Observable<KpiCard[]> {
    return this.http.get<KpiCard[]>(`${this.baseUrl}/summary`, { params: buildHttpParams({ period }) });
  }

  getRevenueProfit(days: number): Observable<ChartDataPoint[]> {
    return this.http.get<ChartDataPoint[]>(`${this.baseUrl}/chart/revenue-profit`, { params: buildHttpParams({ days }) });
  }

  getMarginChart(days: number): Observable<ChartDataPoint[]> {
    return this.http.get<ChartDataPoint[]>(`${this.baseUrl}/chart/margin`, { params: buildHttpParams({ days }) });
  }

  getSkuProfitability(params: { period?: string; page?: number; pageSize?: number; search?: string; sortBy?: string; sortDir?: string } = {}): Observable<SkuProfitability[]> {
    return this.http.get<SkuProfitability[]>(`${this.baseUrl}/sku-profitability`, { params: buildHttpParams(params) });
  }

  getReconciliation(year: number): Observable<ReconciliationRow[]> {
    return this.http.get<ReconciliationRow[]>(`${this.baseUrl}/reconciliation`, { params: buildHttpParams({ year }) });
  }

  getAbcCurve(): Observable<AbcProduct[]> {
    return this.http.get<AbcProduct[]>(`${this.baseUrl}/abc-curve`);
  }
}
