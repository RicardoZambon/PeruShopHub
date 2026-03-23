import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { KpiCard, ChartDataPoint, SkuProfitability, ReconciliationRow, AbcProduct } from '../models/api.models';

@Injectable({ providedIn: 'root' })
export class FinanceService {
  private http = inject(HttpClient);

  getSummary(period: string): Observable<KpiCard[]> {
    const params = new HttpParams().set('period', period);
    return this.http.get<KpiCard[]>('/api/finance/summary', { params });
  }

  getRevenueProfit(days: number): Observable<ChartDataPoint[]> {
    const params = new HttpParams().set('days', days);
    return this.http.get<ChartDataPoint[]>('/api/finance/chart/revenue-profit', { params });
  }

  getMarginChart(days: number): Observable<ChartDataPoint[]> {
    const params = new HttpParams().set('days', days);
    return this.http.get<ChartDataPoint[]>('/api/finance/chart/margin', { params });
  }

  getSkuProfitability(params: { period?: string; page?: number; pageSize?: number; search?: string; sortBy?: string; sortDir?: string } = {}): Observable<SkuProfitability[]> {
    let httpParams = new HttpParams();
    if (params.period) httpParams = httpParams.set('period', params.period);
    if (params.page) httpParams = httpParams.set('page', params.page);
    if (params.pageSize) httpParams = httpParams.set('pageSize', params.pageSize);
    if (params.search) httpParams = httpParams.set('search', params.search);
    if (params.sortBy) httpParams = httpParams.set('sortBy', params.sortBy);
    if (params.sortDir) httpParams = httpParams.set('sortDir', params.sortDir);
    return this.http.get<SkuProfitability[]>('/api/finance/sku-profitability', { params: httpParams });
  }

  getReconciliation(year: number): Observable<ReconciliationRow[]> {
    const params = new HttpParams().set('year', year);
    return this.http.get<ReconciliationRow[]>('/api/finance/reconciliation', { params });
  }

  getAbcCurve(): Observable<AbcProduct[]> {
    return this.http.get<AbcProduct[]>('/api/finance/abc-curve');
  }
}
