import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { buildHttpParams } from '../shared/utils';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { KpiCard, ChartDataPoint, SkuProfitability, ReconciliationRow, AbcProduct, AbcProductApi } from '../models/api.models';
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

  getSkuProfitability(params: {
    period?: string; page?: number; pageSize?: number;
    search?: string; sortBy?: string; sortDir?: string;
    minMargin?: number; maxMargin?: number;
    dateFrom?: string; dateTo?: string;
  } = {}): Observable<SkuProfitability[]> {
    return this.http.get<SkuProfitability[]>(`${this.baseUrl}/sku-profitability`, { params: buildHttpParams(params) });
  }

  refreshSkuProfitability(): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.baseUrl}/sku-profitability/refresh`, {});
  }

  getReconciliation(year: number): Observable<ReconciliationRow[]> {
    return this.http.get<ReconciliationRow[]>(`${this.baseUrl}/reconciliation`, { params: buildHttpParams({ year }) });
  }

  getAbcCurve(params: { dateFrom?: string; dateTo?: string } = {}): Observable<AbcProduct[]> {
    return this.http.get<AbcProductApi[]>(`${this.baseUrl}/abc-curve`, {
      params: buildHttpParams(params),
    }).pipe(
      map((items) => {
        const totalRevenue = items.reduce((sum, i) => sum + i.revenue, 0);
        return items.map((item, index) => ({
          rank: index + 1,
          sku: item.sku,
          nome: item.name,
          produto: item.name,
          receita: item.revenue,
          lucro: item.profit,
          margem: item.margin,
          percentLucro: totalRevenue > 0 ? (item.revenue / totalRevenue) * 100 : 0,
          acumulado: item.cumulativePercentage,
          classificacao: item.classification,
        }));
      }),
    );
  }

  exportProfitabilityPdf(dateFrom?: string, dateTo?: string): Observable<Blob> {
    return this.http.get(`${environment.apiUrl}/reports/profitability/pdf`, {
      params: buildHttpParams({ dateFrom, dateTo }),
      responseType: 'blob',
    });
  }

  exportOrdersPdf(dateFrom?: string, dateTo?: string): Observable<Blob> {
    return this.http.get(`${environment.apiUrl}/reports/orders/pdf`, {
      params: buildHttpParams({ dateFrom, dateTo }),
      responseType: 'blob',
    });
  }

  exportProfitabilityExcel(dateFrom?: string, dateTo?: string): Observable<Blob> {
    return this.http.get(`${environment.apiUrl}/reports/profitability/excel`, {
      params: buildHttpParams({ dateFrom, dateTo }),
      responseType: 'blob',
    });
  }

  exportOrdersExcel(dateFrom?: string, dateTo?: string): Observable<Blob> {
    return this.http.get(`${environment.apiUrl}/reports/orders/excel`, {
      params: buildHttpParams({ dateFrom, dateTo }),
      responseType: 'blob',
    });
  }

  exportInventoryExcel(): Observable<Blob> {
    return this.http.get(`${environment.apiUrl}/reports/inventory/excel`, {
      responseType: 'blob',
    });
  }
}
