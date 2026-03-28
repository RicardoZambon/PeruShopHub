import { Component, signal, computed, inject, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { Subject, debounceTime, takeUntil } from 'rxjs';
import { PricingService, SimulateRequest, SimulationResult, SimulationScenario } from '../../services/pricing.service';
import { ProductService, Product } from '../../services/product.service';
import { ToastService } from '../../services/toast.service';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import { SkeletonComponent } from '../../shared/components/skeleton/skeleton.component';
import { ButtonComponent } from '../../shared/components/button/button.component';
import { SelectDropdownComponent, SelectOption } from '../../shared/components/select-dropdown/select-dropdown.component';
import { formatBrl } from '../../shared/utils';

interface CostField {
  key: string;
  label: string;
  value: number;
  isRate: boolean;
  suffix: string;
}

@Component({
  selector: 'app-simulator',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    PageHeaderComponent,
    SkeletonComponent,
    ButtonComponent,
    SelectDropdownComponent,
  ],
  templateUrl: './simulator.component.html',
  styleUrl: './simulator.component.scss',
})
export class SimulatorComponent implements OnInit, OnDestroy {
  private readonly pricingService = inject(PricingService);
  private readonly productService = inject(ProductService);
  private readonly toastService = inject(ToastService);
  private readonly router = inject(Router);
  private readonly destroy$ = new Subject<void>();
  private readonly recalculate$ = new Subject<void>();

  loading = signal(true);
  simulating = signal(false);
  products = signal<Product[]>([]);
  selectedProductId = signal<string>('');
  selectedProduct = signal<Product | null>(null);
  result = signal<SimulationResult | null>(null);

  // Batch mode
  batchMode = signal(false);
  batchProducts = signal<string[]>([]);
  batchResults = signal<SimulationResult[]>([]);
  batchSimulating = signal(false);

  // Cost override fields
  costFields = signal<CostField[]>([]);

  productOptions = computed<SelectOption[]>(() =>
    this.products().map(p => ({ value: p.id, label: `${p.sku} — ${p.name}` })),
  );

  formatBrl = formatBrl;

  ngOnInit(): void {
    this.loadProducts();
    this.recalculate$
      .pipe(debounceTime(300), takeUntil(this.destroy$))
      .subscribe(() => this.runSimulation());
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  async loadProducts(): Promise<void> {
    try {
      const res = await this.productService.list({ pageSize: 500 });
      this.products.set(res.items);
    } catch {
      this.toastService.show('Erro ao carregar produtos', 'danger');
    } finally {
      this.loading.set(false);
    }
  }

  onProductSelect(productId: string): void {
    this.selectedProductId.set(productId);
    const product = this.products().find(p => p.id === productId) ?? null;
    this.selectedProduct.set(product);
    this.result.set(null);

    if (product) {
      this.costFields.set([
        { key: 'price', label: 'Preço de Venda', value: product.price, isRate: false, suffix: 'R$' },
        { key: 'productCost', label: 'Custo do Produto', value: product.purchaseCost, isRate: false, suffix: 'R$' },
        { key: 'packagingCost', label: 'Embalagem', value: product.packagingCost, isRate: false, suffix: 'R$' },
        { key: 'shippingCost', label: 'Frete (vendedor)', value: 0, isRate: false, suffix: 'R$' },
        { key: 'advertisingCost', label: 'Publicidade', value: 0, isRate: false, suffix: 'R$' },
        { key: 'commissionRate', label: 'Comissão', value: 11, isRate: true, suffix: '%' },
        { key: 'taxRate', label: 'Impostos', value: 6, isRate: true, suffix: '%' },
        { key: 'paymentFeeRate', label: 'Taxa de Pagamento', value: 4.99, isRate: true, suffix: '%' },
      ]);
      // Auto-simulate on product selection
      this.recalculate$.next();
    }
  }

  onFieldChange(): void {
    this.recalculate$.next();
  }

  async runSimulation(): Promise<void> {
    const product = this.selectedProduct();
    if (!product) return;

    this.simulating.set(true);
    try {
      const overrides: Record<string, number> = {};
      for (const field of this.costFields()) {
        overrides[field.key] = field.value;
      }

      const request: SimulateRequest = {
        productId: product.id,
        overrides,
        marketplaceId: 'mercadolivre',
      };

      const res = await this.pricingService.simulate(request);
      this.result.set(res);
    } catch {
      this.toastService.show('Erro na simulação', 'danger');
    } finally {
      this.simulating.set(false);
    }
  }

  // Batch simulation
  toggleBatchMode(): void {
    this.batchMode.update(v => !v);
    this.batchProducts.set([]);
    this.batchResults.set([]);
  }

  toggleBatchProduct(productId: string): void {
    this.batchProducts.update(ids =>
      ids.includes(productId) ? ids.filter(id => id !== productId) : [...ids, productId],
    );
  }

  isBatchSelected(productId: string): boolean {
    return this.batchProducts().includes(productId);
  }

  async runBatchSimulation(): Promise<void> {
    const productIds = this.batchProducts();
    if (productIds.length === 0) return;

    this.batchSimulating.set(true);
    try {
      const items: SimulateRequest[] = productIds.map(id => ({
        productId: id,
        overrides: {},
        marketplaceId: 'mercadolivre',
      }));

      const results = await this.pricingService.batchSimulate(items);
      this.batchResults.set(results);
    } catch {
      this.toastService.show('Erro na simulação em lote', 'danger');
    } finally {
      this.batchSimulating.set(false);
    }
  }

  getMarginColor(diff: number): string {
    if (diff > 0) return 'var(--success)';
    if (diff < 0) return 'var(--danger)';
    return 'var(--text-secondary)';
  }

  getBarWidth(amount: number, scenario: SimulationScenario): string {
    if (scenario.price <= 0) return '0%';
    return `${Math.min(Math.abs(amount / scenario.price) * 100, 100)}%`;
  }
}
