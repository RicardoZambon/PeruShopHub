import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface UserRow {
  id: number;
  nome: string;
  email: string;
  role: string;
  ativo: boolean;
}

export interface Integration {
  id: string;
  name: string;
  marketplace: string;
  status: string;
  connected: boolean;
  comingSoon: boolean;
  logo: string;
  sellerNickname?: string;
  lastSync?: string;
  connectedAt?: string;
}

export interface FixedCostsResponse {
  embalagemPadrao: number;
  aliquotaSimples: number;
  fixedCosts: { id: number; nome: string; valor: number }[];
}

export interface CommissionRule {
  id: string;
  marketplace: string;
  categoryPattern: string;
  listingType: string;
  rate: number;
  isDefault: boolean;
}

export interface TaxProfile {
  id: string;
  taxRegime: string;
  aliquotPercentage: number;
  state: string | null;
}

export interface PaymentFeeRule {
  id: string;
  installmentMin: number;
  installmentMax: number;
  feePercentage: number;
  isDefault: boolean;
}

export interface ReportSchedule {
  id: string;
  frequency: string;
  recipients: string;
  isActive: boolean;
  lastSentAt: string | null;
  createdAt: string;
}

export interface AlertRule {
  id: string;
  type: string;
  threshold: number;
  isActive: boolean;
  productId: string | null;
  productName: string | null;
  createdAt: string;
}

@Injectable({ providedIn: 'root' })
export class SettingsService {
  private http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/settings`;

  getUsers(): Observable<UserRow[]> {
    return this.http.get<UserRow[]>(`${this.baseUrl}/users`);
  }

  getIntegrations(): Observable<Integration[]> {
    return this.http.get<Integration[]>(`${this.baseUrl}/integrations`);
  }

  getCosts(): Observable<FixedCostsResponse> {
    return this.http.get<FixedCostsResponse>(`${this.baseUrl}/costs`);
  }

  getCommissionRules(): Observable<CommissionRule[]> {
    return this.http.get<CommissionRule[]>(`${this.baseUrl}/commission-rules`);
  }

  createCommissionRule(dto: any): Observable<CommissionRule> {
    return this.http.post<CommissionRule>(`${this.baseUrl}/commission-rules`, dto);
  }

  updateCommissionRule(id: string, dto: any): Observable<CommissionRule> {
    return this.http.put<CommissionRule>(`${this.baseUrl}/commission-rules/${id}`, dto);
  }

  deleteCommissionRule(id: string): Observable<any> {
    return this.http.delete(`${this.baseUrl}/commission-rules/${id}`);
  }

  updateCosts(dto: { taxRate: number }): Observable<any> {
    return this.http.put(`${this.baseUrl}/costs`, dto);
  }

  getTaxProfile(): Observable<TaxProfile> {
    return this.http.get<TaxProfile>(`${this.baseUrl}/tax-profile`);
  }

  updateTaxProfile(dto: { taxRegime: string; aliquotPercentage: number; state: string | null }): Observable<TaxProfile> {
    return this.http.put<TaxProfile>(`${this.baseUrl}/tax-profile`, dto);
  }

  getPaymentFeeRules(): Observable<PaymentFeeRule[]> {
    return this.http.get<PaymentFeeRule[]>(`${this.baseUrl}/payment-fee-rules`);
  }

  createPaymentFeeRule(dto: { installmentMin: number; installmentMax: number; feePercentage: number }): Observable<PaymentFeeRule> {
    return this.http.post<PaymentFeeRule>(`${this.baseUrl}/payment-fee-rules`, dto);
  }

  updatePaymentFeeRule(id: string, dto: { installmentMin: number; installmentMax: number; feePercentage: number }): Observable<PaymentFeeRule> {
    return this.http.put<PaymentFeeRule>(`${this.baseUrl}/payment-fee-rules/${id}`, dto);
  }

  deletePaymentFeeRule(id: string): Observable<any> {
    return this.http.delete(`${this.baseUrl}/payment-fee-rules/${id}`);
  }

  getReportSchedules(): Observable<ReportSchedule[]> {
    return this.http.get<ReportSchedule[]>(`${this.baseUrl}/report-schedules`);
  }

  createReportSchedule(dto: { frequency: string; recipients: string; isActive: boolean }): Observable<ReportSchedule> {
    return this.http.post<ReportSchedule>(`${this.baseUrl}/report-schedules`, dto);
  }

  updateReportSchedule(id: string, dto: { frequency: string; recipients: string; isActive: boolean }): Observable<ReportSchedule> {
    return this.http.put<ReportSchedule>(`${this.baseUrl}/report-schedules/${id}`, dto);
  }

  deleteReportSchedule(id: string): Observable<any> {
    return this.http.delete(`${this.baseUrl}/report-schedules/${id}`);
  }

  // Alert Rules
  getAlertRules(): Observable<AlertRule[]> {
    return this.http.get<AlertRule[]>(`${this.baseUrl}/alert-rules`);
  }

  createAlertRule(dto: { type: string; threshold: number; productId: string | null }): Observable<AlertRule> {
    return this.http.post<AlertRule>(`${this.baseUrl}/alert-rules`, dto);
  }

  updateAlertRule(id: string, dto: { threshold: number; isActive: boolean }): Observable<AlertRule> {
    return this.http.put<AlertRule>(`${this.baseUrl}/alert-rules/${id}`, dto);
  }

  deleteAlertRule(id: string): Observable<any> {
    return this.http.delete(`${this.baseUrl}/alert-rules/${id}`);
  }
}
