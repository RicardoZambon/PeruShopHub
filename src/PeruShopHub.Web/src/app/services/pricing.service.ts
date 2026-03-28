import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../environments/environment';
import { buildHttpParams } from '../shared/utils';

export interface PriceCalculationRequest {
  productId: string;
  targetMarginPercent: number;
  marketplaceId: string;
  listingType?: string | null;
}

export interface CostComponent {
  label: string;
  amount: number;
  percentage: number;
  color: string;
}

export interface PriceCalculationResult {
  suggestedPrice: number;
  productCost: number;
  packagingCost: number;
  commissionAmount: number;
  commissionRate: number;
  taxAmount: number;
  taxRate: number;
  paymentFeeAmount: number;
  paymentFeeRate: number;
  totalCosts: number;
  profitAmount: number;
  actualMarginPercent: number;
  costBreakdown: CostComponent[];
}

export interface PricingRule {
  id: string;
  productId: string;
  productName: string;
  productSku: string;
  marketplaceId: string;
  listingType: string | null;
  targetMarginPercent: number;
  suggestedPrice: number;
  createdAt: string;
  updatedAt: string;
}

export interface CreatePricingRuleDto {
  productId: string;
  marketplaceId: string;
  listingType?: string | null;
  targetMarginPercent: number;
}

@Injectable({ providedIn: 'root' })
export class PricingService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/pricing`;

  async calculate(request: PriceCalculationRequest): Promise<PriceCalculationResult> {
    return firstValueFrom(
      this.http.post<PriceCalculationResult>(`${this.baseUrl}/calculate`, request),
    );
  }

  async getRules(productId?: string, marketplaceId?: string): Promise<PricingRule[]> {
    return firstValueFrom(
      this.http.get<PricingRule[]>(`${this.baseUrl}/rules`, {
        params: buildHttpParams({ productId, marketplaceId }),
      }),
    );
  }

  async createRule(dto: CreatePricingRuleDto): Promise<PricingRule> {
    return firstValueFrom(
      this.http.post<PricingRule>(`${this.baseUrl}/rules`, dto),
    );
  }

  async updateRule(id: string, targetMarginPercent: number): Promise<PricingRule> {
    return firstValueFrom(
      this.http.put<PricingRule>(`${this.baseUrl}/rules/${id}`, { targetMarginPercent }),
    );
  }

  async deleteRule(id: string): Promise<void> {
    await firstValueFrom(
      this.http.delete<void>(`${this.baseUrl}/rules/${id}`),
    );
  }
}
